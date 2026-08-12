using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public interface IRequirementJournalService
{
    Task<IReadOnlyList<RequirementJournalEntry>> GetRecentAsync(
        CancellationToken cancellationToken);

    Task<GeneratedPdfDocument?> GetPdfAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
