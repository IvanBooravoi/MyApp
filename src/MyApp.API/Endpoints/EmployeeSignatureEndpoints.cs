using MyApp.Application.DTO;
using MyApp.Application.Services;

namespace MyApp.API.Endpoints;

public static class EmployeeSignatureEndpoints
{
    private const long MaximumSignatureSize = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    public static IEndpointRouteBuilder MapEmployeeSignatureEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/employees/signature")
            .RequireAuthorization(policy => policy.RequireRole("administrator"));

        group.MapGet("/", async (
            string lastName,
            string firstName,
            string? patronymic,
            IEmployeeSignatureService service,
            CancellationToken cancellationToken) =>
            (await service.GetAsync(
                    CreateKey(lastName, firstName, patronymic),
                    cancellationToken))
                .ToHttpResult(signature => Results.File(
                    signature.Content,
                    signature.ContentType,
                    enableRangeProcessing: false)));

        group.MapPost("/", async (
            IFormFile signature,
            string lastName,
            string firstName,
            string? patronymic,
            IEmployeeSignatureService service,
            CancellationToken cancellationToken) =>
        {
            if (signature.Length is <= 0 or > MaximumSignatureSize ||
                !AllowedContentTypes.Contains(signature.ContentType))
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["signature"] =
                        [
                            "Выберите изображение JPEG, PNG или WebP размером не более 2 МБ."
                        ]
                    });
            }

            await using var stream = new MemoryStream();
            await signature.CopyToAsync(stream, cancellationToken);
            return (await service.SaveAsync(
                    CreateKey(lastName, firstName, patronymic),
                    stream.ToArray(),
                    signature.ContentType,
                    cancellationToken))
                .ToHttpResult(Results.Ok);
        }).DisableAntiforgery();

        group.MapDelete("/", async (
            string lastName,
            string firstName,
            string? patronymic,
            IEmployeeSignatureService service,
            CancellationToken cancellationToken) =>
            (await service.DeleteAsync(
                    CreateKey(lastName, firstName, patronymic),
                    cancellationToken))
                .ToHttpResult(Results.Ok));

        return endpoints;
    }

    private static EmployeeSignatureKey CreateKey(
        string lastName,
        string firstName,
        string? patronymic) =>
        new(lastName, firstName, patronymic ?? string.Empty);
}
