using MyApp.Application.DTO;

namespace MyApp.Application.Abstractions;

public interface IMaterialGroupRepository
{
    Task<IReadOnlyList<MaterialGroupResponse>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MaterialGroupMappingResponse>> GetMappingsAsync(
        string sourceTable,
        CancellationToken cancellationToken);

    Task<bool> GroupExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> GroupNameExistsAsync(
        string name,
        CancellationToken cancellationToken);

    Task<bool> MaterialExistsAsync(
        string sourceTable,
        string materialName,
        CancellationToken cancellationToken);

    Task<bool> MaterialIsAssignedAsync(
        string sourceTable,
        string materialName,
        CancellationToken cancellationToken);

    Task<Guid> CreateGroupAsync(
        string name,
        CancellationToken cancellationToken);

    Task<Guid> AddItemAsync(
        Guid groupId,
        string sourceTable,
        string materialName,
        CancellationToken cancellationToken);

    Task<bool> DeleteGroupAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> DeleteItemAsync(Guid id, CancellationToken cancellationToken);
}
