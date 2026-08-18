using MyApp.Application.DTO;
using MyApp.Application.Services;

namespace MyApp.API.Endpoints;

public static class MaterialGroupEndpoints
{
    public static IEndpointRouteBuilder MapMaterialGroupEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/material-groups/map", async (
            string sourceTable,
            IMaterialGroupService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetMappingsAsync(
                sourceTable,
                cancellationToken);
            return result.ToHttpResult(Results.Ok);
        }).RequireAuthorization();

        var admin = endpoints.MapGroup("/api/admin/material-groups")
            .RequireAuthorization(policy => policy.RequireRole("administrator"));

        admin.MapGet("", async (
            IMaterialGroupService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAllAsync(cancellationToken)));

        admin.MapPost("", async (
            MaterialGroupRequest request,
            IMaterialGroupService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CreateGroupAsync(request, cancellationToken);
            return result.ToHttpResult(id => Results.Created(
                $"/api/admin/material-groups/{id}",
                new { id }));
        });

        admin.MapPost("/{groupId:guid}/items", async (
            Guid groupId,
            MaterialGroupItemRequest request,
            IMaterialGroupService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.AddItemAsync(
                groupId,
                request,
                cancellationToken);
            return result.ToHttpResult(id => Results.Created(
                $"/api/admin/material-groups/{groupId}/items/{id}",
                new { id }));
        });

        admin.MapDelete("/{id:guid}", async (
            Guid id,
            IMaterialGroupService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.DeleteGroupAsync(id, cancellationToken);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        admin.MapDelete("/items/{id:guid}", async (
            Guid id,
            IMaterialGroupService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.DeleteItemAsync(id, cancellationToken);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        return endpoints;
    }
}
