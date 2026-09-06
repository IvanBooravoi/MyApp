using MyApp.Application.Services;
using Npgsql;

namespace MyApp.API.Endpoints;

public static class CsvFileEndpoints
{
    private const long MaximumCsvSize = 20 * 1024 * 1024;

    public static IEndpointRouteBuilder MapCsvFileEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/csv-files")
            .RequireAuthorization(policy => policy.RequireRole("administrator"));

        group.MapGet("/{fileName}", async (
            string fileName,
            ICsvFileService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return (await service.GetAsync(fileName, cancellationToken))
                    .ToHttpResult(content => Results.File(
                        content,
                        "text/csv",
                        fileName));
            }
            catch (InvalidOperationException exception)
            {
                return Results.Problem(
                    exception.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Не удалось прочитать CSV-файл");
            }
            catch (PostgresException exception)
            {
                return Results.Problem(
                    exception.MessageText,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "PostgreSQL не разрешил прочитать CSV-файл");
            }
        });

        group.MapPost("/{fileName}", async (
            string fileName,
            IFormFile file,
            ICsvFileService service,
            CancellationToken cancellationToken) =>
        {
            if (file.Length is <= 0 or > MaximumCsvSize ||
                !string.Equals(
                    Path.GetExtension(file.FileName),
                    ".csv",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["file"] =
                        [
                            "Выберите CSV-файл размером не более 20 МБ."
                        ]
                    });
            }

            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            try
            {
                return (await service.ReplaceAsync(
                        fileName,
                        stream.ToArray(),
                        cancellationToken))
                    .ToHttpResult(Results.Ok);
            }
            catch (InvalidOperationException exception)
            {
                return Results.Problem(
                    exception.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Не удалось заменить CSV-файл");
            }
            catch (PostgresException exception)
            {
                return Results.Problem(
                    exception.MessageText,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "PostgreSQL не разрешил заменить CSV-файл");
            }
        }).DisableAntiforgery();

        return endpoints;
    }
}
