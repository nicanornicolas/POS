using Application.Commands;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Tests;

public class ProcessChargeCommandTests
{
    private static AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_SuccessfulCharge_CreatesPendingTransaction_ReturnsId()
    {
        await using var db = GetInMemoryDbContext();

        var mockProvider = new Mock<IFlutterwaveClient>();
        mockProvider.Setup(p => p.ProcessChargeAsync(
                It.Is<ChargeRequest>(r => r.Amount == 1000m && r.Currency == "KES" && r.PaymentToken == "flw-t1nf-test"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChargeResult(true, "FLW-CHARGE-123"));

        var handler = new ProcessChargeCommandHandler(db, mockProvider.Object);

        var result = await handler.Handle(
            new ProcessChargeCommand(1000m, "KES", "flw-t1nf-test", "TERM-003"),
            CancellationToken.None);

        result.Should().NotBe(Guid.Empty);

        var tx = await db.Transactions.FindAsync(result);
        tx.Should().NotBeNull();
        tx!.Status.Should().Be(TransactionStatus.Pending);
        tx.ProviderTransactionId.Should().Be("FLW-CHARGE-123");
        tx.TerminalId.Should().Be("TERM-003");
        tx.Amount.Should().BeEquivalentTo(new Money(1000m, "KES"));
    }

    [Fact]
    public async Task Handle_FailedCharge_ThrowsProviderOperationFailedException()
    {
        await using var db = GetInMemoryDbContext();

        var mockProvider = new Mock<IFlutterwaveClient>();
        mockProvider.Setup(p => p.ProcessChargeAsync(It.IsAny<ChargeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChargeResult(false, "", "Insufficient funds"));

        var handler = new ProcessChargeCommandHandler(db, mockProvider.Object);

        var act = async () => await handler.Handle(
            new ProcessChargeCommand(500m, "KES", "flw-t1nf-fail", "TERM-004"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ProviderOperationFailedException>();

        db.Transactions.Should().BeEmpty();
    }
}
