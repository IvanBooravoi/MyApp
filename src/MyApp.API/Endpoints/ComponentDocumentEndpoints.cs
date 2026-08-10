using MyApp.Application.DTO;
using MyApp.Application.Services;

namespace MyApp.API.Endpoints;

public static class ComponentDocumentEndpoints
{
    public static IEndpointRouteBuilder MapComponentDocumentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/documents/components", (
            ComponentDocumentRequest request,
            IComponentDocumentService documentService) =>
        {
            try
            {
                var result = documentService.Generate(request);
                return result.ToHttpResult(Results.Ok);
            }
            catch (FileNotFoundException exception)
            {
                return Results.Problem(
                    exception.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Файл для формирования PDF не найден");
            }
            catch (InvalidDataException exception)
            {
                return Results.Problem(
                    exception.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Некорректный PDF-шаблон");
            }
        }).RequireAuthorization();

        return endpoints;
    }
}
