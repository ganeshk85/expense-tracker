using ExpenseTracker.Api.Data;
using ExpenseTracker.Auth.Entities;
using ExpenseTracker.Auth.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Data.Repositories;

internal sealed class InviteTokenRepository(AppDbContext db) : IInviteTokenRepository
{
    public Task<InviteToken?> FindByTokenAsync(string token, CancellationToken ct = default)
        => db.InviteTokens.FirstOrDefaultAsync(t => t.Token == token, ct);

    public async Task AddAsync(InviteToken invite, CancellationToken ct = default)
        => await db.InviteTokens.AddAsync(invite, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
