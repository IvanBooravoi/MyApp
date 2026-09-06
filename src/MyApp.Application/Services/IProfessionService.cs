using MyApp.Application.Common;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public interface IProfessionService
{
    Task<IReadOnlyList<ProfessionResponse>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<ServiceResult<ProfessionResponse>> CreateAsync(
        ProfessionRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProfessionResponse>> UpdateAsync(
        Guid id,
        ProfessionRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken);
}
