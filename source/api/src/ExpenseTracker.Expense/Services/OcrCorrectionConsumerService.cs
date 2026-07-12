using System.Text.Json;
using ExpenseTracker.Expense.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ExpenseTracker.Expense.Services;

/// <summary>
/// Reads from the ocr.correction Redis stream and upserts into ocr_field_accuracy
/// to track per-merchant, per-field OCR correction rates.
/// </summary>
public sealed class OcrCorrectionConsumerService(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    ILogger<OcrCorrectionConsumerService> logger) : BackgroundService
{
    private const string Stream = "ocr.correction";
    private const string ConsumerGroup = "ocr-accuracy-workers";
    private const string ConsumerName = "ocr-accuracy-consumer-1";
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = redis.GetDatabase();
        await EnsureGroupAsync(db);

        logger.LogInformation(
            "OcrCorrectionConsumerService started on stream '{Stream}'", Stream);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await db.StreamReadGroupAsync(
                    Stream, ConsumerGroup, ConsumerName, ">", count: 20, noAck: false);

                if (messages is { Length: > 0 })
                {
                    foreach (var msg in messages)
                        await ProcessMessageAsync(db, msg, stoppingToken);
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
                logger.LogError(ex, "Unhandled error in OcrCorrectionConsumerService loop");
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }

    private async Task EnsureGroupAsync(IDatabase db)
    {
        try
        {
            await db.StreamCreateConsumerGroupAsync(
                Stream, ConsumerGroup, StreamPosition.NewMessages, createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            logger.LogDebug("Consumer group {Group} already exists on {Stream}", ConsumerGroup, Stream);
        }
    }

    private async Task ProcessMessageAsync(
        IDatabase db, StreamEntry message, CancellationToken ct)
    {
        try
        {
            var json = message["payload"].ToString();
            if (string.IsNullOrWhiteSpace(json))
            {
                await db.StreamAcknowledgeAsync(Stream, ConsumerGroup, message.Id);
                return;
            }

            var payload = JsonSerializer.Deserialize<CorrectionPayload>(json, JsonOptions);
            if (payload is null || string.IsNullOrWhiteSpace(payload.MerchantNormalized) ||
                string.IsNullOrWhiteSpace(payload.Field))
            {
                await db.StreamAcknowledgeAsync(Stream, ConsumerGroup, message.Id);
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IIntelligenceRepository>();

            await repo.UpsertOcrFieldAccuracyAsync(payload.MerchantNormalized, payload.Field, payload.IsCorrected, ct);
            await repo.SaveChangesAsync(ct);

            logger.LogDebug(
                "OCR accuracy updated: merchant={Merchant} field={Field}",
                payload.MerchantNormalized, payload.Field);

            await db.StreamAcknowledgeAsync(Stream, ConsumerGroup, message.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to process ocr.correction message {Id} — ACKing to prevent requeue", message.Id);
            await db.StreamAcknowledgeAsync(Stream, ConsumerGroup, message.Id);
        }
    }

    private sealed record CorrectionPayload(
        string ReceiptId,
        string MerchantNormalized,
        string Field,
        string? OcrValue,
        string? CorrectedValue,
        bool IsCorrected);
}
