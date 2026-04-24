namespace Application.Interfaces;

public sealed record VerifyResult(bool IsSuccessful, decimal Amount, string Currency);

public sealed record ChargeRequest(decimal Amount, string Currency, string PaymentToken);

public sealed record ChargeResult(bool IsSuccessful, string ProviderTransactionId, string ErrorMessage = "");

public interface IFlutterwaveClient
{
    Task<VerifyResult> VerifyTransactionAsync(string transactionId, CancellationToken ct);

    Task<bool> ProcessRefundAsync(string transactionId, CancellationToken ct);

    Task<ChargeResult> ProcessChargeAsync(ChargeRequest request, CancellationToken ct);
}
