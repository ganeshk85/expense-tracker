using ExpenseTracker.Api.Data;
using ExpenseTracker.Auth.Entities;
using ExpenseTracker.Auth.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Data.Repositories;

internal sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => db.Users.FindAsync([id], ct).AsTask();

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await db.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
