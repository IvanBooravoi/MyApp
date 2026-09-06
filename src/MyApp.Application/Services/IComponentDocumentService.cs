using MyApp.Application.Common;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public interface IComponentDocumentService
{
    Task<ServiceResult<ComponentDocumentResponse>> PrepareAsync(
        ComponentDocumentRequest request,
        Guid userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<ComponentDocumentResponse>> GenerateAsync(
        ComponentDocumentRequest request,
        Guid userId,
        CancellationToken cancellationToken);
}
