using MyApp.Application.Abstractions;
using MyApp.Application.Common;
using MyApp.Application.DTO;

namespace MyApp.Application.Services;

public sealed class ComponentDocumentService(
    IComponentDocumentRenderer renderer,
    IUserRepository userRepository,
    IResponsibleEmployeeService responsibleEmployeeService,
    IRequirementJournalRepository requirementJournalRepository) :
    IComponentDocumentService
{
    private const int MaximumItems = 100;

    public async Task<ServiceResult<ComponentDocumentsResponse>> GenerateAsync(
        ComponentDocumentRequest request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return ServiceResult<ComponentDocumentsResponse>.Validation(errors);
        }

        var user = await userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<ComponentDocumentsResponse>.Unauthorized();
        }

        var responsibleEmployee = await responsibleEmployeeService.ResolveAsync(
            request.SourceTable,
            request.ResponsibleEmployee,
            cancellationToken);
        if (responsibleEmployee.Status != ServiceResultStatus.Success ||
            responsibleEmployee.Value is null)
        {
            return new ServiceResult<ComponentDocumentsResponse>(
                responsibleEmployee.Status,
                Message: responsibleEmployee.Message,
                Errors: responsibleEmployee.Errors);
        }

        var normalizedRequest = request with
        {
            VehicleNumber = request.VehicleNumber.Trim(),
            AuthorPosition = user.Profession.Name,
            AuthorName = BuildAuthorName(
                user.FirstName,
                user.MiddleName,
                user.LastName),
            IssuerPosition = responsibleEmployee.Value.Profession,
            IssuerName = BuildAuthorName(
                responsibleEmployee.Value.FirstName,
                responsibleEmployee.Value.Patronymic,
                responsibleEmployee.Value.LastName),
            Items = request.Items
                .Select(item => item with
                {
                    Name = item.Name.Trim(),
                    Unit = item.Unit.Trim()
                })
                .ToArray()
        };
        var documents = renderer.Render(normalizedRequest);
        var document = documents.Single();
        await requirementJournalRepository.SaveAsync(
            user.Id,
            BuildFullName(user.FirstName, user.MiddleName, user.LastName),
            BuildFullName(
                responsibleEmployee.Value.FirstName,
                responsibleEmployee.Value.Patronymic,
                responsibleEmployee.Value.LastName),
            normalizedRequest.VehicleNumber,
            normalizedRequest.SourceTable,
            normalizedRequest.Items,
            document,
            cancellationToken);
        return ServiceResult<ComponentDocumentsResponse>.Success(
            new ComponentDocumentsResponse(documents));
    }

    private static string BuildAuthorName(
        string firstName,
        string middleName,
        string lastName)
    {
        var initials = new[] { firstName, middleName }
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => $"{name.Trim()[0]}.");
        return $"{string.Concat(initials)} {lastName.Trim()}".Trim();
    }

    private static string BuildFullName(
        string firstName,
        string middleName,
        string lastName) =>
        string.Join(
            ' ',
            new[] { lastName, firstName, middleName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim()));

    private static Dictionary<string, string[]> Validate(
        ComponentDocumentRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.VehicleNumber))
        {
            errors[nameof(request.VehicleNumber)] = ["Укажите номер техники."];
        }

        if (request.ResponsibleEmployee is null)
        {
            errors[nameof(request.ResponsibleEmployee)] =
                ["Выберите ответственное лицо за выдачу."];
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

        if (request.Items.Any(item =>
                item.AvailableQuantity < 0 ||
                item.Quantity > item.AvailableQuantity))
        {
            errors[nameof(ComponentDocumentItem.AvailableQuantity)] =
                ["Количество к списанию не может превышать остаток."];
        }

        return errors;
    }
}
