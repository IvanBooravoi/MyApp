namespace MyApp.Application.DTO {
    public record RegisterRequest(string UserName, string Email, string Password);
    public record LoginRequest(string UserNameOrEmail, string Password);
    public record AuthResponse(string Token);
}
