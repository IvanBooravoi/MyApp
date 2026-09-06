using MyApp.Application.Abstractions;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public sealed class RequirementJournalService(
    IRequirementJournalRepository repository) : IRequirementJournalService
{
    public Task<IReadOnlyList<RequirementJournalEntry>> GetRecentAsync(
        CancellationToken cancellationToken) =>
        repository.GetRecentAsync(cancellationToken);

    public Task<ComponentDocumentRequest?> GetDocumentAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        repository.GetDocumentRequestAsync(id, cancellationToken);

    public Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);
}
