using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using MyApp.Application.DTO;
using MyApp.Application.Services;

namespace MyApp.API.Endpoints;

public static class ComponentDocumentEndpoints
{
    public static IEndpointRouteBuilder MapComponentDocumentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/documents/components/preview", async (
            ComponentDocumentRequest request,
            IComponentDocumentService documentService,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await documentService.PrepareAsync(
                request,
                userId,
                cancellationToken);
            return result.ToHttpResult(Results.Ok);
        }).RequireAuthorization();

        endpoints.MapPost("/api/documents/components", async (
            ComponentDocumentRequest request,
            IComponentDocumentService documentService,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await documentService.GenerateAsync(
                request,
                userId,
                cancellationToken);
            return result.ToHttpResult(Results.Ok);
        }).RequireAuthorization();

        endpoints.MapGet("/api/documents/components/template", (
            IConfiguration configuration) =>
        {
            var templatePath = configuration.GetValue<string>(
                "DocumentTemplates:ComponentIssuePath");
            if (string.IsNullOrWhiteSpace(templatePath) ||
                !File.Exists(templatePath))
            {
                return Results.NotFound();
            }

            return Results.File(
                templatePath,
                "application/pdf",
                enableRangeProcessing: false);
        }).RequireAuthorization();

        return endpoints;
    }

    private static bool TryGetUserId(
        ClaimsPrincipal principal,
        out Guid userId)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdValue, out userId);
    }
}
