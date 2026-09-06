namespace MyApp.Application.DTO;

public sealed record MaterialGroupRequest(string Name);

public sealed record MaterialGroupItemRequest(
    string SourceTable,
    string MaterialName);

public sealed record MaterialGroupItemResponse(
    Guid Id,
    string SourceTable,
    string MaterialName);

public sealed record MaterialGroupResponse(
    Guid Id,
    string Name,
    IReadOnlyList<MaterialGroupItemResponse> Items);

public sealed record MaterialGroupMappingResponse(
    string MaterialName,
    Guid GroupId,
    string GroupName);
