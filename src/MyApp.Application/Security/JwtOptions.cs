namespace MyApp.Application.Security;

public sealed record JwtOptions(string Key, string Issuer, string Audience);
