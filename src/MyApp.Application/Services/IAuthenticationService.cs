using MyApp.Application.Common;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public interface IAuthenticationService
{
    Task<ServiceResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);
}
