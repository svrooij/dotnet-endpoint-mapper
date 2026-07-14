using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Svrooij.EndpointMapper.Generators;

internal static class SelectDtoTextHelper
{
    private static readonly Regex EntityParamRegex = new Regex(@"(?<!\w)e\.", RegexOptions.Compiled);

    private static string? GetSelectExpression(IPropertySymbol property)
    {
        var attr = property.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.Name == "SelectExpressionAttribute" &&
            a.AttributeClass?.ContainingNamespace.ToDisplayString() == "Svrooij.EndpointMapper");

        if (attr == null || attr.ConstructorArguments.Length != 1)
            return null;

        return attr.ConstructorArguments[0].Value as string;
    }

    internal static void GenerateSelectDtoExtensionMethod(StringBuilder sourceBuilder, string dtoClassName, string dtoFullName, string entityFullName, INamedTypeSymbol dtoClass, ITypeSymbol entityType)
    {
        // Get all properties from the DTO class
        var dtoProperties = dtoClass.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        // Separate nullable and non-nullable properties
        // Properties with [SelectExpression] use a custom expression; nullability still controls whether they are selectable
        var nullableProperties = dtoProperties.Where(p => p.NullableAnnotation == NullableAnnotation.Annotated).ToList();
        var nonNullableProperties = dtoProperties.Where(p => p.NullableAnnotation != NullableAnnotation.Annotated).ToList();

        // Determine if we use bitmask or fallback to HashSet
        bool useBitmask = nullableProperties.Count <= 64;

        sourceBuilder.AppendLine("/// <summary>");
        sourceBuilder.AppendLine($"/// Maps {entityType.Name} instances to {dtoClassName}, selecting only specified properties.");
        sourceBuilder.AppendLine("/// </summary>");
        sourceBuilder.AppendLine(GeneratorConstants.ExcludeFromCodeCoverageAttributes());
        sourceBuilder.AppendLine($"public static class {dtoClassName}Extensions");
        sourceBuilder.AppendLine("{");

        if (useBitmask && nullableProperties.Count > 0)
        {
            // Generate property flags enum
            sourceBuilder.AppendLine("  // Property flags for bitwise operations");
            for (int i = 0; i < nullableProperties.Count; i++)
            {
                var propName = nullableProperties[i].Name;
                sourceBuilder.AppendLine($"  private const long {propName}Flag = 1L << {i};");
            }

            // Generate a constant for all properties
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("  // Bitmask with all nullable properties selected");
            long allPropertiesMask = (1L << nullableProperties.Count) - 1;
            sourceBuilder.AppendLine($"  private const long AllPropertiesMask = {allPropertiesMask}L;");
            sourceBuilder.AppendLine();

            // Generate validation array
            var validPropertiesArray = string.Join(", ", nullableProperties.Select(p => $"\"{p.Name.ToLowerInvariant()}\""));
            sourceBuilder.AppendLine($"  private static readonly string[] ValidProperties = new[] {{ {validPropertiesArray} }};");
            sourceBuilder.AppendLine();
        }
        else if (nullableProperties.Count > 0)
        {
            // Fallback to HashSet for >64 properties
            var validPropertiesArray = string.Join(", ", nullableProperties.Select(p => $"\"{p.Name.ToLowerInvariant()}\""));
            sourceBuilder.AppendLine($"  private static readonly string[] ValidProperties = new[] {{ {validPropertiesArray} }};");
            sourceBuilder.AppendLine();
        }

        sourceBuilder.AppendLine("  /// <summary>");
        sourceBuilder.AppendLine($"  /// Projects a query of {entityType.Name} to {dtoClassName} with selective property mapping.");
        sourceBuilder.AppendLine("  /// Use `*` to select all properties.");
        sourceBuilder.AppendLine("  /// </summary>");
        sourceBuilder.AppendLine($"  /// <param name=\"query\">The IQueryable of {entityType.Name}</param>");
        sourceBuilder.AppendLine("  /// <param name=\"properties\">Comma-separated property names to include (e.g., \"id,name,email\"), or `*` for all properties</param>");
        sourceBuilder.AppendLine($"  /// <returns>An IQueryable of {dtoClassName}</returns>");
        sourceBuilder.AppendLine($"  public static IQueryable<{dtoClassName}> Select{dtoClassName}(this IQueryable<{entityFullName}> query, string properties)");
        sourceBuilder.AppendLine("  {");

        if (nullableProperties.Count > 0)
        {
            if (useBitmask)
            {
                sourceBuilder.AppendLine("    long selectedProps = ParseProperties(properties);");
            }
            else
            {
                sourceBuilder.AppendLine("    var selectedProps = new System.Collections.Generic.HashSet<string>(");
                sourceBuilder.AppendLine("      (properties ?? string.Empty).Equals(\"*\", System.StringComparison.Ordinal)");
                sourceBuilder.AppendLine("        ? ValidProperties");
                sourceBuilder.AppendLine("        : (properties ?? string.Empty)");
                sourceBuilder.AppendLine("            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)");
                sourceBuilder.AppendLine("            .Select(p => p.ToLowerInvariant())");
                sourceBuilder.AppendLine("            .Where(p => ValidProperties.Contains(p)),");
                sourceBuilder.AppendLine("      System.StringComparer.OrdinalIgnoreCase");
                sourceBuilder.AppendLine("    );");
            }
        }
        else
        {
            sourceBuilder.AppendLine("    // No nullable properties, so always include all (non-nullable only)");
            if (useBitmask)
            {
                sourceBuilder.AppendLine("    long selectedProps = 0;");
            }
            else
            {
                sourceBuilder.AppendLine("    var selectedProps = new System.Collections.Generic.HashSet<string>(");
                sourceBuilder.AppendLine("      System.StringComparer.OrdinalIgnoreCase");
                sourceBuilder.AppendLine("    );");
            }
        }

        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"    return query.Select(e => e.Select{dtoClassName}(selectedProps));");
        sourceBuilder.AppendLine("  }");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("  /// <summary>");
        sourceBuilder.AppendLine($"  /// Maps a single {entityType.Name} instance to {dtoClassName} with selective property mapping.");
        sourceBuilder.AppendLine("  /// Use `*` to select all properties.");
        sourceBuilder.AppendLine("  /// </summary>");
        sourceBuilder.AppendLine($"  /// <param name=\"entity\">The {entityType.Name} instance</param>");
        sourceBuilder.AppendLine("  /// <param name=\"properties\">Comma-separated property names to include (e.g., \"id,name,email\"), or `*` for all properties</param>");
        sourceBuilder.AppendLine($"  /// <returns>A {dtoClassName} instance with selected properties populated</returns>");
        sourceBuilder.AppendLine($"  public static {dtoClassName} Select{dtoClassName}(this {entityFullName} entity, string properties)");
        sourceBuilder.AppendLine("  {");

        if (nullableProperties.Count > 0)
        {
            if (useBitmask)
            {
                sourceBuilder.AppendLine("    long selectedProps = ParseProperties(properties);");
            }
            else
            {
                sourceBuilder.AppendLine("    var selectedProps = new System.Collections.Generic.HashSet<string>(");
                sourceBuilder.AppendLine("      (properties ?? string.Empty).Equals(\"*\", System.StringComparison.Ordinal)");
                sourceBuilder.AppendLine("        ? ValidProperties");
                sourceBuilder.AppendLine("        : (properties ?? string.Empty)");
                sourceBuilder.AppendLine("            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)");
                sourceBuilder.AppendLine("            .Select(p => p.ToLowerInvariant())");
                sourceBuilder.AppendLine("            .Where(p => ValidProperties.Contains(p)),");
                sourceBuilder.AppendLine("      System.StringComparer.OrdinalIgnoreCase");
                sourceBuilder.AppendLine("    );");
            }
        }
        else
        {
            sourceBuilder.AppendLine("    // No nullable properties, so always include all (non-nullable only)");
            if (useBitmask)
            {
                sourceBuilder.AppendLine("    long selectedProps = 0;");
            }
            else
            {
                sourceBuilder.AppendLine("    var selectedProps = new System.Collections.Generic.HashSet<string>(");
                sourceBuilder.AppendLine("      System.StringComparer.OrdinalIgnoreCase");
                sourceBuilder.AppendLine("    );");
            }
        }

        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"    return entity.Select{dtoClassName}(selectedProps);");
        sourceBuilder.AppendLine("  }");
        sourceBuilder.AppendLine();

        // Generate internal helper method
        if (useBitmask && nullableProperties.Count > 0)
        {
            sourceBuilder.AppendLine("  /// <summary>");
            sourceBuilder.AppendLine($"  /// Maps a single {entityType.Name} instance to {dtoClassName} using a pre-parsed bitmask.");
            sourceBuilder.AppendLine("  /// This internal method is used by the IQueryable extension to avoid parsing the properties string multiple times.");
            sourceBuilder.AppendLine("  /// </summary>");
            sourceBuilder.AppendLine($"  /// <param name=\"entity\">The {entityType.Name} instance</param>");
            sourceBuilder.AppendLine("  /// <param name=\"selectedProps\">Bitmask of selected properties</param>");
            sourceBuilder.AppendLine($"  /// <returns>A {dtoClassName} instance with selected properties populated</returns>");
            sourceBuilder.AppendLine($"  internal static {dtoClassName} Select{dtoClassName}(this {entityFullName} entity, long selectedProps)");
        }
        else if (nullableProperties.Count > 0)
        {
            sourceBuilder.AppendLine("  /// <summary>");
            sourceBuilder.AppendLine($"  /// Maps a single {entityType.Name} instance to {dtoClassName} using a pre-parsed property set.");
            sourceBuilder.AppendLine("  /// This internal method is used by the IQueryable extension to avoid parsing the properties string multiple times.");
            sourceBuilder.AppendLine("  /// </summary>");
            sourceBuilder.AppendLine($"  /// <param name=\"entity\">The {entityType.Name} instance</param>");
            sourceBuilder.AppendLine("  /// <param name=\"selectedProps\">HashSet of lowercase property names to include (for nullable properties only)</param>");
            sourceBuilder.AppendLine($"  /// <returns>A {dtoClassName} instance with selected properties populated</returns>");
            sourceBuilder.AppendLine($"  internal static {dtoClassName} Select{dtoClassName}(this {entityFullName} entity, System.Collections.Generic.HashSet<string> selectedProps)");
        }
        else
        {
            sourceBuilder.AppendLine("  /// <summary>");
            sourceBuilder.AppendLine($"  /// Maps a single {entityType.Name} instance to {dtoClassName} using a pre-parsed property set.");
            sourceBuilder.AppendLine("  /// </summary>");
            sourceBuilder.AppendLine($"  /// <param name=\"entity\">The {entityType.Name} instance</param>");
            sourceBuilder.AppendLine("  /// <param name=\"selectedProps\">Unused for DTOs with only non-nullable properties</param>");
            sourceBuilder.AppendLine($"  /// <returns>A {dtoClassName} instance with selected properties populated</returns>");
            sourceBuilder.AppendLine($"  internal static {dtoClassName} Select{dtoClassName}(this {entityFullName} entity, long selectedProps)");
        }

        sourceBuilder.AppendLine("  {");
        sourceBuilder.AppendLine($"    return new {dtoFullName}");
        sourceBuilder.AppendLine("    {");

        foreach (var prop in dtoProperties)
        {
            var propName = prop.Name;
            var isNonNullable = nonNullableProperties.Any(p => p.Name == propName);
            var selectExpression = GetSelectExpression(prop);
            var valueExpression = selectExpression != null
                ? EntityParamRegex.Replace(selectExpression, "entity.")
                : $"entity.{propName}";

            if (isNonNullable)
            {
                // Non-nullable: always include
                sourceBuilder.AppendLine($"      {propName} = {valueExpression},");
            }
            else if (useBitmask)
            {
                // Nullable with bitmask
                int flagIndex = nullableProperties.FindIndex(p => p.Name == propName);
                sourceBuilder.AppendLine($"      {propName} = (selectedProps & {propName}Flag) != 0 ? {valueExpression} : null,");
            }
            else
            {
                // Nullable with HashSet fallback
                sourceBuilder.AppendLine($"      {propName} = selectedProps.Contains(\"{propName.ToLowerInvariant()}\") ? {valueExpression} : null,");
            }
        }

        sourceBuilder.AppendLine("    };");
        sourceBuilder.AppendLine("  }");

        // Generate ParseProperties helper method if using bitmask
        if (useBitmask && nullableProperties.Count > 0)
        {
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("  /// <summary>");
            sourceBuilder.AppendLine("  /// Parses the properties string and returns a bitmask of selected properties.");
            sourceBuilder.AppendLine("  /// Use `*` to select all properties.");
            sourceBuilder.AppendLine("  /// </summary>");
            sourceBuilder.AppendLine("  private static long ParseProperties(string properties)");
            sourceBuilder.AppendLine("  {");
            sourceBuilder.AppendLine("    // Check for wildcard to select all properties");
            sourceBuilder.AppendLine("    if ((properties ?? string.Empty).Equals(\"*\", System.StringComparison.Ordinal))");
            sourceBuilder.AppendLine("      return AllPropertiesMask;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("    long flags = 0;");
            sourceBuilder.AppendLine("    if (string.IsNullOrEmpty(properties)) return flags;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("    var selected = new System.Collections.Generic.HashSet<string>(");
            sourceBuilder.AppendLine("      properties");
            sourceBuilder.AppendLine("        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)");
            sourceBuilder.AppendLine("        .Select(p => p.ToLowerInvariant())");
            sourceBuilder.AppendLine("        .Where(p => ValidProperties.Contains(p)),");
            sourceBuilder.AppendLine("      System.StringComparer.OrdinalIgnoreCase");
            sourceBuilder.AppendLine("    );");
            sourceBuilder.AppendLine();

            for (int i = 0; i < nullableProperties.Count; i++)
            {
                var propName = nullableProperties[i].Name;
                sourceBuilder.AppendLine($"    if (selected.Contains(\"{propName.ToLowerInvariant()}\")) flags |= {propName}Flag;");
            }

            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("    return flags;");
            sourceBuilder.AppendLine("  }");
        }

        sourceBuilder.AppendLine("}");
        sourceBuilder.AppendLine();
    }
}
