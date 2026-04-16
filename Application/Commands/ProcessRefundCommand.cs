using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Commands;

public sealed record ProcessRefundCommand(Guid TransactionId) : IRequest<bool>;

public sealed class ProcessRefundCommandHandler(IAppDbContext dbContext, IFlutterwaveClient flutterwave)
    : IRequestHandler<ProcessRefundCommand, bool>
{
    public async Task<bool> Handle(ProcessRefundCommand request, CancellationToken ct)
    {
        var transaction = await dbContext.Transactions
            .SingleOrDefaultAsync(transaction => transaction.Id == request.TransactionId, ct)
            ?? throw new TransactionNotFoundException($"Transaction {request.TransactionId} not found.");

        if (transaction.Status == TransactionStatus.Refunded)
        {
            return true;
        }

        if (transaction.Status != TransactionStatus.Settled)
        {
            throw new InvalidTransactionStateException("Only settled transactions can be refunded.");
        }

        var refundAccepted = await flutterwave.ProcessRefundAsync(transaction.ProviderTransactionId, ct);
        if (!refundAccepted)
        {
            throw new ProviderOperationFailedException("Provider rejected the refund request.");
        }

        transaction.MarkAsRefunded();

        dbContext.LedgerEntries.Add(LedgerEntry.Debit(transaction.Id, "MerchantEscrow", transaction.Amount, "Refund reversal"));
        dbContext.LedgerEntries.Add(LedgerEntry.Credit(transaction.Id, "CustomerAccount", transaction.Amount, "Refund reversal"));

        await dbContext.SaveChangesAsync(ct);
        return true;
    }
}
