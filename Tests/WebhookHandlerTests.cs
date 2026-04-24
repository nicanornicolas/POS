using Application.Commands;
using Application.Events;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Tests;

public class WebhookHandlerTests
{
    private static AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_AmountMismatch_ThrowsAmountMismatchException()
    {
        await using var db = GetInMemoryDbContext();
        var tx = new Transaction("FLW-789", new Money(10000, "KES"), "TERM-001");
        db.Transactions.Add(tx);
        await db.SaveChangesAsync(CancellationToken.None);

        var mockProvider = new Mock<IFlutterwaveClient>();
        mockProvider.Setup(p => p.VerifyTransactionAsync("FLW-789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerifyResult(true, 1, "KES"));

        var mockMediator = new Mock<IMediator>();
        var handler = new ProcessWebhookCommandHandler(db, mockProvider.Object, mockMediator.Object);

        var act = async () => await handler.Handle(new ProcessWebhookCommand("FLW-789"), CancellationToken.None);
        await act.Should().ThrowAsync<AmountMismatchException>();

        var dbTx = await db.Transactions.SingleAsync(t => t.ProviderTransactionId == "FLW-789");
        dbTx.Status.Should().Be(TransactionStatus.Pending);
    }

    [Fact]
    public async Task Handle_DuplicateWebhook_ReturnsTrue_WithoutAlteringState()
    {
        await using var db = GetInMemoryDbContext();
        var tx = new Transaction("FLW-999", new Money(500, "KES"), "TERM-001");
        tx.MarkAsSettled();
        db.Transactions.Add(tx);
        await db.SaveChangesAsync(CancellationToken.None);

        var mockProvider = new Mock<IFlutterwaveClient>();
        mockProvider.Setup(p => p.VerifyTransactionAsync("FLW-999", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerifyResult(true, 500, "KES"));

        var mockMediator = new Mock<IMediator>();
        var handler = new ProcessWebhookCommandHandler(db, mockProvider.Object, mockMediator.Object);

        var result = await handler.Handle(new ProcessWebhookCommand("FLW-999"), CancellationToken.None);

        result.Should().BeTrue();
        db.ChangeTracker.HasChanges().Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SuccessfulSettlement_PublishesTransactionSettledEvent()
    {
        await using var db = GetInMemoryDbContext();
        var tx = new Transaction("FLW-111", new Money(750, "KES"), "TERM-002");
        db.Transactions.Add(tx);
        await db.SaveChangesAsync(CancellationToken.None);

        var mockProvider = new Mock<IFlutterwaveClient>();
        mockProvider.Setup(p => p.VerifyTransactionAsync("FLW-111", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerifyResult(true, 750, "KES"));

        var mockMediator = new Mock<IMediator>();
        var handler = new ProcessWebhookCommandHandler(db, mockProvider.Object, mockMediator.Object);

        var result = await handler.Handle(new ProcessWebhookCommand("FLW-111"), CancellationToken.None);

        result.Should().BeTrue();

        var dbTx = await db.Transactions.SingleAsync(t => t.ProviderTransactionId == "FLW-111");
        dbTx.Status.Should().Be(TransactionStatus.Settled);

        mockMediator.Verify(m => m.Publish(
            It.Is<TransactionSettledEvent>(e =>
                e.InternalTransactionId == tx.Id &&
                e.TerminalId == "TERM-002" &&
                e.Amount.Amount == 750m &&
                e.Amount.Currency == "KES"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
