using Application.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications;

public sealed class TerminalNotificationHandler(ILogger<TerminalNotificationHandler> logger) : INotificationHandler<TransactionSettledEvent>
{
    public Task Handle(TransactionSettledEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Step 17: Notifying terminal {TerminalId} for settled transaction {TransactionId} with amount {Amount} {Currency}",
            notification.TerminalId,
            notification.InternalTransactionId,
            notification.Amount.Amount,
            notification.Amount.Currency);

        // Stub for future FCM/SignalR integration
        return Task.CompletedTask;
    }
}
