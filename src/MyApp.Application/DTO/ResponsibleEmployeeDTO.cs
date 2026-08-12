namespace MyApp.Application.DTO;

public sealed record ResponsibleEmployeeResponse(
    string FirstName,
    string Patronymic,
    string LastName,
    string Profession);

public sealed record ResponsibleEmployeeSelection(
    string FirstName,
    string Patronymic,
    string LastName);
