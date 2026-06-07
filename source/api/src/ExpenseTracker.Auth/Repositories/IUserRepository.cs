using ExpenseTracker.Auth.Entities;

namespace ExpenseTracker.Auth.Repositories;

public interface IUserRepository
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IInviteTokenRepository
{
    Task<InviteToken?> FindByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(InviteToken invite, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
