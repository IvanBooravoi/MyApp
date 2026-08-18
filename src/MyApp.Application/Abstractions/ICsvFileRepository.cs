namespace MyApp.Application.Abstractions;

public interface ICsvFileRepository
{
    Task<byte[]> GetAsync(
        string fileName,
        CancellationToken cancellationToken);

    Task ReplaceAsync(
        string fileName,
        byte[] content,
        CancellationToken cancellationToken);
}
