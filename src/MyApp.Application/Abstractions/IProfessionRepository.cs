using MyApp.Domain.Entities;

namespace MyApp.Application.Abstractions;

public interface IProfessionRepository
{
    Task<IReadOnlyList<Profession>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<Profession?> FindByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> IsAssignedAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Profession profession, CancellationToken cancellationToken);

    void Remove(Profession profession);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
