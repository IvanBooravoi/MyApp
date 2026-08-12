using MyApp.Application.Services;

namespace MyApp.API.Endpoints;

public static class RequirementJournalEndpoints
{
    public static IEndpointRouteBuilder MapRequirementJournalEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/requirements", async (
            IRequirementJournalService service,
            CancellationToken cancellationToken) =>
        {
            var requirements = await service.GetRecentAsync(cancellationToken);
            return Results.Ok(requirements);
        }).RequireAuthorization();

        endpoints.MapGet("/api/requirements/{id:guid}/pdf", async (
            Guid id,
            IRequirementJournalService service,
            CancellationToken cancellationToken) =>
        {
            var document = await service.GetPdfAsync(id, cancellationToken);
            return document is null ? Results.NotFound() : Results.Ok(document);
        }).RequireAuthorization();

        endpoints.MapDelete("/api/requirements/{id:guid}", async (
            Guid id,
            IRequirementJournalService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var deleted = await service.DeleteAsync(id, cancellationToken);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException exception)
            {
                return Results.Problem(
                    exception.Message,
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Не удалось восстановить остаток");
            }
        }).RequireAuthorization();

        return endpoints;
    }
}
