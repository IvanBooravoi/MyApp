using MyApp.Application.Common;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public interface IEmployeeSignatureService
{
    Task<ServiceResult<EmployeeSignatureResponse>> GetAsync(
        EmployeeSignatureKey employee,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> SaveAsync(
        EmployeeSignatureKey employee,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteAsync(
        EmployeeSignatureKey employee,
        CancellationToken cancellationToken);
}
