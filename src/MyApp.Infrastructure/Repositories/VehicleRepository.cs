using MyApp.Application.Abstractions;
using MyApp.Application.DTO;
using Npgsql;

namespace MyApp.Infrastructure.Repositories;

public sealed class VehicleRepository(NpgsqlDataSource dataSource) : IVehicleRepository
{
    private const string VehicleSelect =
        """
        SELECT
            vehicles.id,
            COALESCE(groups.full_name, ''),
            COALESCE(types.full_name, ''),
            COALESCE(models.full_name, ''),
            vehicles.gar_number,
            COALESCE(vehicles.gos_number, ''),
            COALESCE(vehicles.vim, '')
        FROM number_car AS vehicles
        LEFT JOIN model_car AS models ON models.id = vehicles.car_mode_id
        LEFT JOIN type_car AS types ON types.id = models.car_type_id
        LEFT JOIN group_car AS groups ON groups.id = types.car_group_id
        """;

    public async Task<IReadOnlyList<VehicleResponse>> GetVehiclesAsync(
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            VehicleSelect +
            """

            ORDER BY
                COALESCE(groups.full_name, ''),
                COALESCE(types.full_name, ''),
                COALESCE(models.full_name, ''),
                vehicles.gar_number NULLS LAST
            """);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<VehicleResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(ReadVehicle(reader));
        }
        return result;
    }

    public async Task<VehicleJournalResponse?> GetJournalAsync(
        Guid vehicleId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var vehicle = await GetVehicleAsync(vehicleId, cancellationToken);
        if (vehicle is null)
        {
            return null;
        }

        var purchases = await GetPurchasesAsync(
            vehicleId, from, to, cancellationToken);
        var defects = await GetDefectsAsync(
            vehicleId, from, to, cancellationToken);
        var hours = await GetHoursAsync(
            vehicleId, from, to, cancellationToken);
        var works = await GetWorksAsync(
            vehicleId, from, to, cancellationToken);
        return new VehicleJournalResponse(
            vehicle, purchases, defects, hours, works);
    }

    public async Task<bool> VehicleExistsAsync(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT EXISTS (SELECT 1 FROM number_car WHERE id = @vehicleId)");
        command.Parameters.AddWithValue("vehicleId", vehicleId);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public Task<Guid> AddPurchaseAsync(
        Guid vehicleId,
        VehiclePurchaseRequest request,
        Guid createdBy,
        CancellationToken cancellationToken) =>
        InsertAsync(
            """
            INSERT INTO vehicle_purchase_requests (
                id, vehicle_id, request_date, request_number, item_name,
                quantity, status, note, created_by)
            VALUES (
                @id, @vehicleId, @date, @number, @item, @quantity,
                @status, @note, @createdBy)
            """,
            vehicleId,
            createdBy,
            cancellationToken,
            ("date", request.RequestDate),
            ("number", request.RequestNumber),
            ("item", request.ItemName),
            ("quantity", request.Quantity),
            ("status", request.Status),
            ("note", request.Note));

    public Task<Guid> AddDefectAsync(
        Guid vehicleId,
        VehicleDefectRequest request,
        Guid createdBy,
        CancellationToken cancellationToken) =>
        InsertAsync(
            """
            INSERT INTO vehicle_defects (
                id, vehicle_id, node_name, failure_reason, created_by)
            VALUES (
                @id, @vehicleId, @nodeName, @failureReason, @createdBy)
            """,
            vehicleId,
            createdBy,
            cancellationToken,
            ("nodeName", request.NodeName),
            ("failureReason", request.FailureReason));

    public async Task<bool> DefectExistsAsync(
        Guid defectId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT EXISTS (SELECT 1 FROM vehicle_defects WHERE id = @defectId)");
        command.Parameters.AddWithValue("defectId", defectId);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<bool> DefectBelongsToVehicleAsync(
        Guid defectId,
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT EXISTS (
                SELECT 1
                FROM vehicle_defects
                WHERE id = @defectId AND vehicle_id = @vehicleId)
            """);
        command.Parameters.AddWithValue("defectId", defectId);
        command.Parameters.AddWithValue("vehicleId", vehicleId);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<int> GetDefectPhotoCountAsync(
        Guid defectId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT COUNT(*) FROM vehicle_defect_photos WHERE defect_id = @defectId");
        command.Parameters.AddWithValue("defectId", defectId);
        return Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken));
    }

    public Task<IReadOnlyList<Guid>> AddDefectPhotosAsync(
        Guid defectId,
        IReadOnlyList<VehicleWorkPhotoUpload> photos,
        CancellationToken cancellationToken) =>
        AddPhotosAsync(
            "vehicle_defect_photos",
            "defect_id",
            defectId,
            photos,
            cancellationToken);

    public Task<VehicleWorkPhotoContent?> GetDefectPhotoAsync(
        Guid photoId,
        CancellationToken cancellationToken) =>
        GetPhotoAsync("vehicle_defect_photos", photoId, cancellationToken);

    public Task<bool> DeleteDefectPhotoAsync(
        Guid photoId,
        CancellationToken cancellationToken) =>
        DeletePhotoAsync("vehicle_defect_photos", photoId, cancellationToken);

    public Task<Guid> AddHoursAsync(
        Guid vehicleId,
        VehicleHoursRequest request,
        Guid createdBy,
        CancellationToken cancellationToken) =>
        InsertAsync(
            """
            INSERT INTO vehicle_hour_readings (
                id, vehicle_id, reading_date, engine_hours, note, created_by)
            VALUES (
                @id, @vehicleId, @date, @hours, @note, @createdBy)
            """,
            vehicleId,
            createdBy,
            cancellationToken,
            ("date", request.ReadingDate),
            ("hours", request.EngineHours),
            ("note", request.Note));

    public async Task ImportHoursAsync(
        DateOnly readingDate,
        IReadOnlyList<VehicleHoursImportItem> items,
        Guid createdBy,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(
            cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            cancellationToken);
        foreach (var item in items)
        {
            var engineHours = item.EngineHours;
            if (engineHours is null)
            {
                await using var previous = connection.CreateCommand();
                previous.Transaction = transaction;
                previous.CommandText =
                    """
                    SELECT engine_hours
                    FROM vehicle_hour_readings
                    WHERE vehicle_id = @vehicleId
                      AND reading_date <= @readingDate
                    ORDER BY reading_date DESC, created_at DESC
                    LIMIT 1
                    """;
                previous.Parameters.AddWithValue("vehicleId", item.VehicleId);
                previous.Parameters.AddWithValue("readingDate", readingDate);
                var value = await previous.ExecuteScalarAsync(cancellationToken);
                engineHours = value is decimal hours ? hours : 0m;
            }

            await using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText =
                """
                UPDATE vehicle_hour_readings
                SET engine_hours = @hours,
                    note = 'Импорт CSV',
                    created_by = @createdBy,
                    created_at = NOW()
                WHERE vehicle_id = @vehicleId
                  AND reading_date = @readingDate
                """;
            update.Parameters.AddWithValue("hours", engineHours.Value);
            update.Parameters.AddWithValue("createdBy", createdBy);
            update.Parameters.AddWithValue("vehicleId", item.VehicleId);
            update.Parameters.AddWithValue("readingDate", readingDate);
            if (await update.ExecuteNonQueryAsync(cancellationToken) > 0)
            {
                continue;
            }

            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                """
                INSERT INTO vehicle_hour_readings (
                    id, vehicle_id, reading_date, engine_hours, note, created_by)
                VALUES (
                    @id, @vehicleId, @readingDate, @hours, 'Импорт CSV', @createdBy)
                """;
            insert.Parameters.AddWithValue("id", Guid.NewGuid());
            insert.Parameters.AddWithValue("vehicleId", item.VehicleId);
            insert.Parameters.AddWithValue("readingDate", readingDate);
            insert.Parameters.AddWithValue("hours", engineHours.Value);
            insert.Parameters.AddWithValue("createdBy", createdBy);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<Guid> AddWorkAsync(
        Guid vehicleId,
        VehicleWorkRequest request,
        Guid createdBy,
        CancellationToken cancellationToken) =>
        InsertAsync(
            """
            INSERT INTO vehicle_works (
                id, vehicle_id, work_date, description, engine_hours,
                performer, note, defect_id, purchase_request_number, created_by)
            VALUES (
                @id, @vehicleId, CURRENT_DATE, @description, NULL,
                '', '', @defectId, @requestNumber, @createdBy)
            """,
            vehicleId,
            createdBy,
            cancellationToken,
            ("description", request.Description),
            ("defectId", request.DefectId),
            ("requestNumber", request.PurchaseRequestNumber));

    public async Task<bool> WorkExistsAsync(
        Guid workId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT EXISTS (SELECT 1 FROM vehicle_works WHERE id = @workId)");
        command.Parameters.AddWithValue("workId", workId);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<int> GetWorkPhotoCountAsync(
        Guid workId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT COUNT(*) FROM vehicle_work_photos WHERE work_id = @workId");
        command.Parameters.AddWithValue("workId", workId);
        return Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<Guid>> AddWorkPhotosAsync(
        Guid workId,
        IReadOnlyList<VehicleWorkPhotoUpload> photos,
        CancellationToken cancellationToken) =>
        await AddPhotosAsync(
            "vehicle_work_photos",
            "work_id",
            workId,
            photos,
            cancellationToken);

    private async Task<IReadOnlyList<Guid>> AddPhotosAsync(
        string table,
        string parentColumn,
        Guid parentId,
        IReadOnlyList<VehicleWorkPhotoUpload> photos,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(
            cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            cancellationToken);
        var ids = new List<Guid>(photos.Count);
        foreach (var photo in photos)
        {
            var id = Guid.NewGuid();
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                $"""
                INSERT INTO {table} (
                    id, {parentColumn}, file_name, content_type, content)
                VALUES (@id, @parentId, @fileName, @contentType, @content)
                """;
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("parentId", parentId);
            command.Parameters.AddWithValue("fileName", photo.FileName);
            command.Parameters.AddWithValue("contentType", photo.ContentType);
            command.Parameters.AddWithValue("content", photo.Content);
            await command.ExecuteNonQueryAsync(cancellationToken);
            ids.Add(id);
        }
        await transaction.CommitAsync(cancellationToken);
        return ids;
    }

    public async Task<VehicleWorkPhotoContent?> GetWorkPhotoAsync(
        Guid photoId,
        CancellationToken cancellationToken) =>
        await GetPhotoAsync("vehicle_work_photos", photoId, cancellationToken);

    private async Task<VehicleWorkPhotoContent?> GetPhotoAsync(
        string table,
        Guid photoId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            $"""
            SELECT file_name, content_type, content
            FROM {table}
            WHERE id = @photoId
            """);
        command.Parameters.AddWithValue("photoId", photoId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new VehicleWorkPhotoContent(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetFieldValue<byte[]>(2))
            : null;
    }

    public async Task<bool> DeleteWorkPhotoAsync(
        Guid photoId,
        CancellationToken cancellationToken) =>
        await DeletePhotoAsync("vehicle_work_photos", photoId, cancellationToken);

    private async Task<bool> DeletePhotoAsync(
        string table,
        Guid photoId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            $"DELETE FROM {table} WHERE id = @photoId");
        command.Parameters.AddWithValue("photoId", photoId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteEntryAsync(
        string category,
        Guid id,
        CancellationToken cancellationToken)
    {
        var table = category switch
        {
            "purchases" => "vehicle_purchase_requests",
            "defects" => "vehicle_defects",
            "hours" => "vehicle_hour_readings",
            "works" => "vehicle_works",
            _ => null
        };
        if (table is null)
        {
            return false;
        }

        await using var command = dataSource.CreateCommand(
            $"DELETE FROM {table} WHERE id = @id");
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private async Task<VehicleResponse?> GetVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            VehicleSelect + "\nWHERE vehicles.id = @vehicleId");
        command.Parameters.AddWithValue("vehicleId", vehicleId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadVehicle(reader)
            : null;
    }

    private async Task<IReadOnlyList<VehiclePurchaseResponse>> GetPurchasesAsync(
        Guid vehicleId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        await using var command = CreateRangeCommand(
            """
            SELECT id, request_date, request_number, item_name, quantity,
                   status, note, created_at
            FROM vehicle_purchase_requests
            WHERE vehicle_id = @vehicleId
              AND (@from IS NULL OR request_date >= @from)
              AND (@to IS NULL OR request_date <= @to)
            ORDER BY request_date DESC, created_at DESC
            """,
            vehicleId,
            from,
            to);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<VehiclePurchaseResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new VehiclePurchaseResponse(
                reader.GetGuid(0),
                reader.GetFieldValue<DateOnly>(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetDecimal(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetFieldValue<DateTimeOffset>(7)));
        }
        return result;
    }

    private async Task<IReadOnlyList<VehicleDefectResponse>> GetDefectsAsync(
        Guid vehicleId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        await using var command = CreateRangeCommand(
            """
            SELECT id, node_name, failure_reason
            FROM vehicle_defects
            WHERE vehicle_id = @vehicleId
              AND (@from IS NULL OR created_at::date >= @from)
              AND (@to IS NULL OR created_at::date <= @to)
            ORDER BY created_at DESC
            """,
            vehicleId,
            from,
            to);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<VehicleDefectResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new VehicleDefectResponse(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                []));
        }
        await reader.DisposeAsync();

        await using var photoCommand = CreateRangeCommand(
            """
            SELECT photos.defect_id, photos.id, photos.file_name,
                   photos.content_type, OCTET_LENGTH(photos.content)
            FROM vehicle_defect_photos AS photos
            JOIN vehicle_defects AS defects ON defects.id = photos.defect_id
            WHERE defects.vehicle_id = @vehicleId
              AND (@from IS NULL OR defects.created_at::date >= @from)
              AND (@to IS NULL OR defects.created_at::date <= @to)
            ORDER BY photos.created_at, photos.id
            """,
            vehicleId,
            from,
            to);
        await using var photoReader = await photoCommand.ExecuteReaderAsync(
            cancellationToken);
        var photosByDefect = new Dictionary<Guid, List<VehicleWorkPhotoResponse>>();
        while (await photoReader.ReadAsync(cancellationToken))
        {
            var defectId = photoReader.GetGuid(0);
            if (!photosByDefect.TryGetValue(defectId, out var photos))
            {
                photos = [];
                photosByDefect[defectId] = photos;
            }
            photos.Add(new VehicleWorkPhotoResponse(
                photoReader.GetGuid(1),
                photoReader.GetString(2),
                photoReader.GetString(3),
                photoReader.GetInt32(4)));
        }
        return result
            .Select(defect => defect with
            {
                Photos = photosByDefect.GetValueOrDefault(defect.Id) ?? []
            })
            .ToArray();
    }

    private async Task<IReadOnlyList<VehicleHoursResponse>> GetHoursAsync(
        Guid vehicleId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        await using var command = CreateRangeCommand(
            """
            SELECT id, reading_date, engine_hours, note, created_at
            FROM vehicle_hour_readings
            WHERE vehicle_id = @vehicleId
              AND (@from IS NULL OR reading_date >= @from)
              AND (@to IS NULL OR reading_date <= @to)
            ORDER BY reading_date DESC, created_at DESC
            """,
            vehicleId,
            from,
            to);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<VehicleHoursResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new VehicleHoursResponse(
                reader.GetGuid(0),
                reader.GetFieldValue<DateOnly>(1),
                reader.GetDecimal(2),
                reader.GetString(3),
                reader.GetFieldValue<DateTimeOffset>(4)));
        }
        return result;
    }

    private async Task<IReadOnlyList<VehicleWorkResponse>> GetWorksAsync(
        Guid vehicleId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        await using var command = CreateRangeCommand(
            """
            SELECT works.id, works.defect_id, COALESCE(defects.node_name, ''),
                   works.description, works.purchase_request_number,
                   works.created_at
            FROM vehicle_works AS works
            LEFT JOIN vehicle_defects AS defects ON defects.id = works.defect_id
            WHERE works.vehicle_id = @vehicleId
              AND (@from IS NULL OR works.created_at::date >= @from)
              AND (@to IS NULL OR works.created_at::date <= @to)
            ORDER BY works.created_at DESC
            """,
            vehicleId,
            from,
            to);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<VehicleWorkResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new VehicleWorkResponse(
                reader.GetGuid(0),
                reader.IsDBNull(1) ? null : reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetFieldValue<DateTimeOffset>(5),
                []));
        }
        await reader.DisposeAsync();

        await using var photoCommand = CreateRangeCommand(
            """
            SELECT photos.work_id, photos.id, photos.file_name,
                   photos.content_type, OCTET_LENGTH(photos.content)
            FROM vehicle_work_photos AS photos
            JOIN vehicle_works AS works ON works.id = photos.work_id
            WHERE works.vehicle_id = @vehicleId
              AND (@from IS NULL OR works.created_at::date >= @from)
              AND (@to IS NULL OR works.created_at::date <= @to)
            ORDER BY photos.created_at, photos.id
            """,
            vehicleId,
            from,
            to);
        await using var photoReader = await photoCommand.ExecuteReaderAsync(
            cancellationToken);
        var photosByWork = new Dictionary<Guid, List<VehicleWorkPhotoResponse>>();
        while (await photoReader.ReadAsync(cancellationToken))
        {
            var workId = photoReader.GetGuid(0);
            if (!photosByWork.TryGetValue(workId, out var photos))
            {
                photos = [];
                photosByWork[workId] = photos;
            }
            photos.Add(new VehicleWorkPhotoResponse(
                photoReader.GetGuid(1),
                photoReader.GetString(2),
                photoReader.GetString(3),
                photoReader.GetInt32(4)));
        }
        return result
            .Select(work => work with
            {
                Photos = photosByWork.GetValueOrDefault(work.Id) ?? []
            })
            .ToArray();
    }

    private NpgsqlCommand CreateRangeCommand(
        string sql,
        Guid vehicleId,
        DateOnly? from,
        DateOnly? to)
    {
        var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("vehicleId", vehicleId);
        command.Parameters.AddWithValue(
            "from",
            NpgsqlTypes.NpgsqlDbType.Date,
            from is null ? DBNull.Value : from.Value);
        command.Parameters.AddWithValue(
            "to",
            NpgsqlTypes.NpgsqlDbType.Date,
            to is null ? DBNull.Value : to.Value);
        return command;
    }

    private async Task<Guid> InsertAsync(
        string sql,
        Guid vehicleId,
        Guid createdBy,
        CancellationToken cancellationToken,
        params (string Name, object? Value)[] parameters)
    {
        var id = Guid.NewGuid();
        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("vehicleId", vehicleId);
        command.Parameters.AddWithValue("createdBy", createdBy);
        foreach (var (name, value) in parameters)
        {
            if (value is null)
            {
                command.Parameters.Add(
                    name,
                    name == "resolvedDate"
                        ? NpgsqlTypes.NpgsqlDbType.Date
                        : NpgsqlTypes.NpgsqlDbType.Numeric).Value = DBNull.Value;
            }
            else
            {
                command.Parameters.AddWithValue(name, value);
            }
        }
        await command.ExecuteNonQueryAsync(cancellationToken);
        return id;
    }

    private static VehicleResponse ReadVehicle(NpgsqlDataReader reader) =>
        new(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetInt32(4),
            reader.GetString(5),
            reader.GetString(6));
}
