using MyApp.Application.Common;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public interface IMaintenanceTemplateService
{
    Task<IReadOnlyList<MaintenanceEquipmentResponse>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> CreateEquipmentAsync(
        MaintenanceEquipmentRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> CreateIntervalAsync(
        Guid equipmentId,
        MaintenanceIntervalRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> AddItemAsync(
        Guid intervalId,
        MaintenanceItemRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> RenameEquipmentAsync(
        Guid id,
        MaintenanceEquipmentRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> RenameIntervalAsync(
        Guid id,
        MaintenanceIntervalRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> UpdateItemAsync(
        Guid id,
        MaintenanceItemRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> DeleteEquipmentAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> DeleteIntervalAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> DeleteItemAsync(
        Guid id,
        CancellationToken cancellationToken);
}
