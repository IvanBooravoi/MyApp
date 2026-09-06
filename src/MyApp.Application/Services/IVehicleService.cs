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

    Task<ServiceResult<bool>> ClaimDefectAsync(
        Guid defectId, Guid userId, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<Guid>>> AddDefectPhotosAsync(
        Guid defectId,
        IReadOnlyList<VehicleWorkPhotoUpload> photos,
        Guid userId,
        CancellationToken cancellationToken);

    Task<VehicleWorkPhotoContent?> GetDefectPhotoAsync(
        Guid photoId,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteDefectPhotoAsync(
        Guid photoId, Guid userId, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<Guid>>> AddDefectVideosAsync(
        Guid defectId, IReadOnlyList<VehicleMediaUpload> videos, Guid userId,
        CancellationToken cancellationToken);
    Task<VehicleWorkPhotoContent?> GetDefectVideoAsync(
        Guid videoId, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteDefectVideoAsync(
        Guid videoId, Guid userId, CancellationToken cancellationToken);

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

    Task<ServiceResult<Guid>> CompleteDefectAsync(
        Guid defectId, VehicleWorkRequest request, Guid userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<Guid>>> AddWorkPhotosAsync(
        Guid workId,
        IReadOnlyList<VehicleWorkPhotoUpload> photos,
        Guid userId,
        CancellationToken cancellationToken);

    Task<VehicleWorkPhotoContent?> GetWorkPhotoAsync(
        Guid photoId,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> DeleteWorkPhotoAsync(
        Guid photoId, Guid userId, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<Guid>>> AddWorkVideosAsync(
        Guid workId, IReadOnlyList<VehicleMediaUpload> videos, Guid userId,
        CancellationToken cancellationToken);
    Task<VehicleWorkPhotoContent?> GetWorkVideoAsync(
        Guid videoId, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteWorkVideoAsync(
        Guid videoId, Guid userId, CancellationToken cancellationToken);

    Task<ServiceResult<bool>> UpdatePartsRequestAsync(
        Guid workId, VehiclePartsRequest request, Guid userId,
        CancellationToken cancellationToken);
    Task<VehicleRequestFileContent?> GetPartsRequestFileAsync(
        Guid workId, CancellationToken cancellationToken);

    Task<bool> DeleteEntryAsync(
        string category,
        Guid id,
        Guid userId,
        CancellationToken cancellationToken);
}
