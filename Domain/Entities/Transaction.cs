using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class Transaction
{
    public Guid Id { get; private set; }

    public string ProviderTransactionId { get; private set; } = string.Empty;

    public string TerminalId { get; private set; } = string.Empty;

    public Money Amount { get; private set; } = Money.Zero();

    public TransactionStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? SettledAtUtc { get; private set; }

    public DateTime? RefundedAtUtc { get; private set; }

    // Modern Npgsql Concurrency Token (maps to PostgreSQL xmin)
    public uint Version { get; private set; }

    private Transaction()
    {
    }

    public Transaction(string providerTransactionId, Money amount, string terminalId)
    {
        if (string.IsNullOrWhiteSpace(providerTransactionId))
        {
            throw new ArgumentException("Provider transaction ID is required.", nameof(providerTransactionId));
        }

        if (string.IsNullOrWhiteSpace(terminalId))
        {
            throw new ArgumentException("Terminal ID is required.", nameof(terminalId));
        }

        ArgumentNullException.ThrowIfNull(amount);

        Id = Guid.NewGuid();
        ProviderTransactionId = providerTransactionId.Trim();
        TerminalId = terminalId.Trim();
        Amount = amount;
        Status = TransactionStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsSettled()
    {
        if (Status == TransactionStatus.Settled)
        {
            return;
        }

        if (Status != TransactionStatus.Pending)
        {
            throw new InvalidOperationException("Only pending transactions can be settled.");
        }

        Status = TransactionStatus.Settled;
        SettledAtUtc = DateTime.UtcNow;
    }

    public void MarkAsRefunded()
    {
        if (Status == TransactionStatus.Refunded)
        {
            return;
        }

        if (Status != TransactionStatus.Settled)
        {
            throw new InvalidOperationException("Only settled transactions can be refunded.");
        }

        Status = TransactionStatus.Refunded;
        RefundedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        if (Status == TransactionStatus.Refunded)
        {
            throw new InvalidOperationException("Refunded transactions cannot be marked as failed.");
        }

        if (Status == TransactionStatus.Failed)
        {
            return;
        }

        if (Status != TransactionStatus.Pending)
        {
            throw new InvalidOperationException("Only pending transactions can be failed.");
        }

        Status = TransactionStatus.Failed;
    }
}