using ADScimApi.SCIM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ADScimApi.Filters;

public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;
    private readonly IWebHostEnvironment _env;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, context.Exception.Message);

        var error = new ScimError
        {
            Status = "500",
            Detail = _env.IsDevelopment() 
                ? context.Exception.Message 
                : "An internal server error occurred."
        };

        if (context.Exception is ArgumentException)
        {
            error.Status = "400";
            error.ScimType = "invalidValue";
        }
        else if (context.Exception is InvalidOperationException)
        {
            error.Status = "409";
            error.ScimType = "uniqueness";
        }
        else if (context.Exception is UnauthorizedAccessException)
        {
            error.Status = "403";
            error.ScimType = "forbidden";
        }
        else if (context.Exception is KeyNotFoundException)
        {
            error.Status = "404";
            error.ScimType = "notFound";
        }

        context.Result = new ObjectResult(error)
        {
            StatusCode = int.Parse(error.Status)
        };

        context.ExceptionHandled = true;
    }
}