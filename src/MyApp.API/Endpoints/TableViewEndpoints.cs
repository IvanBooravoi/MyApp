using System.Security.Claims;
using MyApp.Application.DTO;
using MyApp.Application.Services;

namespace MyApp.API.Endpoints;

public static class TableViewEndpoints
{
    public static IEndpointRouteBuilder MapTableViewEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/tables/{tableName}", async (
            string tableName,
            ITableViewService tableViewService,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken,
            int page = 1,
            string pageSize = "20",
            string? search = null,
            string? lastName = null,
            string? firstName = null,
            string? patronymic = null,
            string? profession = null) =>
        {
            var query = new TableViewRequest(
                tableName,
                page,
                pageSize,
                search,
                lastName,
                firstName,
                patronymic,
                profession,
                principal.IsInRole("administrator"));
            var result = await tableViewService.GetAsync(
                query,
                cancellationToken);
            return result.ToHttpResult(Results.Ok);
        }).RequireAuthorization();

        return endpoints;
    }
}
