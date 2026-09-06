namespace MyApp.Application.DTO;

public sealed record EmployeeSignatureKey(
    string LastName,
    string FirstName,
    string Patronymic);

public sealed record EmployeeSignatureResponse(
    byte[] Content,
    string ContentType);
