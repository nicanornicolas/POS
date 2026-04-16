using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Transaction> Transactions { get; }

    DbSet<LedgerEntry> LedgerEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
