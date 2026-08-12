using MyApp.Application.Abstractions;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public sealed class RequirementJournalService(
    IRequirementJournalRepository repository) : IRequirementJournalService
{
    public Task<IReadOnlyList<RequirementJournalEntry>> GetRecentAsync(
        CancellationToken cancellationToken) =>
        repository.GetRecentAsync(cancellationToken);

    public Task<GeneratedPdfDocument?> GetPdfAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        repository.GetPdfAsync(id, cancellationToken);

    public Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);
}
