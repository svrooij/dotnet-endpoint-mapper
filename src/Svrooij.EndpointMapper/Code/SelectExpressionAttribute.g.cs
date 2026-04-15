using System;

namespace Svrooij.EndpointMapper;

/// <summary>
/// Specifies a LINQ expression used to compute a DTO property from the source entity,
/// for use with <see cref="GenerateSelectAttribute"/>.
/// </summary>
/// <remarks>
/// Use this attribute when the DTO property does not have a direct counterpart on the source entity
/// and needs to be derived via a custom expression.
///
/// The expression must be a valid C# lambda body where the parameter represents the source entity.
/// The parameter name in the expression must match the lambda parameter name used by the generator (<c>e</c>).
///
/// Example usage:
/// <code>
/// [GenerateSelect(typeof(Order))]
/// public class OrderDto
/// {
///     public int Id { get; set; }
///
///     // Computed from a nested property — Order has no direct "CustomerName" property
///     [SelectExpression("e.Customer.Name")]
///     public string? CustomerName { get; set; }
///
///     // Always-computed non-nullable property
///     [SelectExpression("e.Lines.Sum(l => l.Price)")]
///     public decimal TotalPrice { get; set; }
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SelectExpressionAttribute : Attribute
{
    /// <summary>
    /// The LINQ expression body used to compute this property from the source entity.
    /// The entity is referenced as <c>e</c> in the expression.
    /// </summary>
    public string Expression { get; }

    /// <summary>
    /// Marks a DTO property with a custom LINQ expression to compute its value from the source entity.
    /// </summary>
    /// <param name="expression">
    /// A C# expression string where <c>e</c> refers to the source entity instance.
    /// For example: <c>"e.Address.City"</c> or <c>"e.Items.Count()"</c>.
    /// </param>
    public SelectExpressionAttribute(string expression)
    {
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }
}
