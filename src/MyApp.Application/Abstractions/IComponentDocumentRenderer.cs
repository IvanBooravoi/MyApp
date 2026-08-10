using MyApp.Application.DTO;

namespace MyApp.Application.Abstractions;

public interface IComponentDocumentRenderer
{
    IReadOnlyList<GeneratedPdfDocument> Render(ComponentDocumentRequest request);
}
