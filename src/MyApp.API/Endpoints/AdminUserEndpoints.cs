using MyApp.Application.DTO;
using MyApp.Application.Services;

namespace MyApp.API.Endpoints;

public static class AdminUserEndpoints
{
    public static IEndpointRouteBuilder MapAdminUserEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/users")
            .RequireAuthorization(policy => policy.RequireRole("administrator"));

        group.MapGet("", async (
            IUserService userService,
            CancellationToken cancellationToken) =>
            Results.Ok(await userService.GetAllAsync(cancellationToken)));

        group.MapPost("", async (
            CreateUserRequest request,
            IUserService userService,
            CancellationToken cancellationToken) =>
        {
            var result = await userService.CreateAsync(request, cancellationToken);
            return result.ToHttpResult(value => Results.Created(
                $"/api/admin/users/{value.Id}",
                value));
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateUserRequest request,
            IUserService userService,
            CancellationToken cancellationToken) =>
        {
            var result = await userService.UpdateAsync(
                id,
                request,
                cancellationToken);
            return result.ToHttpResult(Results.Ok);
        });

        return endpoints;
    }
}
