namespace Application.Interfaces;

public sealed record VerifyResult(bool IsSuccessful, decimal Amount, string Currency);

public interface IFlutterwaveClient
{
    Task<VerifyResult> VerifyTransactionAsync(string transactionId, CancellationToken ct);

    Task<bool> ProcessRefundAsync(string transactionId, CancellationToken ct);
}
