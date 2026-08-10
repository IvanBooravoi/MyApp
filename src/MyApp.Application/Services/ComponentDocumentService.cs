using MyApp.Application.Abstractions;
using MyApp.Application.Common;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public sealed class ComponentDocumentService(
    IComponentDocumentRenderer renderer) : IComponentDocumentService
{
    private const int MaximumItems = 100;

    public ServiceResult<ComponentDocumentsResponse> Generate(
        ComponentDocumentRequest request)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return ServiceResult<ComponentDocumentsResponse>.Validation(errors);
        }

        var normalizedRequest = request with
        {
            VehicleNumber = request.VehicleNumber.Trim(),
            Items = request.Items
                .Select(item => item with
                {
                    Name = item.Name.Trim(),
                    Unit = item.Unit.Trim()
                })
                .ToArray()
        };
        return ServiceResult<ComponentDocumentsResponse>.Success(
            new ComponentDocumentsResponse(renderer.Render(normalizedRequest)));
    }

    private static Dictionary<string, string[]> Validate(
        ComponentDocumentRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.VehicleNumber))
        {
            errors[nameof(request.VehicleNumber)] = ["Укажите номер техники."];
        }

        if (request.Items.Count == 0)
        {
            errors[nameof(request.Items)] = ["Выберите хотя бы один компонент."];
            return errors;
        }

        if (request.Items.Count > MaximumItems)
        {
            errors[nameof(request.Items)] =
                [$"Можно сформировать не более {MaximumItems} компонентов за один раз."];
        }

        if (request.Items.Any(item => string.IsNullOrWhiteSpace(item.Name)))
        {
            errors[nameof(ComponentDocumentItem.Name)] =
                ["У каждого компонента должно быть наименование."];
        }

        if (request.Items.Any(item => string.IsNullOrWhiteSpace(item.Unit)))
        {
            errors[nameof(ComponentDocumentItem.Unit)] =
                ["У каждого компонента должна быть единица измерения."];
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            errors[nameof(ComponentDocumentItem.Quantity)] =
                ["Количество должно быть больше нуля."];
        }

        return errors;
    }
}
