using MyApp.Application.Common;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public interface IComponentDocumentService
{
    Task<ServiceResult<ComponentDocumentsResponse>> GenerateAsync(
        ComponentDocumentRequest request,
        Guid userId,
        CancellationToken cancellationToken);
}
