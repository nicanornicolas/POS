using Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TransactionsController(IMediator mediator) : ControllerBase
{
    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> RefundTransaction(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ProcessRefundCommand(id), ct);
        return Ok(new { success = result, message = "Refund processed successfully." });
    }
}
