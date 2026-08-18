using MyApp.Application.Abstractions;
using MyApp.Application.Common;

namespace MyApp.Application.Services;

public sealed class CsvFileService(ICsvFileRepository repository) : ICsvFileService
{
    private static readonly HashSet<string> AllowedFileNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "c.csv",
            "o.csv",
            "p.csv"
        };

    public async Task<ServiceResult<byte[]>> GetAsync(
        string fileName,
        CancellationToken cancellationToken)
    {
        var normalizedFileName = NormalizeFileName(fileName);
        if (normalizedFileName is null)
        {
            return ServiceResult<byte[]>.NotFound();
        }

        return ServiceResult<byte[]>.Success(
            await repository.GetAsync(normalizedFileName, cancellationToken));
    }

    public async Task<ServiceResult<bool>> ReplaceAsync(
        string fileName,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var normalizedFileName = NormalizeFileName(fileName);
        if (normalizedFileName is null)
        {
            return ServiceResult<bool>.NotFound();
        }

        if (content.Length == 0)
        {
            return ServiceResult<bool>.Validation(
                new Dictionary<string, string[]>
                {
                    ["file"] = ["Выберите непустой CSV-файл."]
                });
        }

        await repository.ReplaceAsync(
            normalizedFileName,
            content,
            cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    private static string? NormalizeFileName(string fileName)
    {
        var normalized = fileName.Trim().ToLowerInvariant();
        return AllowedFileNames.Contains(normalized) ? normalized : null;
    }
}
