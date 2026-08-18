namespace MyApp.Application.DTO;

public sealed record MaintenanceEquipmentRequest(string Name);

public sealed record MaintenanceIntervalRequest(string Name);

public sealed record MaintenanceItemRequest(
    string MaterialName,
    decimal Quantity);

public sealed record MaintenanceItemResponse(
    Guid Id,
    string MaterialName,
    string Unit,
    decimal Quantity,
    decimal AvailableQuantity);

public sealed record MaintenanceIntervalResponse(
    Guid Id,
    string Name,
    IReadOnlyList<MaintenanceItemResponse> Items);

public sealed record MaintenanceEquipmentResponse(
    Guid Id,
    string Name,
    IReadOnlyList<MaintenanceIntervalResponse> Intervals);
