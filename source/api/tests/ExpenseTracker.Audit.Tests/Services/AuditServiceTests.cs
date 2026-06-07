using ExpenseTracker.Audit.Entities;
using ExpenseTracker.Audit.Models;
using ExpenseTracker.Audit.Repositories;
using ExpenseTracker.Audit.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExpenseTracker.Audit.Tests.Services;

public sealed class AuditServiceTests
{
    private static IServiceScopeFactory BuildScopeFactory(IAuditRepository repo)
    {
        var services = new ServiceCollection();
        services.AddSingleton(repo);
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task LogAsync_WritesEntryToRepository()
    {
        // Arrange
        var capturedEntries = new List<AuditLog>();
        var repoMock = new Mock<IAuditRepository>();
        repoMock
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((entry, _) => capturedEntries.Add(entry))
            .Returns(Task.CompletedTask);
        repoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var scopeFactory = BuildScopeFactory(repoMock.Object);
        var logger = NullLogger<AuditService>.Instance;
        var sut = new AuditService(scopeFactory, logger);

        var request = new WriteAuditLogRequest(
            UserId: Guid.NewGuid(),
            Action: AuditAction.Login,
            IpAddress: "127.0.0.1");

        // Act
        await sut.LogAsync(request);
        // Allow the fire-and-forget Task.Run to complete.
        await Task.Delay(200);

        // Assert
        repoMock.Verify(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
        repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        capturedEntries.Should().HaveCount(1);
        capturedEntries[0].Action.Should().Be(AuditAction.Login);
        capturedEntries[0].IpAddress.Should().Be("127.0.0.1");
    }

    [Fact]
    public async Task LogAsync_WhenRepositoryThrows_DoesNotPropagate()
    {
        // Arrange — repository throws; the service must swallow the exception.
        var repoMock = new Mock<IAuditRepository>();
        repoMock
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var scopeFactory = BuildScopeFactory(repoMock.Object);
        var logger = NullLogger<AuditService>.Instance;
        var sut = new AuditService(scopeFactory, logger);

        // Act — must not throw.
        var act = async () =>
        {
            await sut.LogAsync(new WriteAuditLogRequest(null, AuditAction.Logout, IpAddress: "::1"));
            await Task.Delay(200);
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogAsync_WithNullOrEmptyIpAddress_StoresUnknown()
    {
        var capturedEntries = new List<AuditLog>();
        var repoMock = new Mock<IAuditRepository>();
        repoMock
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((e, _) => capturedEntries.Add(e))
            .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = new AuditService(BuildScopeFactory(repoMock.Object), NullLogger<AuditService>.Instance);

        await sut.LogAsync(new WriteAuditLogRequest(null, AuditAction.ReceiptUpload, IpAddress: ""));
        await Task.Delay(200);

        capturedEntries[0].IpAddress.Should().Be("unknown");
    }

    [Fact]
    public async Task GetLogsAsync_ReturnsMappedPagedResponse()
    {
        // Arrange — return two fake audit log entries.
        var userId = Guid.NewGuid();
        var fakeEntries = new List<AuditLog>
        {
            new()
            {
                UserId = userId,
                Action = AuditAction.Login,
                IpAddress = "10.0.0.1",
            },
            new()
            {
                UserId = userId,
                Action = AuditAction.ReceiptUpload,
                ResourceType = AuditResourceType.Receipt,
                ResourceId = Guid.NewGuid(),
                IpAddress = "10.0.0.1",
            },
        };

        var repoMock = new Mock<IAuditRepository>();
        repoMock
            .Setup(r => r.QueryAsync(It.IsAny<AuditLogQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<AuditLog>)fakeEntries, fakeEntries.Count));

        var sut = new AuditService(BuildScopeFactory(repoMock.Object), NullLogger<AuditService>.Instance);

        // Act
        var result = await sut.GetLogsAsync(new AuditLogQuery(userId, null, null, null));

        // Assert
        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].Action.Should().Be(AuditAction.Login);
        result.Items[1].ResourceType.Should().Be(AuditResourceType.Receipt);
    }
}
