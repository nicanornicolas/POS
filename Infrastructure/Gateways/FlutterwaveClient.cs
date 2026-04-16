using System.Net.Http.Json;
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

    // Private DTOs for JSON deserialization
    private sealed record FlutterwaveVerifyResponse(
        [property: JsonPropertyName("data")] FlutterwaveData Data);

    private sealed record FlutterwaveData(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("amount")] decimal Amount,
        [property: JsonPropertyName("currency")] string Currency);
}
