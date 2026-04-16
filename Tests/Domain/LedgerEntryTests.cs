using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;

namespace Tests.Domain;

public class LedgerEntryTests
{
    [Fact]
    public void Debit_CreatesPositiveLedgerEntry()
    {
        var transactionId = Guid.NewGuid();
        var entry = LedgerEntry.Debit(transactionId, "MerchantEscrow", new Money(50m, "KES"), "Refund reversal");

        entry.TransactionId.Should().Be(transactionId);
        entry.EntryType.Should().Be(LedgerEntryType.Debit);
        entry.Amount.Amount.Should().Be(50m);
        entry.Amount.Currency.Should().Be("KES");
        entry.AccountCode.Should().Be("MerchantEscrow");
        entry.Narration.Should().Be("Refund reversal");
    }

    [Fact]
    public void Credit_ThrowsForZeroAmount()
    {
        var transactionId = Guid.NewGuid();

        Action act = () => LedgerEntry.Credit(transactionId, "CustomerAccount", Money.Zero("KES"));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}