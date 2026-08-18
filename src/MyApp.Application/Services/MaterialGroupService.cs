using MyApp.Application.Abstractions;
using MyApp.Application.Common;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public sealed class MaterialGroupService(
    IMaterialGroupRepository repository) : IMaterialGroupService
{
    private static readonly HashSet<string> AllowedSources =
        new(StringComparer.Ordinal)
        {
            "v_full_ost",
            "v_meh_ost"
        };

    public Task<IReadOnlyList<MaterialGroupResponse>> GetAllAsync(
        CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);

    public async Task<ServiceResult<IReadOnlyList<MaterialGroupMappingResponse>>>
        GetMappingsAsync(
            string sourceTable,
            CancellationToken cancellationToken)
    {
        if (!AllowedSources.Contains(sourceTable))
        {
            return ServiceResult<IReadOnlyList<MaterialGroupMappingResponse>>
                .BadRequest("Неизвестный источник материалов.");
        }

        return ServiceResult<IReadOnlyList<MaterialGroupMappingResponse>>.Success(
            await repository.GetMappingsAsync(sourceTable, cancellationToken));
    }

    public async Task<ServiceResult<Guid>> CreateGroupAsync(
        MaterialGroupRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (name.Length == 0 || name.Length > 100)
        {
            return ServiceResult<Guid>.Validation(
                new Dictionary<string, string[]>
                {
                    [nameof(request.Name)] =
                        ["Название группы должно содержать от 1 до 100 символов."]
                });
        }

        if (await repository.GroupNameExistsAsync(name, cancellationToken))
        {
            return ServiceResult<Guid>.Conflict(
                "Группа с таким названием уже существует.");
        }

        return ServiceResult<Guid>.Success(
            await repository.CreateGroupAsync(name, cancellationToken));
    }

    public async Task<ServiceResult<Guid>> AddItemAsync(
        Guid groupId,
        MaterialGroupItemRequest request,
        CancellationToken cancellationToken)
    {
        var materialName = request.MaterialName.Trim();
        if (!AllowedSources.Contains(request.SourceTable) ||
            materialName.Length == 0)
        {
            return ServiceResult<Guid>.Validation(
                new Dictionary<string, string[]>
                {
                    [nameof(request.MaterialName)] =
                        ["Укажите корректный источник и наименование материала."]
                });
        }

        if (!await repository.GroupExistsAsync(groupId, cancellationToken))
        {
            return ServiceResult<Guid>.NotFound();
        }

        if (!await repository.MaterialExistsAsync(
                request.SourceTable,
                materialName,
                cancellationToken))
        {
            return ServiceResult<Guid>.Validation(
                new Dictionary<string, string[]>
                {
                    [nameof(request.MaterialName)] =
                        ["Материал с таким наименованием не найден."]
                });
        }

        if (await repository.MaterialIsAssignedAsync(
                request.SourceTable,
                materialName,
                cancellationToken))
        {
            return ServiceResult<Guid>.Conflict(
                "Материал уже назначен другой группе.");
        }

        return ServiceResult<Guid>.Success(
            await repository.AddItemAsync(
                groupId,
                request.SourceTable,
                materialName,
                cancellationToken));
    }

    public async Task<ServiceResult<Guid>> DeleteGroupAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await repository.DeleteGroupAsync(id, cancellationToken)
            ? ServiceResult<Guid>.Success(id)
            : ServiceResult<Guid>.NotFound();

    public async Task<ServiceResult<Guid>> DeleteItemAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await repository.DeleteItemAsync(id, cancellationToken)
            ? ServiceResult<Guid>.Success(id)
            : ServiceResult<Guid>.NotFound();

}
