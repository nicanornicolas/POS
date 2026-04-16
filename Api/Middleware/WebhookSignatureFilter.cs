using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace Api.Middleware;

public class WebhookSignatureFilter(IConfiguration config) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var expectedHash = config["Flutterwave:WebhookHash"];

        // Fail securely if the environment is misconfigured.
        if (string.IsNullOrEmpty(expectedHash))
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("verif-hash", out var signature) || signature != expectedHash)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}
