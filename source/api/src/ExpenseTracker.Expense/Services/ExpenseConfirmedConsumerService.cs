using System.Text.Json;
using ExpenseTracker.Expense.Repositories;
using ExpenseTracker.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ExpenseTracker.Expense.Services;

/// <summary>
/// Reads from the expense.confirmed Redis stream and upserts into merchant_category_map
/// and merchant_tag_history to power auto-categorization and smart tag suggestions.
/// </summary>
public sealed class ExpenseConfirmedConsumerService(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    ILogger<ExpenseConfirmedConsumerService> logger) : BackgroundService
{
    private const string Stream = "expense.confirmed";
    private const string ConsumerGroup = "intelligence-workers";
    private const string ConsumerName = "intelligence-consumer-1";
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
            "ExpenseConfirmedConsumerService started on stream '{Stream}'", Stream);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await db.StreamReadGroupAsync(
                    Stream, ConsumerGroup, ConsumerName, ">", count: 10, noAck: false);

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
                logger.LogError(ex, "Unhandled error in ExpenseConfirmedConsumerService loop");
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

            var payload = JsonSerializer.Deserialize<ConfirmedPayload>(json, JsonOptions);
            if (payload is null)
            {
                await db.StreamAcknowledgeAsync(Stream, ConsumerGroup, message.Id);
                return;
            }

            if (!string.IsNullOrWhiteSpace(payload.MerchantName) &&
                !string.IsNullOrWhiteSpace(payload.Category))
            {
                var normalized = MerchantNormalizer.Normalize(payload.MerchantName);
                if (!string.IsNullOrEmpty(normalized))
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IIntelligenceRepository>();

                    await repo.UpsertMerchantCategoryAsync(
                        payload.HouseholdId, normalized, payload.Category, ct);

                    if (payload.Tags is { Length: > 0 })
                        await repo.UpsertTagHistoryAsync(payload.HouseholdId, normalized, payload.Tags, ct);

                    await repo.SaveChangesAsync(ct);

                    logger.LogInformation(
                        "Merchant-category map updated: merchant={Merchant} category={Category}",
                        normalized, payload.Category);
                }
            }

            await db.StreamAcknowledgeAsync(Stream, ConsumerGroup, message.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to process expense.confirmed message {Id} — ACKing to prevent requeue", message.Id);
            await db.StreamAcknowledgeAsync(Stream, ConsumerGroup, message.Id);
        }
    }

    private sealed record ConfirmedPayload(
        Guid HouseholdId,
        string? MerchantName,
        string? Category,
        string[]? Tags);
}
