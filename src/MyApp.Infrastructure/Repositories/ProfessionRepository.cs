using Microsoft.EntityFrameworkCore;
using MyApp.Application.Abstractions;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Db;

namespace MyApp.Infrastructure.Repositories;

public sealed class ProfessionRepository(AppDbContext db) : IProfessionRepository
{
    public async Task<IReadOnlyList<Profession>> GetAllAsync(
        CancellationToken cancellationToken) =>
        await db.Professions
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .ToArrayAsync(cancellationToken);

    public Task<Profession?> FindByIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        db.Professions.FindAsync([id], cancellationToken).AsTask();

    public Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        db.Professions.AnyAsync(
            profession => profession.Id == id,
            cancellationToken);

    public Task<bool> IsAssignedAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        db.Users.AnyAsync(user => user.PositionId == id, cancellationToken);

    public async Task AddAsync(
        Profession profession,
        CancellationToken cancellationToken) =>
        await db.Professions.AddAsync(profession, cancellationToken);

    public void Remove(Profession profession) => db.Professions.Remove(profession);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await db.SaveChangesAsync(cancellationToken);
}
