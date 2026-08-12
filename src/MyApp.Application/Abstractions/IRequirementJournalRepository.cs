using MyApp.Application.DTO;

namespace MyApp.Application.Abstractions;

public interface IRequirementJournalRepository
{
    Task SaveAsync(
        Guid userId,
        string authorName,
        string issuerName,
        string vehicleNumber,
        string sourceTable,
        IReadOnlyList<ComponentDocumentItem> items,
        GeneratedPdfDocument document,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RequirementJournalEntry>> GetRecentAsync(
        CancellationToken cancellationToken);

    Task<GeneratedPdfDocument?> GetPdfAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
