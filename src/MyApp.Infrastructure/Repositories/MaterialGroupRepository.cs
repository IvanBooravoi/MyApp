using MyApp.Application.Abstractions;
using MyApp.Application.DTO;
using Npgsql;

namespace MyApp.Infrastructure.Repositories;

public sealed class MaterialGroupRepository(
    NpgsqlDataSource dataSource) : IMaterialGroupRepository
{
    public async Task<IReadOnlyList<MaterialGroupResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT
                groups.id,
                groups.name,
                items.id,
                items.source_table,
                items.material_name
            FROM material_groups AS groups
            LEFT JOIN material_group_items AS items
                ON items.group_id = groups.id
            ORDER BY groups.name, items.material_name
            """);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var groups = new List<MaterialGroupResponse>();
        var indexes = new Dictionary<Guid, int>();
        var items = new Dictionary<Guid, List<MaterialGroupItemResponse>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var groupId = reader.GetGuid(0);
            if (!indexes.ContainsKey(groupId))
            {
                indexes[groupId] = groups.Count;
                items[groupId] = [];
                groups.Add(new MaterialGroupResponse(
                    groupId,
                    reader.GetString(1),
                    items[groupId]));
            }

            if (!reader.IsDBNull(2))
            {
                items[groupId].Add(new MaterialGroupItemResponse(
                    reader.GetGuid(2),
                    reader.GetString(3),
                    reader.GetString(4)));
            }
        }

        return groups;
    }

    public async Task<IReadOnlyList<MaterialGroupMappingResponse>> GetMappingsAsync(
        string sourceTable,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT items.material_name, groups.id, groups.name
            FROM material_group_items AS items
            JOIN material_groups AS groups ON groups.id = items.group_id
            WHERE items.source_table = @sourceTable
            ORDER BY items.material_name
            """);
        command.Parameters.AddWithValue("sourceTable", sourceTable);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var mappings = new List<MaterialGroupMappingResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            mappings.Add(new MaterialGroupMappingResponse(
                reader.GetString(0),
                reader.GetGuid(1),
                reader.GetString(2)));
        }

        return mappings;
    }

    public Task<bool> GroupExistsAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        ExecuteExistsAsync(
            "SELECT EXISTS (SELECT 1 FROM material_groups WHERE id = @value)",
            id,
            cancellationToken);

    public Task<bool> GroupNameExistsAsync(
        string name,
        CancellationToken cancellationToken) =>
        ExecuteExistsAsync(
            """
            SELECT EXISTS (
                SELECT 1 FROM material_groups
                WHERE LOWER(BTRIM(name)) = LOWER(BTRIM(@value)))
            """,
            name,
            cancellationToken);

    public async Task<bool> MaterialExistsAsync(
        string sourceTable,
        string materialName,
        CancellationToken cancellationToken)
    {
        if (sourceTable is not ("v_full_ost" or "v_meh_ost"))
        {
            return false;
        }

        await using var command = dataSource.CreateCommand(
            $"""
            SELECT EXISTS (
                SELECT 1
                FROM {sourceTable}
                WHERE BTRIM("Наименование") = BTRIM(@materialName))
            """);
        command.Parameters.AddWithValue("materialName", materialName);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<bool> MaterialIsAssignedAsync(
        string sourceTable,
        string materialName,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT EXISTS (
                SELECT 1
                FROM material_group_items
                WHERE source_table = @sourceTable
                  AND BTRIM(material_name) = BTRIM(@materialName))
            """);
        command.Parameters.AddWithValue("sourceTable", sourceTable);
        command.Parameters.AddWithValue("materialName", materialName);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<Guid> CreateGroupAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        await using var command = dataSource.CreateCommand(
            "INSERT INTO material_groups (id, name) VALUES (@id, @name)");
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("name", name);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return id;
    }

    public async Task<Guid> AddItemAsync(
        Guid groupId,
        string sourceTable,
        string materialName,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO material_group_items (
                id, group_id, source_table, material_name)
            VALUES (@id, @groupId, @sourceTable, @materialName)
            """);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("groupId", groupId);
        command.Parameters.AddWithValue("sourceTable", sourceTable);
        command.Parameters.AddWithValue("materialName", materialName);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return id;
    }

    public Task<bool> DeleteGroupAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        ExecuteDeleteAsync(
            "DELETE FROM material_groups WHERE id = @id",
            id,
            cancellationToken);

    public Task<bool> DeleteItemAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        ExecuteDeleteAsync(
            "DELETE FROM material_group_items WHERE id = @id",
            id,
            cancellationToken);

    private async Task<bool> ExecuteExistsAsync<T>(
        string sql,
        T value,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("value", value!);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    private async Task<bool> ExecuteDeleteAsync(
        string sql,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }
}
