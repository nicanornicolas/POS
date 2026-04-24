// -----------------------------------------------------------------------
// <copyright file="LedgerEntry.cs" company="Ikonex Systems">
//     Copyright (c) 2026 Ikonex Systems. All rights reserved.
//     PROPRIETARY AND CONFIDENTIAL.
//     Unauthorized copying of this file, via any medium, is strictly prohibited.
// </copyright>
// -----------------------------------------------------------------------

using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class LedgerEntry
{
    public Guid Id { get; private set; }

    public Guid TransactionId { get; private set; }

    public string AccountCode { get; private set; } = string.Empty;

    public LedgerEntryType EntryType { get; private set; }

    public Money Amount { get; private set; } = Money.Zero();

    public DateTime CreatedAtUtc { get; private set; }

    public string? Narration { get; private set; }

    private LedgerEntry()
    {
    }

    private LedgerEntry(Guid transactionId, string accountCode, LedgerEntryType entryType, Money amount, string? narration)
    {
        if (transactionId == Guid.Empty)
        {
            throw new ArgumentException("Transaction ID is required.", nameof(transactionId));
        }

        if (string.IsNullOrWhiteSpace(accountCode))
        {
            throw new ArgumentException("Account code is required.", nameof(accountCode));
        }

        ArgumentNullException.ThrowIfNull(amount);

        if (amount.Amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Ledger entries require a positive amount.");
        }

        Id = Guid.NewGuid();
        TransactionId = transactionId;
        AccountCode = accountCode.Trim();
        EntryType = entryType;
        Amount = amount;
        Narration = string.IsNullOrWhiteSpace(narration) ? null : narration.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static LedgerEntry Debit(Guid transactionId, string accountCode, Money amount, string? narration = null)
        => new(transactionId, accountCode, LedgerEntryType.Debit, amount, narration);

    public static LedgerEntry Credit(Guid transactionId, string accountCode, Money amount, string? narration = null)
        => new(transactionId, accountCode, LedgerEntryType.Credit, amount, narration);
}
