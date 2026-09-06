using MyApp.Application.Common;

namespace MyApp.Application.Services;

public interface ICsvFileService
{
    Task<ServiceResult<byte[]>> GetAsync(
        string fileName,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ReplaceAsync(
        string fileName,
        byte[] content,
        CancellationToken cancellationToken);
}
