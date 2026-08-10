using MyApp.Application.Common;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public interface IComponentDocumentService
{
    ServiceResult<ComponentDocumentsResponse> Generate(
        ComponentDocumentRequest request);
}
