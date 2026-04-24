using Application.Commands;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Tests;

public class RefundHandlerTests
{
    private static AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_PendingTransaction_ThrowsInvalidTransactionStateException()
    {
        await using var db = GetInMemoryDbContext();
        var tx = new Transaction("FLW-123", new Money(100, "KES"), "TERM-001");
        db.Transactions.Add(tx);
        await db.SaveChangesAsync(CancellationToken.None);

        var mockProvider = new Mock<IFlutterwaveClient>();
        var handler = new ProcessRefundCommandHandler(db, mockProvider.Object);

        var act = async () => await handler.Handle(new ProcessRefundCommand(tx.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidTransactionStateException>();
        mockProvider.Verify(p => p.ProcessRefundAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyRefundedTransaction_ReturnsTrue_WithoutCallingProvider()
    {
        await using var db = GetInMemoryDbContext();
        var tx = new Transaction("FLW-456", new Money(100, "KES"), "TERM-001");
        tx.MarkAsSettled();
        tx.MarkAsRefunded();
        db.Transactions.Add(tx);
        await db.SaveChangesAsync(CancellationToken.None);

        var mockProvider = new Mock<IFlutterwaveClient>();
        var handler = new ProcessRefundCommandHandler(db, mockProvider.Object);

        var result = await handler.Handle(new ProcessRefundCommand(tx.Id), CancellationToken.None);

        result.Should().BeTrue();
        mockProvider.Verify(p => p.ProcessRefundAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
