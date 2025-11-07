using System.Text;

namespace Svrooij.EndpointMapper.Generators;

internal static class ValidatorTextHelper
{
    internal static void GenerateValidationFilter(
        StringBuilder sourceBuilder,
 string validatorClassName,
        string validatorFullName,
        string dtoClassName,
        string dtoFullName)
    {
        // Generate the validation filter class
        sourceBuilder.AppendLine($"  /// <summary>");
        sourceBuilder.AppendLine($"  /// Validation filter for {dtoClassName} using {validatorClassName}.");
        sourceBuilder.AppendLine($"  /// </summary>");
        sourceBuilder.AppendLine($"  internal class {validatorClassName}Filter : IEndpointFilter");
        sourceBuilder.AppendLine($"  {{");
        sourceBuilder.AppendLine($"    private readonly IValidator<{dtoFullName}> _validator;");
        sourceBuilder.AppendLine($"    ");
        sourceBuilder.AppendLine($"    /// <summary>");
        sourceBuilder.AppendLine($"    /// Initializes a new instance of {validatorClassName}Filter.");
        sourceBuilder.AppendLine($"    /// </summary>");
        sourceBuilder.AppendLine($"    public {validatorClassName}Filter()");
        sourceBuilder.AppendLine($"    {{");
        sourceBuilder.AppendLine($"      _validator = new {validatorFullName}();");
        sourceBuilder.AppendLine($"    }}");
        sourceBuilder.AppendLine($"    ");
        sourceBuilder.AppendLine($"    /// <summary>");
        sourceBuilder.AppendLine($"  /// Invokes the validation filter.");
        sourceBuilder.AppendLine($"    /// </summary>");
        sourceBuilder.AppendLine($"    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)");
        sourceBuilder.AppendLine($"    {{");
        sourceBuilder.AppendLine($"      var dto = context.Arguments.OfType<{dtoFullName}>().FirstOrDefault();");
        sourceBuilder.AppendLine($"      if (dto != null)");
        sourceBuilder.AppendLine($"      {{");
        sourceBuilder.AppendLine($"        var validationResult = await _validator.ValidateAsync(dto);");
        sourceBuilder.AppendLine($"        if (!validationResult.IsValid)");
        sourceBuilder.AppendLine($"        {{");
        sourceBuilder.AppendLine($"          var errors = validationResult.Errors");
        sourceBuilder.AppendLine($"            .GroupBy(e => e.PropertyName)");
        sourceBuilder.AppendLine($"            .ToDictionary(");
        sourceBuilder.AppendLine($"              g => g.Key,");
        sourceBuilder.AppendLine($"              g => g.Select(e => e.ErrorMessage).ToArray()");
        sourceBuilder.AppendLine($"            );");
        sourceBuilder.AppendLine($"          return Results.BadRequest(errors);");
        sourceBuilder.AppendLine($"        }}");
        sourceBuilder.AppendLine($"      }}");
        sourceBuilder.AppendLine($"      return await next(context);");
        sourceBuilder.AppendLine($"    }}");
        sourceBuilder.AppendLine($"  }}");
        sourceBuilder.AppendLine($"  ");

        // Generate the extension method
        sourceBuilder.AppendLine($"  /// <summary>");
        sourceBuilder.AppendLine($"  /// Extension method to add validation filter for {dtoClassName}.");
        sourceBuilder.AppendLine($"  /// </summary>");
        sourceBuilder.AppendLine($"  internal static class {validatorClassName}Extensions");
        sourceBuilder.AppendLine($"  {{");
        sourceBuilder.AppendLine($"    /// <summary>");
        sourceBuilder.AppendLine($"    /// Adds validation for {dtoClassName} using {validatorClassName}.");
        sourceBuilder.AppendLine($"    /// </summary>");
        sourceBuilder.AppendLine($"    /// <param name=\"builder\">The RouteHandlerBuilder to add validation to.</param>");
        sourceBuilder.AppendLine($"    /// <returns>The RouteHandlerBuilder with validation added for chaining.</returns>");
        sourceBuilder.AppendLine($"    public static RouteHandlerBuilder AddValidationWith{validatorClassName}(this RouteHandlerBuilder builder)");
        sourceBuilder.AppendLine($"      => builder.AddEndpointFilter<{validatorClassName}Filter>().ProducesValidationProblem();");
        sourceBuilder.AppendLine($"  }}");
        sourceBuilder.AppendLine($"  ");
    }
}
