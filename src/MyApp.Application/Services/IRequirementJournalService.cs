using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public interface IRequirementJournalService
{
    Task<IReadOnlyList<RequirementJournalEntry>> GetRecentAsync(
        CancellationToken cancellationToken);

    Task<ComponentDocumentRequest?> GetDocumentAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
