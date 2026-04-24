using Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TransactionsController(IMediator mediator) : ControllerBase
{
    [HttpPost("charge")]
    public async Task<IActionResult> Charge([FromBody] ChargeApiRequest request, CancellationToken ct)
    {
        var transactionId = await mediator.Send(
            new ProcessChargeCommand(request.Amount, request.Currency, request.PaymentToken, request.TerminalId), ct);

        return Accepted(new
        {
            TransactionId = transactionId,
            Status = "Pending",
            Message = "Charge initiated. Awaiting network confirmation."
        });
    }

    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> RefundTransaction(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ProcessRefundCommand(id), ct);
        return Ok(new { success = result, message = "Refund processed successfully." });
    }
}

public sealed record ChargeApiRequest(decimal Amount, string Currency, string PaymentToken, string TerminalId);
