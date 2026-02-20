using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Svrooij.EndpointMapper;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EndpointMapperAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor MissingParameterlessConstructor = new DiagnosticDescriptor(
        id: "SVEM001",
        title: "IMapEndpoint implementation must have a parameterless constructor",
        messageFormat: "Class '{0}' implements IMapEndpoint but does not have a parameterless constructor. The source generator requires a parameterless constructor to instantiate endpoint classes.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Classes implementing IMapEndpoint must have a parameterless constructor so they can be instantiated by the generated code.",
        helpLinkUri: "https://github.com/svrooij/dotnet-endpoint-mapper#usage");

    public static readonly DiagnosticDescriptor GenerateSelectMissingParameterlessConstructor = new DiagnosticDescriptor(
        id: "SVEM002",
        title: "GenerateSelect DTO must have a parameterless constructor",
        messageFormat: "Class '{0}' is decorated with [GenerateSelect] but does not have a parameterless constructor. The source generator requires a parameterless constructor to instantiate the DTO.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Classes decorated with [GenerateSelect] must have a parameterless constructor so they can be instantiated by the generated mapping methods.",
        helpLinkUri: "https://github.com/svrooij/dotnet-endpoint-mapper#usage");

    public static readonly DiagnosticDescriptor GenerateSelectMissingSourceProperty = new DiagnosticDescriptor(
        id: "SVEM003",
        title: "GenerateSelect DTO property not found in source entity",
        messageFormat: "Property '{0}' on class '{1}' decorated with [GenerateSelect] does not exist on the source entity type '{2}' or has an incompatible type",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "All public properties on a [GenerateSelect] DTO must have corresponding properties on the source entity type with compatible types.",
        helpLinkUri: "https://github.com/svrooij/dotnet-endpoint-mapper#usage");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            MissingParameterlessConstructor,
            GenerateSelectMissingParameterlessConstructor,
            GenerateSelectMissingSourceProperty);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

        if (classSymbol == null)
            return;

        // Check if the class implements IMapEndpoint
        var implementsIMapEndpoint = classSymbol.AllInterfaces.Any(i =>
            i.Name == "IMapEndpoint" &&
            i.ContainingNamespace.ToDisplayString() == "Svrooij.EndpointMapper");

        if (implementsIMapEndpoint)
        {
            AnalyzeIMapEndpoint(context, classDeclaration, classSymbol);
        }

        // Check if the class has GenerateSelect attribute
        var hasGenerateSelectAttribute = classSymbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "GenerateSelectAttribute" &&
            attr.AttributeClass?.ContainingNamespace.ToDisplayString() == "Svrooij.EndpointMapper");

        if (hasGenerateSelectAttribute)
        {
            AnalyzeGenerateSelect(context, classDeclaration, classSymbol);
        }
    }

    private static void AnalyzeIMapEndpoint(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration, INamedTypeSymbol classSymbol)
    {
        // Check if the class has a parameterless constructor
        var hasParameterlessConstructor = HasParameterlessConstructor(classSymbol);

        if (!hasParameterlessConstructor)
        {
            var diagnostic = Diagnostic.Create(
                MissingParameterlessConstructor,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeGenerateSelect(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration, INamedTypeSymbol classSymbol)
    {
        // Check if the class has a parameterless constructor
        var hasParameterlessConstructor = HasParameterlessConstructor(classSymbol);

        if (!hasParameterlessConstructor)
        {
            var diagnostic = Diagnostic.Create(
                GenerateSelectMissingParameterlessConstructor,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name);

            context.ReportDiagnostic(diagnostic);
            return; // Can't proceed without parameterless constructor
        }

        // Get the GenerateSelect attribute to find the source entity type
        var generateSelectAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(attr =>
                attr.AttributeClass?.Name == "GenerateSelectAttribute" &&
                attr.AttributeClass?.ContainingNamespace.ToDisplayString() == "Svrooij.EndpointMapper");

        if (generateSelectAttribute == null || generateSelectAttribute.ConstructorArguments.Length != 1)
            return;

        var entityTypeArg = generateSelectAttribute.ConstructorArguments[0];
        if (entityTypeArg.Value is not INamedTypeSymbol entityType)
            return;

        // Check each public property on the DTO
        var dtoProperties = classSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        // Get all public properties from the entity type, including inherited properties
        var entityProperties = GetAllPublicProperties(entityType);

        foreach (var dtoProp in dtoProperties)
        {
            // Find matching property on entity
            var entityProp = entityProperties.FirstOrDefault(p =>
                p.Name.Equals(dtoProp.Name, System.StringComparison.OrdinalIgnoreCase));

            if (entityProp == null)
            {
                // Property doesn't exist on entity
                var propertyDeclaration = classDeclaration.Members
                    .OfType<PropertyDeclarationSyntax>()
                    .FirstOrDefault(p => p.Identifier.Text == dtoProp.Name);

                if (propertyDeclaration != null)
                {
                    var diagnostic = Diagnostic.Create(
                        GenerateSelectMissingSourceProperty,
                        propertyDeclaration.GetLocation(),
                        dtoProp.Name,
                        classSymbol.Name,
                        entityType.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
            else if (!AreTypesCompatible(dtoProp.Type, entityProp.Type))
            {
                // Types are incompatible
                var propertyDeclaration = classDeclaration.Members
                    .OfType<PropertyDeclarationSyntax>()
                    .FirstOrDefault(p => p.Identifier.Text == dtoProp.Name);

                if (propertyDeclaration != null)
                {
                    var diagnostic = Diagnostic.Create(
                        GenerateSelectMissingSourceProperty,
                        propertyDeclaration.GetLocation(),
                        dtoProp.Name,
                        classSymbol.Name,
                        entityType.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static List<IPropertySymbol> GetAllPublicProperties(INamedTypeSymbol typeSymbol)
    {
        var properties = new List<IPropertySymbol>();
        var currentType = typeSymbol;

        // Walk up the inheritance chain
        while (currentType != null)
        {
            var currentProperties = currentType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public);

            properties.AddRange(currentProperties);

            currentType = currentType.BaseType;
        }

        return properties;
    }

    private static bool HasParameterlessConstructor(INamedTypeSymbol classSymbol)
    {
        // If no constructors are explicitly declared, C# provides an implicit parameterless constructor
        var constructors = classSymbol.Constructors.Where(c => !c.IsStatic).ToArray();

        if (constructors.Length == 0)
            return true; // Implicit parameterless constructor

        // Check if any constructor has zero parameters
        return constructors.Any(c => c.Parameters.Length == 0);
    }

    private static bool AreTypesCompatible(ITypeSymbol dtoType, ITypeSymbol entityType)
    {
        // Allow nullable and non-nullable variants to be compatible
        // For example: string? is compatible with string
        var dtoNonNullable = ExtractNonNullableType(dtoType);
        var entityNonNullable = ExtractNonNullableType(entityType);

        // Check if types are equal or one is nullable version of the other
        if (SymbolEqualityComparer.Default.Equals(dtoNonNullable, entityNonNullable))
            return true;

        // Also check by name for types that might not have symbol equality
        return dtoNonNullable.ToDisplayString() == entityNonNullable.ToDisplayString();
    }

    private static ITypeSymbol ExtractNonNullableType(ITypeSymbol type)
    {
        // Check if this is a Nullable<T> value type (e.g., int?)
        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            namedType.TypeArguments.Length > 0)
        {
            return namedType.TypeArguments[0];
        }

        // Check if this is a nullable reference type (e.g., string?)
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return type.WithNullableAnnotation(NullableAnnotation.None);
        }

        return type;
    }
}