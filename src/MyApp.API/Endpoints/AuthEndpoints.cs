using MyApp.Application.DTO;
using MyApp.Application.Services;

namespace MyApp.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/login", async (
            LoginRequest request,
            IAuthenticationService authenticationService,
            CancellationToken cancellationToken) =>
        {
            var result = await authenticationService.LoginAsync(
                request,
                cancellationToken);
            return result.ToHttpResult(Results.Ok);
        });

        return endpoints;
    }
}
