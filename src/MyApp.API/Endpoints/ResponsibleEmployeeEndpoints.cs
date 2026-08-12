using MyApp.Application.Services;

namespace MyApp.API.Endpoints;

public static class ResponsibleEmployeeEndpoints
{
    public static IEndpointRouteBuilder MapResponsibleEmployeeEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/employees/responsible", async (
            string sourceTable,
            IResponsibleEmployeeService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetAsync(sourceTable, cancellationToken);
            return result.ToHttpResult(Results.Ok);
        }).RequireAuthorization();

        return endpoints;
    }
}
