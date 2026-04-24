using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;

namespace Infrastructure.Gateways;

public sealed class FlutterwaveClient(HttpClient httpClient) : IFlutterwaveClient
{
    public async Task<VerifyResult> VerifyTransactionAsync(string transactionId, CancellationToken ct)
    {
        var response = await httpClient.GetAsync($"transactions/{transactionId}/verify", ct);

        if (!response.IsSuccessStatusCode)
        {
            return new VerifyResult(false, 0, "");
        }

        var result = await response.Content.ReadFromJsonAsync<FlutterwaveVerifyResponse>(cancellationToken: ct);

        if (result?.Data == null || !string.Equals(result.Data.Status, "successful", StringComparison.OrdinalIgnoreCase))
        {
            return new VerifyResult(false, 0, "");
        }

        return new VerifyResult(true, result.Data.Amount, result.Data.Currency);
    }

    public async Task<bool> ProcessRefundAsync(string transactionId, CancellationToken ct)
    {
        var response = await httpClient.PostAsync($"transactions/{transactionId}/refund", null, ct);
        return response.IsSuccessStatusCode;
    }

    // TODO: Align with exact Sunmi SDK payload post-integration
    public async Task<ChargeResult> ProcessChargeAsync(ChargeRequest request, CancellationToken ct)
    {
        var txRef = $"IKONEX-{Guid.NewGuid()}";

        var payload = new
        {
            token = request.PaymentToken,
            currency = request.Currency,
            amount = request.Amount.ToString("F2"),
            tx_ref = txRef,
            email = "customer@ikonex.com"
        };

        var response = await httpClient.PostAsJsonAsync("charges?type=token", payload, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            return new ChargeResult(false, txRef, $"Flutterwave charge failed: {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<FlutterwaveChargeResponse>(cancellationToken: ct);

        if (result?.Data == null || !string.Equals(result.Data.Status, "successful", StringComparison.OrdinalIgnoreCase))
        {
            return new ChargeResult(false, txRef, "Flutterwave charge was not successful.");
        }

        return new ChargeResult(true, txRef);
    }

    // Private DTOs for JSON deserialization
    private sealed record FlutterwaveVerifyResponse(
        [property: JsonPropertyName("data")] FlutterwaveData Data);

    private sealed record FlutterwaveData(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("amount")] decimal Amount,
        [property: JsonPropertyName("currency")] string Currency);

    private sealed record FlutterwaveChargeResponse(
        [property: JsonPropertyName("data")] FlutterwaveChargeData? Data);

    private sealed record FlutterwaveChargeData(
        [property: JsonPropertyName("status")] string Status);
}
