namespace MyApp.Application.DTO;

public sealed record TableViewRequest(
    string TableName,
    int Page,
    string PageSize,
    string? Search,
    string? LastName,
    string? FirstName,
    string? Patronymic,
    string? Profession);

public sealed record TableViewResponse(
    IReadOnlyList<string> Columns,
    IReadOnlyList<Dictionary<string, object?>> Rows,
    long Total,
    int Page,
    string PageSize);
