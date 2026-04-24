using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using MediatR;

namespace Application.Commands;

public sealed record ProcessChargeCommand(decimal Amount, string Currency, string PaymentToken, string TerminalId) : IRequest<Guid>;

public sealed class ProcessChargeCommandHandler(IAppDbContext dbContext, IFlutterwaveClient flutterwave)
    : IRequestHandler<ProcessChargeCommand, Guid>
{
    public async Task<Guid> Handle(ProcessChargeCommand request, CancellationToken ct)
    {
        var chargeRequest = new ChargeRequest(request.Amount, request.Currency, request.PaymentToken);
        var chargeResult = await flutterwave.ProcessChargeAsync(chargeRequest, ct);

        if (!chargeResult.IsSuccessful)
        {
            throw new ProviderOperationFailedException($"Charge failed: {chargeResult.ErrorMessage}");
        }

        var amount = new Money(request.Amount, request.Currency);
        var transaction = new Transaction(chargeResult.ProviderTransactionId, amount, request.TerminalId);

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync(ct);

        return transaction.Id;
    }
}
