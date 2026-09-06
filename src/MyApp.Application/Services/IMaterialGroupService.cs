using MyApp.Application.Common;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public interface IMaterialGroupService
{
    Task<IReadOnlyList<MaterialGroupResponse>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<MaterialGroupMappingResponse>>> GetMappingsAsync(
        string sourceTable,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> CreateGroupAsync(
        MaterialGroupRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> AddItemAsync(
        Guid groupId,
        MaterialGroupItemRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> DeleteGroupAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> DeleteItemAsync(
        Guid id,
        CancellationToken cancellationToken);

}
