using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;

namespace Tests.Domain;

public class TransactionTests
{
    [Fact]
    public void MarkAsSettled_IsIdempotent()
    {
        var transaction = new Transaction("FLW-123", new Money(100m, "KES"));

        transaction.MarkAsSettled();
        transaction.MarkAsSettled();

        transaction.Status.Should().Be(TransactionStatus.Settled);
    }

    [Fact]
    public void MarkAsRefunded_RequiresSettledTransaction()
    {
        var transaction = new Transaction("FLW-123", new Money(100m, "KES"));

        Action act = () => transaction.MarkAsRefunded();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only settled transactions can be refunded.");
    }

    [Fact]
    public void MarkAsRefunded_IsIdempotentAfterRefund()
    {
        var transaction = new Transaction("FLW-123", new Money(100m, "KES"));
        transaction.MarkAsSettled();
        transaction.MarkAsRefunded();

        transaction.MarkAsRefunded();

        transaction.Status.Should().Be(TransactionStatus.Refunded);
    }
}