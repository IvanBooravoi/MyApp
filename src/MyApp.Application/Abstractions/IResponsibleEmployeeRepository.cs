using MyApp.Application.DTO;

namespace MyApp.Application.Abstractions;

public interface IResponsibleEmployeeRepository
{
    Task<IReadOnlyList<ResponsibleEmployeeResponse>> GetByProfessionAsync(
        string profession,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        ResponsibleEmployeeSelection employee,
        string profession,
        CancellationToken cancellationToken);
}
