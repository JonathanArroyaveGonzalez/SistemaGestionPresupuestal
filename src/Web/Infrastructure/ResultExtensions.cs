using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAPFIAI.Application.Common.Models;

namespace SAPFIAI.Web.Infrastructure;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok();
        }

        return Results.Problem(
            statusCode: GetStatusCode(result.Error.Code),
            title: "Bad Request",
            detail: result.Error.Description,
            extensions: new Dictionary<string, object?>
            {
                { "errorCode", result.Error.Code }
            });
    }

    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return Results.Problem(
            statusCode: GetStatusCode(result.Error.Code),
            title: "Bad Request",
            detail: result.Error.Description,
            extensions: new Dictionary<string, object?>
            {
                { "errorCode", result.Error.Code }
            });
    }

    public static IResult ToCreatedResult<T>(this Result<T> result, Func<T, string> locationFactory)
    {
        if (result.IsSuccess)
        {
            return Results.Created(locationFactory(result.Value), result.Value);
        }

        return Results.Problem(
            statusCode: GetStatusCode(result.Error.Code),
            title: "Bad Request",
            detail: result.Error.Description,
            extensions: new Dictionary<string, object?>
            {
                { "errorCode", result.Error.Code }
            });
    }

    private static int GetStatusCode(string errorCode)
    {
        if (errorCode.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status404NotFound;
        }

        if (errorCode.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status401Unauthorized;
        }
        
        if (errorCode.Contains("Forbidden", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status403Forbidden;
        }

        return StatusCodes.Status400BadRequest;
    }
}
