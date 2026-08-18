using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MyApp.Application.DTO;
using MyApp.Application.Services;

namespace MyApp.API.Endpoints;

public static class UserProfileEndpoints
{
    private const long MaximumAvatarSize = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedAvatarTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    public static IEndpointRouteBuilder MapUserProfileEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/profile").RequireAuthorization();

        group.MapGet("", async (
            ClaimsPrincipal principal,
            IUserService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }
            return (await service.GetProfileAsync(userId, cancellationToken))
                .ToHttpResult(Results.Ok);
        });

        group.MapPut("", async (
            UpdateProfileRequest request,
            ClaimsPrincipal principal,
            IUserService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }
            return (await service.UpdateProfileAsync(
                    userId, request, cancellationToken))
                .ToHttpResult(Results.Ok);
        });

        group.MapGet("/avatar", async (
            ClaimsPrincipal principal,
            IUserService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }
            return (await service.GetAvatarAsync(userId, cancellationToken))
                .ToHttpResult(avatar => Results.File(
                    avatar.Content,
                    avatar.ContentType,
                    enableRangeProcessing: false));
        });

        group.MapPost("/avatar", async (
            IFormFile avatar,
            ClaimsPrincipal principal,
            IUserService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }
            if (avatar.Length is <= 0 or > MaximumAvatarSize ||
                !AllowedAvatarTypes.Contains(avatar.ContentType))
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["avatar"] =
                        [
                            "Выберите изображение JPEG, PNG или WebP размером не более 2 МБ."
                        ]
                    });
            }

            await using var stream = new MemoryStream();
            await avatar.CopyToAsync(stream, cancellationToken);
            return (await service.UpdateAvatarAsync(
                    userId,
                    stream.ToArray(),
                    avatar.ContentType,
                    cancellationToken))
                .ToHttpResult(Results.Ok);
        }).DisableAntiforgery();

        group.MapDelete("/avatar", async (
            ClaimsPrincipal principal,
            IUserService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }
            return (await service.DeleteAvatarAsync(userId, cancellationToken))
                .ToHttpResult(Results.Ok);
        });

        return endpoints;
    }

    private static bool TryGetUserId(
        ClaimsPrincipal principal,
        out Guid userId)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out userId);
    }
}
