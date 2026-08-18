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

    Task<ServiceResult<UserProfileResponse>> GetProfileAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ServiceResult<UserProfileResponse>> UpdateProfileAsync(
        Guid id,
        UpdateProfileRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<UserAvatarResponse>> GetAvatarAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ServiceResult<UserProfileResponse>> UpdateAvatarAsync(
        Guid id,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken);

    Task<ServiceResult<UserProfileResponse>> DeleteAvatarAsync(
        Guid id,
        CancellationToken cancellationToken);
}
