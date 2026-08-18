namespace MyApp.Application.DTO;

public sealed record VehicleResponse(
    Guid Id,
    string GroupName,
    string TypeName,
    string ModelName,
    int? GarageNumber,
    string StateNumber,
    string Vin);

public sealed record VehiclePurchaseRequest(
    DateOnly RequestDate,
    string RequestNumber,
    string ItemName,
    decimal Quantity,
    string Status,
    string Note);

public sealed record VehiclePurchaseResponse(
    Guid Id,
    DateOnly RequestDate,
    string RequestNumber,
    string ItemName,
    decimal Quantity,
    string Status,
    string Note,
    DateTimeOffset CreatedAt);

public sealed record VehicleDefectRequest(
    string NodeName,
    string FailureReason);

public sealed record VehicleDefectResponse(
    Guid Id,
    string NodeName,
    string FailureReason,
    IReadOnlyList<VehicleWorkPhotoResponse> Photos);

public sealed record VehicleHoursRequest(
    DateOnly ReadingDate,
    decimal EngineHours,
    string Note);

public sealed record VehicleHoursResponse(
    Guid Id,
    DateOnly ReadingDate,
    decimal EngineHours,
    string Note,
    DateTimeOffset CreatedAt);

public sealed record VehicleHoursImportItem(
    Guid VehicleId,
    decimal? EngineHours);

public sealed record VehicleHoursImportError(
    int Line,
    string Message);

public sealed record VehicleHoursImportResponse(
    int Imported,
    IReadOnlyList<VehicleHoursImportError> Errors);

public sealed record VehicleWorkRequest(
    Guid DefectId,
    string Description,
    string PurchaseRequestNumber);

public sealed record VehicleWorkResponse(
    Guid Id,
    Guid? DefectId,
    string DefectNodeName,
    string Description,
    string PurchaseRequestNumber,
    DateTimeOffset CreatedAt,
    IReadOnlyList<VehicleWorkPhotoResponse> Photos);

public sealed record VehicleWorkPhotoResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long Size);

public sealed record VehicleWorkPhotoUpload(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed record VehicleWorkPhotoContent(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed record VehicleJournalResponse(
    VehicleResponse Vehicle,
    IReadOnlyList<VehiclePurchaseResponse> Purchases,
    IReadOnlyList<VehicleDefectResponse> Defects,
    IReadOnlyList<VehicleHoursResponse> Hours,
    IReadOnlyList<VehicleWorkResponse> Works);
