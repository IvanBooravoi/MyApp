using MyApp.Application.Common;

namespace MyApp.API.Endpoints;

internal static class EndpointResultMapper
{
    public static IResult ToHttpResult<T>(
        this ServiceResult<T> result,
        Func<T, IResult> onSuccess)
    {
        return result.Status switch
        {
            ServiceResultStatus.Success when result.Value is not null =>
                onSuccess(result.Value),
            ServiceResultStatus.Unauthorized => Results.Unauthorized(),
            ServiceResultStatus.NotFound => Results.NotFound(),
            ServiceResultStatus.Conflict =>
                Results.Conflict(new { message = result.Message }),
            ServiceResultStatus.BadRequest =>
                Results.BadRequest(result.Message),
            ServiceResultStatus.ValidationError =>
                Results.ValidationProblem(result.Errors
                    ?? throw new InvalidOperationException(
                        "A validation result must contain errors.")),
            _ => throw new InvalidOperationException(
                $"Unsupported service result status '{result.Status}'.")
        };
    }
}
