using Jarvis.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Jarvis.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var properties = typeof(TRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            var value = property.GetValue(request);
            if (value is string str && string.IsNullOrWhiteSpace(str))
            {
                _logger.LogWarning("Property {PropertyName} is null or whitespace on {RequestName}",
                    property.Name, typeof(TRequest).Name);
            }
        }

        return await next();
    }
}