using System.Text.Json.Serialization;
using Api.Middleware;
using Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class WebhooksController(IMediator mediator) : ControllerBase
{
    [HttpPost("flutterwave")]
    [ServiceFilter(typeof(WebhookSignatureFilter))]
    public async Task<IActionResult> HandleFlutterwaveWebhook([FromBody] FlutterwaveWebhookPayload payload, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(payload?.Data?.Id.ToString()))
        {
            return BadRequest(new { error = "Invalid payload" });
        }

        await mediator.Send(new ProcessWebhookCommand(payload.Data.Id.ToString()), ct);
        return Ok();
    }
}

public record FlutterwaveWebhookPayload([property: JsonPropertyName("data")] WebhookTransactionData Data);

public record WebhookTransactionData([property: JsonPropertyName("id")] int Id);
