using Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Tests;

public class WebhookSignatureFilterTests
{
    private static ActionExecutingContext CreateContext(string? headerValue)
    {
        var httpContext = new DefaultHttpContext();

        if (headerValue != null)
        {
            httpContext.Request.Headers["verif-hash"] = headerValue;
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: null!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("WRONG_HASH")]
    public async Task OnActionExecutionAsync_InvalidOrMissingHeader_ReturnsUnauthorized(string? headerValue)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "Flutterwave:WebhookHash", "CORRECT_HASH" } })
            .Build();

        var filter = new WebhookSignatureFilter(config);
        var context = CreateContext(headerValue);

        await filter.OnActionExecutionAsync(context,
            () => Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), null!)));

        context.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task OnActionExecutionAsync_ValidHeader_CallsNext()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "Flutterwave:WebhookHash", "CORRECT_HASH" } })
            .Build();

        var filter = new WebhookSignatureFilter(config);
        var context = CreateContext("CORRECT_HASH");

        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), null!));
        };

        await filter.OnActionExecutionAsync(context, next);

        context.Result.Should().BeNull();
        nextCalled.Should().BeTrue();
    }
}
