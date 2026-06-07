namespace ExpenseTracker.Receipt.Repositories;

public interface IReceiptRepository
{
    Task<Entities.Receipt?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Entities.Receipt receipt, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
