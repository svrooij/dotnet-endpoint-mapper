using System;

namespace Svrooij.EndpointMapper;

/// <summary>
/// Attribute to mark a class for selective property mapping from a specified entity type, see the <see cref="GenerateSelectAttribute.GenerateSelectAttribute(Type)"/> for more details.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.CodeDom.Compiler.GeneratedCode("Svrooij.EndpointMapper", "1.0.0")]
public sealed class GenerateSelectAttribute : Attribute
{
    /// <summary>
    /// The entity type to map from.
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// Marks a class as an object used for selective property mapping.
    /// </summary>
    /// <remarks>
    /// The source generator will create extension methods for mapping from the specified entity type.
    /// 
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
    /// <param name="entityType">The entity type to map from</param>
    public GenerateSelectAttribute(Type entityType)
    {
        EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
    }
}
