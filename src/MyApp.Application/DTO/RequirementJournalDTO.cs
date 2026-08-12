namespace MyApp.Application.DTO;

public sealed record RequirementJournalEntry(
    Guid Id,
    DateTimeOffset CreatedAt,
    string AuthorName,
    string IssuerName,
    string VehicleNumber,
    IReadOnlyList<ComponentDocumentItem> Items);
