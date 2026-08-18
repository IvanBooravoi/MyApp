using MyApp.Application.Abstractions;
using MyApp.Application.DTO;
using Npgsql;

namespace MyApp.Infrastructure.Repositories;

public sealed class EmployeeSignatureRepository(
    NpgsqlDataSource dataSource) : IEmployeeSignatureRepository
{
    public async Task<EmployeeSignatureResponse?> GetAsync(
        EmployeeSignatureKey employee,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT content, content_type
            FROM employee_signatures
            WHERE last_name = @lastName
              AND first_name = @firstName
              AND patronymic = @patronymic
            """);
        AddEmployeeParameters(command, employee);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new EmployeeSignatureResponse(
                reader.GetFieldValue<byte[]>(0),
                reader.GetString(1))
            : null;
    }

    public async Task SaveAsync(
        EmployeeSignatureKey employee,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO employee_signatures (
                last_name,
                first_name,
                patronymic,
                content,
                content_type)
            VALUES (
                @lastName,
                @firstName,
                @patronymic,
                @content,
                @contentType)
            ON CONFLICT (last_name, first_name, patronymic)
            DO UPDATE SET
                content = EXCLUDED.content,
                content_type = EXCLUDED.content_type,
                updated_at = NOW()
            """);
        AddEmployeeParameters(command, employee);
        command.Parameters.AddWithValue("content", content);
        command.Parameters.AddWithValue("contentType", contentType);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        EmployeeSignatureKey employee,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            DELETE FROM employee_signatures
            WHERE last_name = @lastName
              AND first_name = @firstName
              AND patronymic = @patronymic
            """);
        AddEmployeeParameters(command, employee);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void AddEmployeeParameters(
        NpgsqlCommand command,
        EmployeeSignatureKey employee)
    {
        command.Parameters.AddWithValue("lastName", employee.LastName);
        command.Parameters.AddWithValue("firstName", employee.FirstName);
        command.Parameters.AddWithValue("patronymic", employee.Patronymic);
    }
}
