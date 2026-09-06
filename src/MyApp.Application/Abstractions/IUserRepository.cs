using MyApp.Domain.Entities;

namespace MyApp.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> FindByLoginAsync(
        string login,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> UserNameExistsAsync(
        string userName,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
