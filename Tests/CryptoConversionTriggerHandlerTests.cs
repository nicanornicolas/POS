using Application.Events;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Crypto;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests;

public class CryptoConversionTriggerHandlerTests
{
    [Fact]
    public async Task Handle_LogsCorrectAmountAndCurrency()
    {
        var mockLogger = new Mock<ILogger<CryptoConversionTriggerHandler>>();
        var handler = new CryptoConversionTriggerHandler(mockLogger.Object);

        var transactionId = Guid.NewGuid();
        var notification = new TransactionSettledEvent(transactionId, "TERM-006", new Money(5000m, "KES"));

        await handler.Handle(notification, CancellationToken.None);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Step 9/10") &&
                    v.ToString()!.Contains(transactionId.ToString()) &&
                    v.ToString()!.Contains("5000") &&
                    v.ToString()!.Contains("KES")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
