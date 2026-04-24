using Application.Events;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Notifications;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests;

public class TerminalNotificationHandlerTests
{
    [Fact]
    public async Task Handle_LogsCorrectTerminalIdAndTransactionDetails()
    {
        var mockLogger = new Mock<ILogger<TerminalNotificationHandler>>();
        var handler = new TerminalNotificationHandler(mockLogger.Object);

        var transactionId = Guid.NewGuid();
        var notification = new TransactionSettledEvent(transactionId, "TERM-005", new Money(2500m, "KES"));

        await handler.Handle(notification, CancellationToken.None);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Step 17") &&
                    v.ToString()!.Contains("TERM-005") &&
                    v.ToString()!.Contains(transactionId.ToString()) &&
                    v.ToString()!.Contains("2500") &&
                    v.ToString()!.Contains("KES")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
