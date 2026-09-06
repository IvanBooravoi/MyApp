using MyApp.Application.Common;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public interface IResponsibleEmployeeService
{
    Task<ServiceResult<IReadOnlyList<ResponsibleEmployeeResponse>>> GetAsync(
        string sourceTable,
        CancellationToken cancellationToken);

    Task<ServiceResult<ResponsibleEmployeeResponse>> ResolveAsync(
        string sourceTable,
        ResponsibleEmployeeSelection employee,
        CancellationToken cancellationToken);
}
