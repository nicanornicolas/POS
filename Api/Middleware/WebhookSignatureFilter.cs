// -----------------------------------------------------------------------
// <copyright file="WebhookSignatureFilter.cs" company="Ikonex Systems">
//     Copyright (c) 2026 Ikonex Systems. All rights reserved.
//     PROPRIETARY AND CONFIDENTIAL.
//     Unauthorized copying of this file, via any medium, is strictly prohibited.
// </copyright>
// -----------------------------------------------------------------------

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
