using System.Text.Json;
using ExpenseTracker.Ocr.Entities;
using ExpenseTracker.Ocr.Models;
using ExpenseTracker.Ocr.Repositories;
using ExpenseTracker.Receipt.Entities;
using ExpenseTracker.Receipt.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ExpenseTracker.Ocr.Services;

/// <summary>
/// Background service that continuously reads from the <c>ocr.results</c> Redis stream
/// using XREADGROUP for reliable at-least-once delivery.
///
/// On a "complete" result: upserts the Expense and ExpenseItems records, updates receipt status.
/// On an "ocr_failed" result: updates receipt status only.
///
/// Consumer group: "api-consumer"
/// Consumer name: "api-{MachineName}" (unique per deployment instance).
/// </summary>
public sealed class OcrResultConsumerService(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    ILogger<OcrResultConsumerService> logger) : BackgroundService
{
    private const string ConsumerName = "api-consumer";
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan BlockTimeout = TimeSpan.FromSeconds(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = redis.GetDatabase();
        var instanceId = $"{ConsumerName}-{Environment.MachineName}";

        await EnsureConsumerGroupAsync(db, stoppingToken);

        logger.LogInformation(
            "OcrResultConsumerService started. Stream={Stream} Group={Group} Consumer={Consumer}",
            OcrStreams.ResultsStream, OcrStreams.ConsumerGroup, instanceId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await db.StreamReadGroupAsync(
                    OcrStreams.ResultsStream,
                    OcrStreams.ConsumerGroup,
                    instanceId,
                    ">",           // Only undelivered messages
                    count: 10,
                    noAck: false);

                if (messages is { Length: > 0 })
                {
                    foreach (var message in messages)
                    {
                        await ProcessMessageAsync(db, message, instanceId, stoppingToken);
                    }
                }
                else
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in OcrResultConsumerService loop");
                await Task.Delay(PollInterval, stoppingToken);
            }
        }

        logger.LogInformation("OcrResultConsumerService stopped.");
    }

    private async Task EnsureConsumerGroupAsync(IDatabase db, CancellationToken ct)
    {
        try
        {
            // Create stream + group if they don't exist. "$" means only new messages.
            await db.StreamCreateConsumerGroupAsync(
                OcrStreams.ResultsStream,
                OcrStreams.ConsumerGroup,
                StreamPosition.NewMessages,
                createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Group already exists — this is the normal case on restart.
            logger.LogDebug("Consumer group {Group} already exists on {Stream}",
                OcrStreams.ConsumerGroup, OcrStreams.ResultsStream);
        }
    }

    private async Task ProcessMessageAsync(
        IDatabase db,
        StreamEntry message,
        string instanceId,
        CancellationToken ct)
    {
        var messageId = message.Id.ToString();
        try
        {
            var json = message["payload"].ToString();
            if (string.IsNullOrWhiteSpace(json))
            {
                logger.LogWarning("OCR result message {Id} has empty payload — ACKing and skipping", messageId);
                await db.StreamAcknowledgeAsync(OcrStreams.ResultsStream, OcrStreams.ConsumerGroup, message.Id);
                return;
            }

            var result = JsonSerializer.Deserialize<OcrResultMessage>(json, JsonOptions);
            if (result is null || !Guid.TryParse(result.ReceiptId, out var receiptId))
            {
                logger.LogWarning("OCR result message {Id} could not be deserialized — ACKing and skipping", messageId);
                await db.StreamAcknowledgeAsync(OcrStreams.ResultsStream, OcrStreams.ConsumerGroup, message.Id);
                return;
            }

            logger.LogInformation("Processing OCR result for receipt {ReceiptId} status={Status}",
                receiptId, result.Status);

            await using var scope = scopeFactory.CreateAsyncScope();
            var expenseRepo = scope.ServiceProvider.GetRequiredService<IExpenseRepository>();
            var receiptRepo = scope.ServiceProvider.GetRequiredService<IReceiptRepository>();

            if (result.Status == OcrResultStatus.Complete)
            {
                await HandleCompleteResultAsync(result, receiptId, expenseRepo, receiptRepo, ct);
            }
            else if (result.Status == OcrResultStatus.OcrFailed)
            {
                await HandleFailedResultAsync(receiptId, receiptRepo, ct);
            }
            else if (result.Status.StartsWith("processing (retry", StringComparison.OrdinalIgnoreCase))
            {
                // Retry-in-progress status from the OCR worker — increment retry count for UI polling.
                await HandleRetryStatusAsync(receiptId, receiptRepo, ct);
            }
            else
            {
                logger.LogWarning("Unknown OCR result status '{Status}' for receipt {ReceiptId} — skipping",
                    result.Status, receiptId);
            }

            // XACK — tell Redis this message was processed successfully.
            await db.StreamAcknowledgeAsync(OcrStreams.ResultsStream, OcrStreams.ConsumerGroup, message.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process OCR result message {Id} — ACKing to prevent requeue", messageId);
            await db.StreamAcknowledgeAsync(OcrStreams.ResultsStream, OcrStreams.ConsumerGroup, message.Id);
        }
    }

    private static async Task HandleCompleteResultAsync(
        OcrResultMessage result,
        Guid receiptId,
        IExpenseRepository expenseRepo,
        IReceiptRepository receiptRepo,
        CancellationToken ct)
    {
        DateTimeOffset? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(result.Date) &&
            DateTimeOffset.TryParse(result.Date, out var d))
        {
            parsedDate = d;
        }

        var confidenceJson = result.Confidence is { Count: > 0 }
            ? JsonSerializer.Serialize(result.Confidence)
            : null;

        // Load the receipt to resolve the owning user.
        var receipt = await receiptRepo.FindByIdAsync(receiptId, ct);
        var ownerId = receipt?.UploadedByUserId ?? Guid.Empty;

        var existing = await expenseRepo.FindByReceiptIdAsync(receiptId, ct);
        if (existing is null)
        {
            var expense = new Expense
            {
                ReceiptId = receiptId,
                UserId = ownerId,
                MerchantName = result.MerchantName,
                MerchantAddress = result.MerchantAddress,
                Date = parsedDate,
                Time = result.Time,
                Subtotal = result.Subtotal,
                TaxAmount = result.TaxAmount,
                Total = result.Total,
                Barcode = result.Barcode,
                ConfidenceJson = confidenceJson,
                OcrStatus = OcrStatusValue.Complete,
                Source = ExpenseSource.Ocr,
                Items = MapLineItems(result.LineItems),
            };
            await expenseRepo.UpsertAsync(expense, ct);
        }
        else
        {
            existing.MerchantName = result.MerchantName;
            existing.MerchantAddress = result.MerchantAddress;
            existing.Date = parsedDate;
            existing.Time = result.Time;
            existing.Subtotal = result.Subtotal;
            existing.TaxAmount = result.TaxAmount;
            existing.Total = result.Total;
            existing.Barcode = result.Barcode;
            existing.ConfidenceJson = confidenceJson;
            existing.OcrStatus = OcrStatusValue.Complete;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.Items = MapLineItems(result.LineItems);
            await expenseRepo.UpsertAsync(existing, ct);
        }

        await expenseRepo.SaveChangesAsync(ct);

        // receipt was loaded above to resolve ownerId — update its status in the same context.
        if (receipt is not null)
        {
            receipt.Status = ReceiptStatus.Complete;
            receipt.UpdatedAt = DateTimeOffset.UtcNow;
            await receiptRepo.SaveChangesAsync(ct);
        }
    }

    private static async Task HandleFailedResultAsync(
        Guid receiptId,
        IReceiptRepository receiptRepo,
        CancellationToken ct)
    {
        var receipt = await receiptRepo.FindByIdAsync(receiptId, ct);
        if (receipt is not null)
        {
            receipt.Status = ReceiptStatus.OcrFailed;
            receipt.OcrRetryCount = 3;
            receipt.UpdatedAt = DateTimeOffset.UtcNow;
            await receiptRepo.SaveChangesAsync(ct);
        }
    }

    private static async Task HandleRetryStatusAsync(
        Guid receiptId,
        IReceiptRepository receiptRepo,
        CancellationToken ct)
    {
        var receipt = await receiptRepo.FindByIdAsync(receiptId, ct);
        if (receipt is not null)
        {
            // Status stays Processing; OcrRetryCount is exposed by GET /receipts/{id}/status
            // so the frontend can render "Processing (retry X of 3)".
            receipt.OcrRetryCount++;
            receipt.UpdatedAt = DateTimeOffset.UtcNow;
            await receiptRepo.SaveChangesAsync(ct);
        }
    }

    private static List<ExpenseItem> MapLineItems(IReadOnlyList<OcrLineItem>? lineItems)
        => lineItems?
            .Select(li => new ExpenseItem
            {
                Name = li.Name,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
            })
            .ToList()
           ?? [];
}
