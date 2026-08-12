namespace MyApp.Application.DTO;

public sealed record ComponentDocumentRequest(
    DateOnly Date,
    string VehicleNumber,
    string SourceTable,
    ResponsibleEmployeeSelection ResponsibleEmployee,
    IReadOnlyList<ComponentDocumentItem> Items)
{
    public string AuthorPosition { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string IssuerPosition { get; init; } = string.Empty;
    public string IssuerName { get; init; } = string.Empty;
}

public sealed record ComponentDocumentItem(
    string Name,
    string Unit,
    decimal Quantity,
    decimal AvailableQuantity);

public sealed record GeneratedPdfDocument(
    string FileName,
    byte[] Content);

public sealed record ComponentDocumentsResponse(
    IReadOnlyList<GeneratedPdfDocument> Documents);
