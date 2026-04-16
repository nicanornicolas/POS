using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Transaction entity
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).ValueGeneratedNever();

            entity.Property(t => t.ProviderTransactionId)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(t => t.ProviderTransactionId)
                .IsUnique()
                .HasDatabaseName("IX_Transaction_ProviderTransactionId");

            // Configure Money value object as owned type
            entity.OwnsOne(t => t.Amount, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnType("decimal(18,4)")
                    .HasColumnName("Amount")
                    .IsRequired();

                money.Property(m => m.Currency)
                    .HasMaxLength(3)
                    .HasColumnName("Currency")
                    .IsRequired();
            });

            entity.Property(t => t.Status)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(t => t.CreatedAtUtc)
                .IsRequired()
                .HasDefaultValue(DateTime.UtcNow);

            entity.Property(t => t.SettledAtUtc)
                .IsRequired(false);

            entity.Property(t => t.RefundedAtUtc)
                .IsRequired(false);

            // Concurrency token for idempotency
            entity.Property<byte[]>("RowVersion")
                .IsRowVersion();
        });

        // Configure LedgerEntry entity
        modelBuilder.Entity<LedgerEntry>(entity =>
        {
            entity.HasKey(le => le.Id);
            entity.Property(le => le.Id).ValueGeneratedNever();

            entity.Property(le => le.TransactionId)
                .IsRequired();

            entity.Property(le => le.AccountCode)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(le => le.EntryType)
                .IsRequired()
                .HasConversion<int>();

            // Configure Money value object as owned type
            entity.OwnsOne(le => le.Amount, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnType("decimal(18,4)")
                    .HasColumnName("Amount")
                    .IsRequired();

                money.Property(m => m.Currency)
                    .HasMaxLength(3)
                    .HasColumnName("Currency")
                    .IsRequired();
            });

            entity.Property(le => le.CreatedAtUtc)
                .IsRequired()
                .HasDefaultValue(DateTime.UtcNow);

            entity.Property(le => le.Narration)
                .IsRequired(false)
                .HasMaxLength(500);

            entity.HasIndex(le => le.TransactionId)
                .HasDatabaseName("IX_LedgerEntry_TransactionId");

            entity.HasIndex(le => new { le.TransactionId, le.AccountCode })
                .HasDatabaseName("IX_LedgerEntry_TransactionId_AccountCode");
        });
    }
}
