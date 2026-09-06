using MyApp.Application.DTO;

namespace MyApp.Application.Abstractions;

public interface IEmployeeSignatureRepository
{
    Task<EmployeeSignatureResponse?> GetAsync(
        EmployeeSignatureKey employee,
        CancellationToken cancellationToken);

    Task SaveAsync(
        EmployeeSignatureKey employee,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        EmployeeSignatureKey employee,
        CancellationToken cancellationToken);
}
