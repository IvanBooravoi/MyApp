using MyApp.Application.DTO;
using MyApp.Application.Services;

namespace MyApp.API.Endpoints;

public static class MaintenanceTemplateEndpoints
{
    public static IEndpointRouteBuilder MapMaintenanceTemplateEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/maintenance-templates", async (
            IMaintenanceTemplateService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAllAsync(cancellationToken)))
            .RequireAuthorization();

        var admin = endpoints.MapGroup("/api/admin/maintenance-templates")
            .RequireAuthorization(policy => policy.RequireRole("administrator"));

        admin.MapPost("/equipment", async (
            MaintenanceEquipmentRequest request,
            IMaintenanceTemplateService service,
            CancellationToken cancellationToken) =>
            (await service.CreateEquipmentAsync(request, cancellationToken))
                .ToHttpResult(id => Results.Created(
                    $"/api/admin/maintenance-templates/equipment/{id}",
                    new { id })));

        admin.MapPut("/equipment/{id:guid}", async (
            Guid id,
            MaintenanceEquipmentRequest request,
            IMaintenanceTemplateService service,
            CancellationToken cancellationToken) =>
            (await service.RenameEquipmentAsync(id, request, cancellationToken))
                .ToHttpResult(_ => Results.NoContent()));

        admin.MapDelete("/equipment/{id:guid}", async (
            Guid id,
            IMaintenanceTemplateService service,
            CancellationToken cancellationToken) =>
            (await service.DeleteEquipmentAsync(id, cancellationToken))
                .ToHttpResult(_ => Results.NoContent()));

        admin.MapPost("/equipment/{equipmentId:guid}/intervals", async (
            Guid equipmentId,
            MaintenanceIntervalRequest request,
            IMaintenanceTemplateService service,
            CancellationToken cancellationToken) =>
            (await service.CreateIntervalAsync(
                equipmentId, request, cancellationToken))
                .ToHttpResult(id => Results.Created(
                    $"/api/admin/maintenance-templates/intervals/{id}",
                    new { id })));

        admin.MapPut("/intervals/{id:guid}", async (
            Guid id,
            MaintenanceIntervalRequest request,
            IMaintenanceTemplateService service,
            CancellationToken cancellationToken) =>
            (await service.RenameIntervalAsync(id, request, cancellationToken))
                .ToHttpResult(_ => Results.NoContent()));

        admin.MapDelete("/intervals/{id:guid}", async (
            Guid id,
            IMaintenanceTemplateService service,
            CancellationToken cancellationToken) =>
            (await service.DeleteIntervalAsync(id, cancellationToken))
                .ToHttpResult(_ => Results.NoContent()));

        admin.MapPost("/intervals/{intervalId:guid}/items", async (
            Guid intervalId,
            MaintenanceItemRequest request,
            IMaintenanceTemplateService service,
            CancellationToken cancellationToken) =>
            (await service.AddItemAsync(intervalId, request, cancellationToken))
                .ToHttpResult(id => Results.Created(
                    $"/api/admin/maintenance-templates/items/{id}",
                    new { id })));

        admin.MapPut("/items/{id:guid}", async (
            Guid id,
            MaintenanceItemRequest request,
            IMaintenanceTemplateService service,
            CancellationToken cancellationToken) =>
            (await service.UpdateItemAsync(id, request, cancellationToken))
                .ToHttpResult(_ => Results.NoContent()));

        admin.MapDelete("/items/{id:guid}", async (
            Guid id,
            IMaintenanceTemplateService service,
            CancellationToken cancellationToken) =>
            (await service.DeleteItemAsync(id, cancellationToken))
                .ToHttpResult(_ => Results.NoContent()));

        return endpoints;
    }
}
