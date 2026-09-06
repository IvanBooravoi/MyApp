using MyApp.Application.DTO;

namespace MyApp.Application.Abstractions;

public interface IRequirementJournalRepository
{
    Task SaveAsync(
        Guid userId,
        string authorName,
        string issuerName,
        ComponentDocumentRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RequirementJournalEntry>> GetRecentAsync(
        CancellationToken cancellationToken);

    Task<ComponentDocumentRequest?> GetDocumentRequestAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
