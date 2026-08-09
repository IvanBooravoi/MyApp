using MyApp.Application.DTO;

namespace MyApp.Application.Abstractions;

public interface ITableViewRepository
{
    Task<TableViewResponse> QueryAsync(
        TableViewRequest request,
        CancellationToken cancellationToken);
}
