using System.Security.Claims;
using BrainTrain.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BrainTrain.Api.Endpoints;

public static class EndpointHelpers
{
    public static long UserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue("sub")
                  ?? throw new GameError(401, "no_token", "Token inválido.");
        return long.Parse(sub);
    }

    public static IResult Problem(this GameError e) => Results.Problem(
        statusCode: e.Status,
        title: e.Code,
        detail: e.Message);

    /// <summary>Convierte GameError en ProblemDetails uniformes para toda la API.</summary>
    public static async Task GameErrorMiddleware(HttpContext ctx, RequestDelegate next)
    {
        try
        {
            await next(ctx);
        }
        catch (GameError e)
        {
            ctx.Response.StatusCode = e.Status;
            await ctx.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = e.Status,
                Title = e.Code,
                Detail = e.Message
            });
        }
    }
}
