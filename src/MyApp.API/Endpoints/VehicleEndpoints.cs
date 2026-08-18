using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MyApp.Application.DTO;
using MyApp.Application.Services;

namespace MyApp.API.Endpoints;

public static class VehicleEndpoints
{
    private const long MaximumHoursImportSize = 5 * 1024 * 1024;

    public static IEndpointRouteBuilder MapVehicleEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/vehicles").RequireAuthorization();

        group.MapGet("/", async (
            IVehicleService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetVehiclesAsync(cancellationToken)));

        group.MapGet("/{vehicleId:guid}/journal", async (
            Guid vehicleId,
            DateOnly? from,
            DateOnly? to,
            IVehicleService service,
            CancellationToken cancellationToken) =>
        {
            var journal = await service.GetJournalAsync(
                vehicleId, from, to, cancellationToken);
            return journal is null ? Results.NotFound() : Results.Ok(journal);
        });

        group.MapPost("/{vehicleId:guid}/purchases", async (
            Guid vehicleId,
            VehiclePurchaseRequest request,
            ClaimsPrincipal principal,
            IVehicleService service,
            CancellationToken cancellationToken) =>
            await CreateAsync(
                principal,
                userId => service.AddPurchaseAsync(
                    vehicleId, request, userId, cancellationToken),
                $"/api/vehicles/{vehicleId}/journal"));

        group.MapPost("/{vehicleId:guid}/defects", async (
            Guid vehicleId,
            VehicleDefectRequest request,
            ClaimsPrincipal principal,
            IVehicleService service,
            CancellationToken cancellationToken) =>
            await CreateAsync(
                principal,
                userId => service.AddDefectAsync(
                    vehicleId, request, userId, cancellationToken),
                $"/api/vehicles/{vehicleId}/journal"));

        group.MapPost("/defects/{defectId:guid}/photos", async (
            Guid defectId,
            IFormFile file,
            IVehicleService service,
            CancellationToken cancellationToken) =>
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            var result = await service.AddDefectPhotosAsync(
                defectId,
                [
                    new VehicleWorkPhotoUpload(
                        Path.GetFileName(file.FileName),
                        file.ContentType,
                        stream.ToArray())
                ],
                cancellationToken);
            return result.ToHttpResult(ids => Results.Created(
                $"/api/vehicles/defect-photos/{ids[0]}",
                new { id = ids[0] }));
        }).DisableAntiforgery();

        group.MapGet("/defect-photos/{photoId:guid}", async (
            Guid photoId,
            IVehicleService service,
            CancellationToken cancellationToken) =>
        {
            var photo = await service.GetDefectPhotoAsync(
                photoId, cancellationToken);
            return photo is null
                ? Results.NotFound()
                : Results.File(
                    photo.Content,
                    photo.ContentType,
                    photo.FileName,
                    enableRangeProcessing: true);
        });

        group.MapDelete("/defect-photos/{photoId:guid}", async (
            Guid photoId,
            IVehicleService service,
            CancellationToken cancellationToken) =>
            await service.DeleteDefectPhotoAsync(photoId, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());

        group.MapPost("/{vehicleId:guid}/hours", async (
            Guid vehicleId,
            VehicleHoursRequest request,
            ClaimsPrincipal principal,
            IVehicleService service,
            CancellationToken cancellationToken) =>
            await CreateAsync(
                principal,
                userId => service.AddHoursAsync(
                    vehicleId, request, userId, cancellationToken),
                $"/api/vehicles/{vehicleId}/journal"));

        group.MapPost("/hours/import", async (
            IFormFile file,
            ClaimsPrincipal principal,
            IVehicleService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }
            var extension = Path.GetExtension(file.FileName);
            if (file.Length is <= 0 or > MaximumHoursImportSize ||
                !string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["file"] = ["Выберите файл CSV или XLSX размером не более 5 МБ."]
                    });
            }

            try
            {
                await using var stream = file.OpenReadStream();
                return Results.Ok(await service.ImportHoursAsync(
                    stream,
                    extension,
                    userId,
                    DateOnly.FromDateTime(DateTime.Today),
                    cancellationToken));
            }
            catch (DecoderFallbackException)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["file"] = ["CSV-файл должен быть сохранён в кодировке UTF-8."]
                    });
            }
        }).DisableAntiforgery();

        group.MapPost("/{vehicleId:guid}/works", async (
            Guid vehicleId,
            VehicleWorkRequest request,
            ClaimsPrincipal principal,
            IVehicleService service,
            CancellationToken cancellationToken) =>
            await CreateAsync(
                principal,
                userId => service.AddWorkAsync(
                    vehicleId, request, userId, cancellationToken),
                $"/api/vehicles/{vehicleId}/journal"));

        group.MapPost("/works/{workId:guid}/photos", async (
            Guid workId,
            IFormFile file,
            IVehicleService service,
            CancellationToken cancellationToken) =>
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            var result = await service.AddWorkPhotosAsync(
                workId,
                [
                    new VehicleWorkPhotoUpload(
                        Path.GetFileName(file.FileName),
                        file.ContentType,
                        stream.ToArray())
                ],
                cancellationToken);
            return result.ToHttpResult(ids => Results.Created(
                $"/api/vehicles/work-photos/{ids[0]}",
                new { id = ids[0] }));
        }).DisableAntiforgery();

        group.MapGet("/work-photos/{photoId:guid}", async (
            Guid photoId,
            IVehicleService service,
            CancellationToken cancellationToken) =>
        {
            var photo = await service.GetWorkPhotoAsync(
                photoId, cancellationToken);
            return photo is null
                ? Results.NotFound()
                : Results.File(
                    photo.Content,
                    photo.ContentType,
                    photo.FileName,
                    enableRangeProcessing: true);
        });

        group.MapDelete("/work-photos/{photoId:guid}", async (
            Guid photoId,
            IVehicleService service,
            CancellationToken cancellationToken) =>
            await service.DeleteWorkPhotoAsync(photoId, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());

        group.MapDelete("/{category}/{id:guid}", async (
            string category,
            Guid id,
            IVehicleService service,
            CancellationToken cancellationToken) =>
            await service.DeleteEntryAsync(category, id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        ClaimsPrincipal principal,
        Func<Guid, Task<MyApp.Application.Common.ServiceResult<Guid>>> create,
        string location)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Results.Unauthorized();
        }
        return (await create(userId)).ToHttpResult(
            id => Results.Created(location, new { id }));
    }

    private static bool TryGetUserId(
        ClaimsPrincipal principal,
        out Guid userId)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out userId);
    }
}
