using System.Text.Json;
using MyApp.Application.Abstractions;
using MyApp.Application.DTO;
using Npgsql;
using NpgsqlTypes;

namespace MyApp.Infrastructure.Repositories;

public sealed class RequirementJournalRepository(
    NpgsqlDataSource dataSource) : IRequirementJournalRepository
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task SaveAsync(
        Guid userId,
        string authorName,
        string issuerName,
        ComponentDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var requirementId = Guid.NewGuid();
        await using var connection = await dataSource.OpenConnectionAsync(
            cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            cancellationToken);

        await using (var headerCommand = connection.CreateCommand())
        {
            headerCommand.Transaction = transaction;
            headerCommand.CommandText =
                """
                INSERT INTO component_requirements (
                    id,
                    created_by,
                    author_name,
                    issuer_name,
                    vehicle_number,
                    source_table,
                    form_data)
                VALUES (
                    @id,
                    @createdBy,
                    @authorName,
                    @issuerName,
                    @vehicleNumber,
                    @sourceTable,
                    @formData)
                """;
            headerCommand.Parameters.AddWithValue("id", requirementId);
            headerCommand.Parameters.AddWithValue("createdBy", userId);
            headerCommand.Parameters.AddWithValue("authorName", authorName);
            headerCommand.Parameters.AddWithValue("issuerName", issuerName);
            headerCommand.Parameters.AddWithValue(
                "vehicleNumber",
                request.VehicleNumber);
            headerCommand.Parameters.AddWithValue(
                "sourceTable",
                request.SourceTable);
            headerCommand.Parameters.AddWithValue(
                "formData",
                NpgsqlDbType.Jsonb,
                JsonSerializer.Serialize(request, JsonOptions));
            await headerCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        for (var index = 0; index < request.Items.Count; index++)
        {
            var item = request.Items[index];
            await UpdateCsvAsync(
                connection,
                transaction,
                request.SourceTable,
                item,
                cancellationToken);
            await using var itemCommand = connection.CreateCommand();
            itemCommand.Transaction = transaction;
            itemCommand.CommandText =
                """
                INSERT INTO component_requirement_items (
                    requirement_id,
                    position,
                    name,
                    unit,
                    quantity)
                VALUES (
                    @requirementId,
                    @position,
                    @name,
                    @unit,
                    @quantity)
                """;
            itemCommand.Parameters.AddWithValue("requirementId", requirementId);
            itemCommand.Parameters.AddWithValue("position", index + 1);
            itemCommand.Parameters.AddWithValue("name", item.Name);
            itemCommand.Parameters.AddWithValue("unit", item.Unit);
            itemCommand.Parameters.AddWithValue("quantity", item.Quantity);
            await itemCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task UpdateCsvAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sourceTable,
        ComponentDocumentItem item,
        CancellationToken cancellationToken)
    {
        var fileName = sourceTable switch
        {
            "v_full_ost" or "full_ost" => "o",
            "v_meh_ost" or "meh_ost" => "c",
            _ => throw new InvalidOperationException(
                "Неизвестный источник компонентов.")
        };
        var newValue = Math.Max(
            item.AvailableQuantity - item.Quantity,
            0);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "SELECT edit_csv_tab(@fileName, @searchText, @newValue)";
        command.Parameters.AddWithValue("fileName", fileName);
        command.Parameters.AddWithValue("searchText", item.Name);
        command.Parameters.AddWithValue("newValue", newValue);
        var result = Convert.ToString(
            await command.ExecuteScalarAsync(cancellationToken));
        if (string.IsNullOrWhiteSpace(result) ||
            !result.StartsWith("Успешно обновлено!", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                result ?? "Функция edit_csv_tab не вернула результат.");
        }
    }

    public async Task<IReadOnlyList<RequirementJournalEntry>> GetRecentAsync(
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            WITH recent AS (
                SELECT id, created_at, author_name, issuer_name, vehicle_number
                FROM component_requirements
                ORDER BY created_at DESC
                LIMIT 200
            )
            SELECT
                recent.id,
                recent.created_at,
                recent.author_name,
                recent.issuer_name,
                recent.vehicle_number,
                items.name,
                items.unit,
                items.quantity
            FROM recent
            LEFT JOIN component_requirement_items AS items
                ON items.requirement_id = recent.id
            ORDER BY recent.created_at DESC, items.position
            """);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var entries = new List<RequirementJournalEntry>();
        var entryIndexes = new Dictionary<Guid, int>();
        var entryItems = new Dictionary<Guid, List<ComponentDocumentItem>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetGuid(0);
            if (!entryIndexes.ContainsKey(id))
            {
                entryIndexes[id] = entries.Count;
                entryItems[id] = [];
                entries.Add(new RequirementJournalEntry(
                    id,
                    reader.GetFieldValue<DateTimeOffset>(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    entryItems[id]));
            }

            if (!reader.IsDBNull(5))
            {
                entryItems[id].Add(new ComponentDocumentItem(
                    reader.GetString(5),
                    reader.GetString(6),
                    reader.GetDecimal(7),
                    reader.GetDecimal(7)));
            }
        }

        return entries;
    }

    public async Task<ComponentDocumentRequest?> GetDocumentRequestAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT form_data
            FROM component_requirements
            WHERE id = @id
            """);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken) || reader.IsDBNull(0))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ComponentDocumentRequest>(
            reader.GetString(0),
            JsonOptions);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(
            cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            cancellationToken);

        string sourceTable;
        await using (var sourceCommand = connection.CreateCommand())
        {
            sourceCommand.Transaction = transaction;
            sourceCommand.CommandText =
                "SELECT source_table FROM component_requirements WHERE id = @id";
            sourceCommand.Parameters.AddWithValue("id", id);
            var source = await sourceCommand.ExecuteScalarAsync(cancellationToken);
            if (source is null)
            {
                return false;
            }

            sourceTable = Convert.ToString(source) ?? string.Empty;
        }

        var items = new List<ComponentDocumentItem>();
        await using (var itemsCommand = connection.CreateCommand())
        {
            itemsCommand.Transaction = transaction;
            itemsCommand.CommandText =
                """
                SELECT name, unit, quantity
                FROM component_requirement_items
                WHERE requirement_id = @id
                ORDER BY position
                """;
            itemsCommand.Parameters.AddWithValue("id", id);
            await using var reader = await itemsCommand.ExecuteReaderAsync(
                cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new ComponentDocumentItem(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetDecimal(2),
                    0));
            }
        }

        foreach (var item in items)
        {
            await RestoreCsvAsync(
                connection,
                transaction,
                sourceTable,
                item,
                cancellationToken);
        }

        await using var deleteCommand = connection.CreateCommand();
        deleteCommand.Transaction = transaction;
        deleteCommand.CommandText =
            "DELETE FROM component_requirements WHERE id = @id";
        deleteCommand.Parameters.AddWithValue("id", id);
        var deleted = await deleteCommand.ExecuteNonQueryAsync(cancellationToken) > 0;
        await transaction.CommitAsync(cancellationToken);
        return deleted;
    }

    private static async Task RestoreCsvAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sourceTable,
        ComponentDocumentItem item,
        CancellationToken cancellationToken)
    {
        var fileName = sourceTable switch
        {
            "v_full_ost" or "full_ost" => "o",
            "v_meh_ost" or "meh_ost" => "c",
            _ => throw new InvalidOperationException(
                "Для требования не указан источник остатков.")
        };
        var currentQuantity = await GetCurrentQuantityAsync(
            connection,
            transaction,
            sourceTable,
            item.Name,
            cancellationToken);
        var newValue = currentQuantity + item.Quantity;

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "SELECT edit_csv_tab(@fileName, @searchText, @newValue)";
        command.Parameters.AddWithValue("fileName", fileName);
        command.Parameters.AddWithValue("searchText", item.Name);
        command.Parameters.AddWithValue("newValue", newValue);
        var result = Convert.ToString(
            await command.ExecuteScalarAsync(cancellationToken));
        if (string.IsNullOrWhiteSpace(result) ||
            !result.StartsWith("Успешно обновлено!", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                result ?? "Функция edit_csv_tab не вернула результат.");
        }
    }

    private static async Task<decimal> GetCurrentQuantityAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sourceTable,
        string itemName,
        CancellationToken cancellationToken)
    {
        if (sourceTable is not (
                "v_full_ost" or
                "v_meh_ost" or
                "full_ost" or
                "meh_ost"))
        {
            throw new InvalidOperationException(
                "Для требования не указан источник остатков.");
        }

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sourceTable switch
        {
            "v_full_ost" or "v_meh_ost" =>
                $"""
                SELECT "Количество"
                FROM {sourceTable}
                WHERE BTRIM("Наименование") = BTRIM(@name)
                LIMIT 1
                """,
            _ =>
                $"""
                SELECT amount
                FROM {sourceTable}
                WHERE BTRIM(name) = BTRIM(@name)
                LIMIT 1
                """
        };
        command.Parameters.AddWithValue("name", itemName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null || result is DBNull)
        {
            throw new InvalidOperationException(
                $"Компонент «{itemName.Trim()}» не найден в остатках.");
        }

        if (result is decimal decimalValue)
        {
            return decimalValue;
        }

        var text = Convert.ToString(
            result,
            System.Globalization.CultureInfo.InvariantCulture);
        if (decimal.TryParse(
                text?.Replace(',', '.'),
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var quantity))
        {
            return quantity;
        }

        throw new InvalidOperationException(
            $"Некорректный остаток для компонента «{itemName.Trim()}»: {text}.");
    }
}
