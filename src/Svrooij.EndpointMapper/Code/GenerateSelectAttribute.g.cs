using System;

namespace Svrooij.EndpointMapper;

/// <summary>
/// Marks a class as a DTO that supports selective property mapping.
/// The source generator will create extension methods for mapping from the specified entity type.
/// </summary>
/// <remarks>
/// Example usage:
/// <code>
/// [GenerateSelect(typeof(User))]
/// public class UserDto
/// {
///     public int Id { get; set; }
///     public string? Name { get; set; }
/// }
/// </code>
/// 
/// This generates an extension method: <c>userDto = user.SelectUserDto("id,name")</c>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class GenerateSelectAttribute : Attribute
{
    /// <summary>
    /// The entity type to map from.
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateSelectAttribute"/> class.
    /// </summary>
    /// <param name="entityType">The entity type to map from</param>
    public GenerateSelectAttribute(Type entityType)
    {
        EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
    }
}
