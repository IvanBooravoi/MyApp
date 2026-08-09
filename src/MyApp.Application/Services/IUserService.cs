using MyApp.Application.Common;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserListItem>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<ServiceResult<CreateUserResponse>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<UpdateUserResponse>> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken);
}
