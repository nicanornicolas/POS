using Api.Middleware;
using Application.Commands;
using Application.Interfaces;
using Infrastructure.Gateways;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// 1. Persistence setup
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

// 2. CQRS / MediatR setup
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ProcessRefundCommand).Assembly));

// 3. Provider setup with Polly resilience
builder.Services.AddHttpClient<IFlutterwaveClient, FlutterwaveClient>()
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// 4. API and security setup
builder.Services.AddScoped<WebhookSignatureFilter>();
builder.Services.AddControllers();

var app = builder.Build();

// 5. Middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Fail-fast validation for critical payment provider configuration.
using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    if (string.IsNullOrWhiteSpace(config["Flutterwave:SecretKey"]))
    {
        throw new Exception("CRITICAL: Flutterwave:SecretKey is missing from configuration.");
    }

    if (string.IsNullOrWhiteSpace(config["Flutterwave:WebhookHash"]))
    {
        throw new Exception("CRITICAL: Flutterwave:WebhookHash is missing from configuration.");
    }
}

app.Run();
