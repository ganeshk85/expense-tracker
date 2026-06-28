# ExpenseTracker.Ocr

Handles the async OCR pipeline: job queuing, result consumption, and expense hydration from receipt scans.

---

## Redis Streams

Two Redis streams connect the .NET API to the Python OCR worker. Neither side knows how the other is implemented — the stream messages are the contract between them.

### `ocr.jobs` — Work Queue (API → OCR Worker)

**Type:** Redis Stream, consumer group `ocr-workers`

Written by the .NET API inside `ReceiptService.UploadAsync` immediately after the file is saved to disk. The upload endpoint returns to the caller without waiting for OCR to complete.

Message payload:
```json
{
  "receiptId": "b501c4da-...",
  "filePath": "/storage/receipts/{userId}/{uuid}.jpg",
  "userId": "243d8776-...",
  "submittedAt": "2026-06-27T18:00:00Z"
}
```

Consumed by the Python `OcrWorker` using `XREADGROUP` with `">"` (undelivered messages only), blocking up to 5 seconds while the queue is empty.

---

### `ocr.results` — Results Stream (OCR Worker → API)

**Type:** Redis Stream, consumer group `api-consumer`

Written by the Python `OcrWorker` after each processing attempt. Three possible message types:

| Status | When published |
|---|---|
| `complete` | OCR succeeded — includes all extracted fields |
| `processing (retry N of 3)` | Between retry attempts — lets the UI show retry progress |
| `ocr_failed` | All 3 attempts exhausted |

Consumed by `OcrResultConsumerService` (a .NET `BackgroundService`) polling every 500ms, processing up to 10 messages per read.

---

### Why Streams Instead of a Simple List

Redis streams (`XADD` / `XREADGROUP` / `XACK`) provide three guarantees a plain list (`LPUSH` / `BRPOP`) does not:

1. **At-least-once delivery** — messages stay in the Pending Entries List (PEL) until explicitly `XACK`'d. A worker crash does not lose the job.
2. **Multiple consumer instances** — a consumer group allows multiple OCR worker instances to read the same stream without duplicating work.
3. **Audit trail** — stream entries persist after ACK (until trimmed), allowing post-hoc inspection of what was processed.

> Note: the `receipt.uploaded` queue (thumbnail generation) uses a plain `LPUSH` / `BRPOP` list. Thumbnails are best-effort so the simpler mechanism is sufficient there.

---

## End-to-End Workflow

```
User uploads file
      │
      ▼
POST /receipts/upload  (.NET API)
  ├─ Save file to /storage/receipts/{userId}/{uuid}.jpg
  ├─ INSERT receipt row  (status = Processing)
  ├─ LPUSH receipt.uploaded  ──────────────────► ThumbnailWorker (Python)
  │                                                  │ Generate 200px JPEG
  │                                                  │ Save to /storage/thumbnails/{id}.jpg
  │                                                  └► PATCH /receipts/{id}/thumbnail
  │                                                         └─ UPDATE receipts SET ThumbnailPath
  │
  └─ XADD ocr.jobs  ───────────────────────────► OcrWorker (Python)
                                                      │
                                                      ├─ 1. Load image (JPG/PNG/HEIC/PDF)
                                                      ├─ 2. OpenCV preprocess
                                                      │       grayscale → deskew → denoise → threshold
                                                      ├─ 3. Tesseract — extract raw text
                                                      ├─ 4. Parse fields
                                                      │       merchant, date, total, subtotal, tax, line items
                                                      ├─ 5. pyzbar — scan barcode
                                                      └─ 6. Write raw JSON to /storage/ocr-json/{id}.json
                                                            │
                                          ┌─────────────────┴──────────────────┐
                                       success                            failure (retry up to 3×)
                                          │                                     │
                                   XADD ocr.results                     XADD ocr.results
                                   status=complete                       status=processing (retry N of 3)
                                                                         ... after 3 failures:
                                                                         status=ocr_failed
                                          │
                                          ▼
                              OcrResultConsumerService  (.NET BackgroundService)
                                XREADGROUP ocr.results  every 500ms
                                          │
                          ┌───────────────┼─────────────────┐
                       complete      retry N of 3        ocr_failed
                          │               │                   │
                INSERT expenses    OcrRetryCount++    receipt status
                + expense_items                        = OcrFailed
                receipt status
                = Complete
                          │
                          ▼
            Frontend polls GET /receipts/{id}/status  every 2s
            ├─ Returns: status, thumbnailUrl, ocrRetryCount
            ├─ status=Complete   → shows "View Expenses"
            └─ status=OcrFailed  → shows "Add Manually"
```

---

## Retry Behaviour

The OCR worker retries failed pipelines up to 3 times with exponential backoff:

| Attempt | Delay before this attempt |
|---|---|
| 1 | None — runs immediately |
| 2 | 10 seconds |
| 3 | 30 seconds |

Between attempts, a `processing (retry N of 3)` message is published to `ocr.results`. The API increments `OcrRetryCount` on the receipt row so the frontend polling endpoint can surface the progress.

After all attempts are exhausted, an `ocr_failed` message is published and the receipt status is set to `OcrFailed`. The message is always `XACK`'d regardless of outcome — failure state is conveyed through the result message, not through message requeue.

---

## Key Design Decisions

**OCR runs in Python, results consumed in .NET.** The stream is the only coupling between the two runtimes. The API does not import or call Python directly.

**The upload endpoint never blocks on OCR.** `POST /receipts/upload` returns in under 2 seconds. OCR completion is async — the frontend discovers it by polling `GET /receipts/{id}/status`.

**Always XACK, even on failure.** Leaving a message un-ACK'd would cause the worker to redeliver it indefinitely. Instead, failures are surfaced via the `ocr_failed` result message, which the API translates into a `OcrFailed` receipt status.

**Thumbnail generation is independent of OCR.** The thumbnail job is enqueued at the same time as the OCR job but processed by a separate `ThumbnailWorker`. A thumbnail can be ready and shown on the processing screen before OCR completes.

---

## Module Structure

```
ExpenseTracker.Ocr/
  Entities/
    Expense.cs          -- Expense and ExpenseItem EF Core entities; OcrStatusValue and ExpenseSource constants
  Models/
    OcrStreamMessages.cs -- Deserialization types for ocr.results payload
  Repositories/
    IExpenseRepository.cs -- FindByReceiptIdAsync, UpsertAsync (used by result consumer)
  Services/
    OcrResultConsumerService.cs -- BackgroundService reading from ocr.results stream
  OcrModuleExtensions.cs  -- IServiceCollection registration
```
