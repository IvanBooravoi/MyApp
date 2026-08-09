using MyApp.Application.DTO;
using MyApp.Application.Services;

namespace MyApp.API.Endpoints;

public static class ProfessionEndpoints
{
    public static IEndpointRouteBuilder MapProfessionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/professions")
            .RequireAuthorization(policy => policy.RequireRole("administrator"));

        group.MapGet("", async (
            IProfessionService professionService,
            CancellationToken cancellationToken) =>
            Results.Ok(await professionService.GetAllAsync(cancellationToken)));

        group.MapPost("", async (
            ProfessionRequest request,
            IProfessionService professionService,
            CancellationToken cancellationToken) =>
        {
            var result = await professionService.CreateAsync(
                request,
                cancellationToken);
            return result.ToHttpResult(value => Results.Created(
                $"/api/admin/professions/{value.Id}",
                value));
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            ProfessionRequest request,
            IProfessionService professionService,
            CancellationToken cancellationToken) =>
        {
            var result = await professionService.UpdateAsync(
                id,
                request,
                cancellationToken);
            return result.ToHttpResult(Results.Ok);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IProfessionService professionService,
            CancellationToken cancellationToken) =>
        {
            var result = await professionService.DeleteAsync(
                id,
                cancellationToken);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        return endpoints;
    }
}
