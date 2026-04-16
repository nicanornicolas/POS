using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Commands;

public sealed record ProcessWebhookCommand(string ProviderTransactionId) : IRequest<bool>;

public sealed class ProcessWebhookCommandHandler(IAppDbContext dbContext, IFlutterwaveClient flutterwave)
    : IRequestHandler<ProcessWebhookCommand, bool>
{
    public async Task<bool> Handle(ProcessWebhookCommand request, CancellationToken ct)
    {
        var verification = await flutterwave.VerifyTransactionAsync(request.ProviderTransactionId, ct);

        if (!verification.IsSuccessful)
        {
            throw new ProviderOperationFailedException("Webhook verification failed at provider.");
        }

        var transaction = await dbContext.Transactions
            .SingleOrDefaultAsync(transaction => transaction.ProviderTransactionId == request.ProviderTransactionId, ct)
            ?? throw new TransactionNotFoundException($"Local transaction for provider ID {request.ProviderTransactionId} not found.");

        if (transaction.Amount.Amount != verification.Amount ||
            !string.Equals(transaction.Amount.Currency, verification.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new AmountMismatchException("Verified amount/currency does not match local ledger.");
        }

        if (transaction.Status == TransactionStatus.Settled)
        {
            return true;
        }

        if (transaction.Status != TransactionStatus.Pending)
        {
            throw new InvalidTransactionStateException("Only pending transactions can be settled by a webhook.");
        }

        transaction.MarkAsSettled();

        dbContext.LedgerEntries.Add(LedgerEntry.Debit(transaction.Id, "CustomerAccount", transaction.Amount, "Sale settlement"));
        dbContext.LedgerEntries.Add(LedgerEntry.Credit(transaction.Id, "MerchantEscrow", transaction.Amount, "Sale settlement"));

        await dbContext.SaveChangesAsync(ct);
        return true;
    }
}
