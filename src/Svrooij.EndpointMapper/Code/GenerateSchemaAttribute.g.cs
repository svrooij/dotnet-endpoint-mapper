using System;

namespace Svrooij.EndpointMapper;

/// <summary>
/// Marks a class as a Type that can should be validated using the specific validator type.
/// </summary>
/// <remarks>
/// Example usage:
/// <code>
/// [GenerateSchema(typeof(UserDtoValidator))]
/// public class UserDto
/// {
///     public int Id { get; set; }
///     public string? Name { get; set; }
/// }
/// </code>
/// 
/// This adds a schema transformer for the UserDto based on the rules defined in UserDtoValidator, to the generated FluentValidationSchemaTransformer.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class GenerateSchemaAttribute : Attribute
{
    /// <summary>
    /// The validator type to use for generating the schema.
    /// </summary>
    public Type ValidatorType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateSchemaAttribute"/> class.
    /// </summary>
    /// <param name="validatorType">Validator to use the schema from</param>
    public GenerateSchemaAttribute(Type validatorType)
    {
        ValidatorType = validatorType ?? throw new ArgumentNullException(nameof(validatorType));
    }
}
