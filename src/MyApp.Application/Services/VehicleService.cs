using MyApp.Application.Abstractions;
using MyApp.Application.Common;
using MyApp.Application.DTO;
using System.Globalization;
using System.Text;

namespace MyApp.Application.Services;

public sealed class VehicleService(IVehicleRepository repository) : IVehicleService
{
    private const int MaximumPhotosPerEntry = 10;
    private const int MaximumPhotoSize = 8 * 1024 * 1024;
    private static readonly HashSet<string> AllowedPhotoTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public Task<IReadOnlyList<VehicleResponse>> GetVehiclesAsync(
        CancellationToken cancellationToken) =>
        repository.GetVehiclesAsync(cancellationToken);

    public Task<VehicleJournalResponse?> GetJournalAsync(
        Guid vehicleId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken) =>
        repository.GetJournalAsync(vehicleId, from, to, cancellationToken);

    public Task<ServiceResult<Guid>> AddPurchaseAsync(
        Guid vehicleId,
        VehiclePurchaseRequest request,
        Guid createdBy,
        CancellationToken cancellationToken) =>
        CreateAsync(
            vehicleId,
            ValidatePurchase(request),
            () => repository.AddPurchaseAsync(
                vehicleId,
                request with
                {
                    RequestNumber = Clean(request.RequestNumber),
                    ItemName = Clean(request.ItemName),
                    Status = Clean(request.Status),
                    Note = Clean(request.Note)
                },
                createdBy,
                cancellationToken),
            cancellationToken);

    public Task<ServiceResult<Guid>> AddDefectAsync(
        Guid vehicleId,
        VehicleDefectRequest request,
        Guid createdBy,
        CancellationToken cancellationToken) =>
        CreateAsync(
            vehicleId,
            ValidateDefect(request),
            () => repository.AddDefectAsync(
                vehicleId,
                request with
                {
                    NodeName = Clean(request.NodeName),
                    FailureReason = Clean(request.FailureReason)
                },
                createdBy,
                cancellationToken),
            cancellationToken);

    public async Task<ServiceResult<IReadOnlyList<Guid>>> AddDefectPhotosAsync(
        Guid defectId,
        IReadOnlyList<VehicleWorkPhotoUpload> photos,
        CancellationToken cancellationToken)
    {
        if (!await repository.DefectExistsAsync(defectId, cancellationToken))
        {
            return ServiceResult<IReadOnlyList<Guid>>.NotFound();
        }
        var validation = await ValidatePhotosAsync(
            photos,
            () => repository.GetDefectPhotoCountAsync(defectId, cancellationToken));
        return validation is not null
            ? PhotoValidation(validation)
            : ServiceResult<IReadOnlyList<Guid>>.Success(
                await repository.AddDefectPhotosAsync(
                    defectId, photos, cancellationToken));
    }

    public Task<VehicleWorkPhotoContent?> GetDefectPhotoAsync(
        Guid photoId,
        CancellationToken cancellationToken) =>
        repository.GetDefectPhotoAsync(photoId, cancellationToken);

    public Task<bool> DeleteDefectPhotoAsync(
        Guid photoId,
        CancellationToken cancellationToken) =>
        repository.DeleteDefectPhotoAsync(photoId, cancellationToken);

    public Task<ServiceResult<Guid>> AddHoursAsync(
        Guid vehicleId,
        VehicleHoursRequest request,
        Guid createdBy,
        CancellationToken cancellationToken) =>
        CreateAsync(
            vehicleId,
            request.EngineHours < 0
                ? "Моточасы не могут быть отрицательными."
                : null,
            () => repository.AddHoursAsync(
                vehicleId,
                request with { Note = Clean(request.Note) },
                createdBy,
                cancellationToken),
            cancellationToken);

    public async Task<VehicleHoursImportResponse> ImportHoursAsync(
        Stream csv,
        Guid createdBy,
        DateOnly readingDate,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            csv,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);
        var content = await reader.ReadToEndAsync(cancellationToken);
        var lines = content
            .Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
        var vehicles = await repository.GetVehiclesAsync(cancellationToken);
        var errors = new List<VehicleHoursImportError>();
        var importedByVehicle = new Dictionary<Guid, VehicleHoursImportItem>();
        var firstDataLine = true;

        for (var index = 0; index < lines.Length; index++)
        {
            var lineNumber = index + 1;
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var separator = DetectSeparator(line);
            var columns = ParseCsvLine(line, separator);
            if (firstDataLine && IsHeader(columns))
            {
                firstDataLine = false;
                continue;
            }
            firstDataLine = false;

            if (columns.Count != 3)
            {
                errors.Add(new(
                    lineNumber,
                    "Ожидаются 3 колонки: garage_number;model;engine_hours."));
                continue;
            }
            if (!int.TryParse(columns[0].Trim(), out var garageNumber))
            {
                errors.Add(new(lineNumber, "Некорректный гаражный номер."));
                continue;
            }
            var model = columns[1].Trim();
            decimal? engineHours = null;
            if (!string.IsNullOrWhiteSpace(columns[2]))
            {
                if (!TryParseDecimal(columns[2], out var parsedEngineHours) ||
                    parsedEngineHours < 0)
                {
                    errors.Add(new(lineNumber, "Некорректное значение моточасов."));
                    continue;
                }
                engineHours = parsedEngineHours;
            }

            var matches = vehicles
                .Where(vehicle =>
                    vehicle.GarageNumber == garageNumber &&
                    string.Equals(
                        vehicle.ModelName.Trim(),
                        model,
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length == 0)
            {
                errors.Add(new(
                    lineNumber,
                    $"Техника «{model}», гаражный № {garageNumber}, не найдена."));
                continue;
            }
            if (matches.Length > 1)
            {
                errors.Add(new(
                    lineNumber,
                    $"Найдено несколько машин «{model}» с гаражным № {garageNumber}."));
                continue;
            }
            if (importedByVehicle.ContainsKey(matches[0].Id))
            {
                errors.Add(new(
                    lineNumber,
                    "Эта машина уже указана в импортируемом файле."));
                continue;
            }
            importedByVehicle[matches[0].Id] = new(
                matches[0].Id,
                engineHours);
        }

        if (importedByVehicle.Count > 0)
        {
            await repository.ImportHoursAsync(
                readingDate,
                importedByVehicle.Values.ToArray(),
                createdBy,
                cancellationToken);
        }
        return new VehicleHoursImportResponse(
            importedByVehicle.Count,
            errors);
    }

    public async Task<ServiceResult<Guid>> AddWorkAsync(
        Guid vehicleId,
        VehicleWorkRequest request,
        Guid createdBy,
        CancellationToken cancellationToken)
    {
        var validation = ValidateWork(request);
        if (validation is not null)
        {
            return ServiceResult<Guid>.Validation(
                new Dictionary<string, string[]>
                {
                    ["vehicle"] = [validation]
                });
        }
        if (!await repository.VehicleExistsAsync(vehicleId, cancellationToken) ||
            !await repository.DefectBelongsToVehicleAsync(
                request.DefectId, vehicleId, cancellationToken))
        {
            return ServiceResult<Guid>.NotFound();
        }
        return ServiceResult<Guid>.Success(await repository.AddWorkAsync(
            vehicleId,
            request with
            {
                Description = Clean(request.Description),
                PurchaseRequestNumber = Clean(request.PurchaseRequestNumber)
            },
            createdBy,
            cancellationToken));
    }

    public Task<bool> DeleteEntryAsync(
        string category,
        Guid id,
        CancellationToken cancellationToken) =>
        repository.DeleteEntryAsync(category, id, cancellationToken);

    public async Task<ServiceResult<IReadOnlyList<Guid>>> AddWorkPhotosAsync(
        Guid workId,
        IReadOnlyList<VehicleWorkPhotoUpload> photos,
        CancellationToken cancellationToken)
    {
        if (!await repository.WorkExistsAsync(workId, cancellationToken))
        {
            return ServiceResult<IReadOnlyList<Guid>>.NotFound();
        }
        var validation = await ValidatePhotosAsync(
            photos,
            () => repository.GetWorkPhotoCountAsync(workId, cancellationToken));
        return validation is not null
            ? PhotoValidation(validation)
            : ServiceResult<IReadOnlyList<Guid>>.Success(
                await repository.AddWorkPhotosAsync(
                    workId, photos, cancellationToken));
    }

    public Task<VehicleWorkPhotoContent?> GetWorkPhotoAsync(
        Guid photoId,
        CancellationToken cancellationToken) =>
        repository.GetWorkPhotoAsync(photoId, cancellationToken);

    public Task<bool> DeleteWorkPhotoAsync(
        Guid photoId,
        CancellationToken cancellationToken) =>
        repository.DeleteWorkPhotoAsync(photoId, cancellationToken);

    private async Task<ServiceResult<Guid>> CreateAsync(
        Guid vehicleId,
        string? validationMessage,
        Func<Task<Guid>> create,
        CancellationToken cancellationToken)
    {
        if (validationMessage is not null)
        {
            return ServiceResult<Guid>.Validation(
                new Dictionary<string, string[]>
                {
                    ["vehicle"] = [validationMessage]
                });
        }
        if (!await repository.VehicleExistsAsync(vehicleId, cancellationToken))
        {
            return ServiceResult<Guid>.NotFound();
        }
        return ServiceResult<Guid>.Success(await create());
    }

    private static string? ValidatePurchase(VehiclePurchaseRequest request)
    {
        if (Clean(request.ItemName).Length is < 1 or > 500)
        {
            return "Наименование должно содержать от 1 до 500 символов.";
        }
        if (request.Quantity <= 0)
        {
            return "Количество должно быть больше нуля.";
        }
        return null;
    }

    private static string? ValidateDefect(VehicleDefectRequest request) =>
        ValidateText(request.NodeName, "Узел")
        ?? ValidateText(request.FailureReason, "Причина неисправности");

    private static string? ValidateWork(VehicleWorkRequest request)
    {
        if (request.DefectId == Guid.Empty)
        {
            return "Выберите неисправность.";
        }
        var description = ValidateText(request.Description, "Выполненные работы");
        return description ?? (Clean(request.PurchaseRequestNumber).Length > 100
            ? "Номер заявки не должен превышать 100 символов."
            : null);
    }

    private static string? ValidateText(string value, string label) =>
        Clean(value).Length is < 1 or > 4000
            ? $"{label} должно содержать от 1 до 4000 символов."
            : null;

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;

    private static async Task<string?> ValidatePhotosAsync(
        IReadOnlyList<VehicleWorkPhotoUpload> photos,
        Func<Task<int>> getExistingCount)
    {
        if (photos.Count == 0)
        {
            return "Выберите хотя бы одну фотографию.";
        }
        if (await getExistingCount() + photos.Count > MaximumPhotosPerEntry)
        {
            return $"Для одной записи можно сохранить не более {MaximumPhotosPerEntry} фотографий.";
        }
        return photos.Any(photo =>
            photo.Content.Length is <= 0 or > MaximumPhotoSize ||
            !AllowedPhotoTypes.Contains(photo.ContentType))
            ? "Разрешены JPEG, PNG и WebP размером не более 8 МБ."
            : null;
    }

    private static char DetectSeparator(string line)
    {
        var semicolons = line.Count(character => character == ';');
        var commas = line.Count(character => character == ',');
        return semicolons >= commas ? ';' : ',';
    }

    private static IReadOnlyList<string> ParseCsvLine(string line, char separator)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == separator && !quoted)
            {
                values.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(character);
            }
        }
        values.Add(value.ToString());
        return values;
    }

    private static bool IsHeader(IReadOnlyList<string> columns) =>
        columns.Count >= 3 &&
        columns[0].Trim().Equals(
            "garage_number",
            StringComparison.OrdinalIgnoreCase) &&
        columns[1].Trim().Equals(
            "model",
            StringComparison.OrdinalIgnoreCase);

    private static bool TryParseDecimal(string value, out decimal result) =>
        decimal.TryParse(
            value.Trim().Replace(',', '.'),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out result);

    private static ServiceResult<IReadOnlyList<Guid>> PhotoValidation(
        string message) =>
        ServiceResult<IReadOnlyList<Guid>>.Validation(
            new Dictionary<string, string[]> { ["photos"] = [message] });
}
