using MyApp.Application.Common;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public interface IVehicleService
{
    Task<IReadOnlyList<VehicleResponse>> GetVehiclesAsync(
        CancellationToken cancellationToken);

    Task<VehicleJournalResponse?> GetJournalAsync(
        Guid vehicleId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> AddPurchaseAsync(
        Guid vehicleId,
        VehiclePurchaseRequest request,
        Guid createdBy,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> AddDefectAsync(
        Guid vehicleId,
        VehicleDefectRequest request,
        Guid createdBy,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<Guid>>> AddDefectPhotosAsync(
        Guid defectId,
        IReadOnlyList<VehicleWorkPhotoUpload> photos,
        CancellationToken cancellationToken);

    Task<VehicleWorkPhotoContent?> GetDefectPhotoAsync(
        Guid photoId,
        CancellationToken cancellationToken);

    Task<bool> DeleteDefectPhotoAsync(
        Guid photoId,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> AddHoursAsync(
        Guid vehicleId,
        VehicleHoursRequest request,
        Guid createdBy,
        CancellationToken cancellationToken);

    Task<VehicleHoursImportResponse> ImportHoursAsync(
        Stream file,
        string fileExtension,
        Guid createdBy,
        DateOnly readingDate,
        CancellationToken cancellationToken);

    Task<ServiceResult<Guid>> AddWorkAsync(
        Guid vehicleId,
        VehicleWorkRequest request,
        Guid createdBy,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<Guid>>> AddWorkPhotosAsync(
        Guid workId,
        IReadOnlyList<VehicleWorkPhotoUpload> photos,
        CancellationToken cancellationToken);

    Task<VehicleWorkPhotoContent?> GetWorkPhotoAsync(
        Guid photoId,
        CancellationToken cancellationToken);

    Task<bool> DeleteWorkPhotoAsync(
        Guid photoId,
        CancellationToken cancellationToken);

    Task<bool> DeleteEntryAsync(
        string category,
        Guid id,
        CancellationToken cancellationToken);
}
