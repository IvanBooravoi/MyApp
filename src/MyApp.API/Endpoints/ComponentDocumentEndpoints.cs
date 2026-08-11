using MyApp.Application.DTO;
using MyApp.Application.Services;
using PdfSharp.Pdf.IO;

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
            catch (PdfReaderException exception)
            {
                return Results.Problem(
                    exception.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Не удалось прочитать PDF-шаблон");
            }
            catch (UnauthorizedAccessException exception)
            {
                return Results.Problem(
                    exception.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Нет доступа к PDF-шаблону");
            }
            catch (IOException exception)
            {
                return Results.Problem(
                    exception.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Ошибка чтения или записи PDF");
            }
        }).RequireAuthorization();

        return endpoints;
    }
}
