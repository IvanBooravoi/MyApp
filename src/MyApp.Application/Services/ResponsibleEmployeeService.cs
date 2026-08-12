using MyApp.Application.Abstractions;
using MyApp.Application.Common;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public sealed class ResponsibleEmployeeService(
    IResponsibleEmployeeRepository repository) : IResponsibleEmployeeService
{
    public async Task<ServiceResult<IReadOnlyList<ResponsibleEmployeeResponse>>> GetAsync(
        string sourceTable,
        CancellationToken cancellationToken)
    {
        var profession = GetProfession(sourceTable);
        if (profession is null)
        {
            return ServiceResult<IReadOnlyList<ResponsibleEmployeeResponse>>
                .BadRequest("Неизвестный источник остатков.");
        }

        return ServiceResult<IReadOnlyList<ResponsibleEmployeeResponse>>.Success(
            await repository.GetByProfessionAsync(
                profession,
                cancellationToken));
    }

    public async Task<ServiceResult<ResponsibleEmployeeResponse>> ResolveAsync(
        string sourceTable,
        ResponsibleEmployeeSelection employee,
        CancellationToken cancellationToken)
    {
        var profession = GetProfession(sourceTable);
        if (profession is null)
        {
            return ServiceResult<ResponsibleEmployeeResponse>.BadRequest(
                "Неизвестный источник остатков.");
        }

        if (!await repository.ExistsAsync(
            employee,
            profession,
            cancellationToken))
        {
            return ServiceResult<ResponsibleEmployeeResponse>.Validation(
                new Dictionary<string, string[]>
                {
                    [nameof(employee)] =
                        ["Выбранное ответственное лицо не найдено."]
                });
        }

        return ServiceResult<ResponsibleEmployeeResponse>.Success(
            new ResponsibleEmployeeResponse(
                employee.FirstName.Trim(),
                employee.Patronymic.Trim(),
                employee.LastName.Trim(),
                profession));
    }

    private static string? GetProfession(string sourceTable) =>
        sourceTable.ToLowerInvariant() switch
        {
            "full_ost" => "Кладовщик",
            "meh_ost" => "Старший механик",
            _ => null
        };
}
