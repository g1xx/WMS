using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Common;

namespace Warehouse.Api.Common;

public static class ResultExtensions
{
    public static ActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => new NotFoundObjectResult(result.Error),
            ResultErrorType.Conflict => new ConflictObjectResult(result.Error),
            _ => new BadRequestObjectResult(result.Error)
        };
    }
}
