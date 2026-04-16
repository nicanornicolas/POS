using Domain.ValueObjects;
using FluentAssertions;

namespace Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Ctor_NormalizesCurrencyAndAcceptsZeroAmount()
    {
        var money = new Money(0m, "kes");

        money.Amount.Should().Be(0m);
        money.Currency.Should().Be("KES");
    }

    [Fact]
    public void Ctor_ThrowsForNegativeAmount()
    {
        Action act = () => new Money(-1m, "KES");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Subtract_ThrowsWhenResultWouldBeNegative()
    {
        var money = new Money(10m, "KES");

        Action act = () => money.Subtract(new Money(11m, "KES"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Resulting amount cannot be negative.");
    }
}