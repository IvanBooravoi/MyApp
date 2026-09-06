using MyApp.Application.Common;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public interface ITableViewService
{
    Task<ServiceResult<TableViewResponse>> GetAsync(
        TableViewRequest request,
        CancellationToken cancellationToken);
}
