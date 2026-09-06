namespace MyApp.Application.DTO;

public record ProfessionRequest(string Profession);

public sealed record ProfessionResponse(Guid Id, string Profession);
