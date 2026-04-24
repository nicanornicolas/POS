using Application.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Crypto;

public sealed class CryptoConversionTriggerHandler(ILogger<CryptoConversionTriggerHandler> logger) : INotificationHandler<TransactionSettledEvent>
{
    public Task Handle(TransactionSettledEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Step 9/10: Triggering crypto conversion for transaction {TransactionId} with amount {Amount} {Currency}",
            notification.InternalTransactionId,
            notification.Amount.Amount,
            notification.Amount.Currency);

        // Stub for future Binance/TenderCash integration
        return Task.CompletedTask;
    }
}
