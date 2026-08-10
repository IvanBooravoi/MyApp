namespace MyApp.Application.DTO;

public sealed record ComponentDocumentRequest(
    DateOnly Date,
    string VehicleNumber,
    IReadOnlyList<ComponentDocumentItem> Items);

public sealed record ComponentDocumentItem(
    string Name,
    string Unit,
    decimal Quantity);

public sealed record GeneratedPdfDocument(
    string FileName,
    byte[] Content);

public sealed record ComponentDocumentsResponse(
    IReadOnlyList<GeneratedPdfDocument> Documents);
