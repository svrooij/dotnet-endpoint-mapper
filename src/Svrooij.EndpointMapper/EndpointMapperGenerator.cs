using System.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Svrooij.EndpointMapper;

[Generator]
public sealed class EndpointMapperGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // "Generate" code that is included in the Code folder.
        // Every file in there will be a .g.cs file in the project where this source generator is used.
        context.RegisterPostInitializationOutput(static ctx =>
        {
            var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (var resourceName in resources)
            {
                if (resourceName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
                {
                    using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                    if (stream is not null)
                    {
                        using var reader = new System.IO.StreamReader(stream);
                        var sourceText = reader.ReadToEnd();
                        ctx.AddSource(resourceName, sourceText);
                    }
                }
            }
        });

        var endpointMapperDeclarations = context.SyntaxProvider
          .CreateSyntaxProvider(
            predicate: static (s, _) => IsSyntaxTargetForEndpointMapperGeneration(s),
            transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
          .Where(static m => m is not null);

        var compilationAndMappings = context.CompilationProvider.Combine(endpointMapperDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndMappings, static (spc, source) =>
        {
            var (compilation, mappings) = source;
            AddMappedEndpointsSource(compilation, mappings!, spc);
        });


        var selectDtoDeclarations = context.SyntaxProvider
          .CreateSyntaxProvider(
            predicate: static (s, _) => IsSyntaxTargetForGenerateSelectGeneration(s),
            transform: static (ctx, _) => GetSemanticTargetForGenerateSelectGeneration(ctx))
          .Where(static m => m is not null);

        var compilationAndSelectDtos = context.CompilationProvider.Combine(selectDtoDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndSelectDtos, static (spc, source) =>
        {
            var (compilation, selectDtos) = source;
            ExecuteSelectDtoGeneration(compilation, selectDtos!, spc);
        });
    }

    private static bool IsSyntaxTargetForEndpointMapperGeneration(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDeclaration || classDeclaration.BaseList == null)
            return false;

        // Check if any base type has "IMapEndpoint" name at syntax level
        return classDeclaration.BaseList.Types.Any(baseType =>
            baseType.Type.ToString().Contains("IMapEndpoint"));
    }

    private static INamedTypeSymbol? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

        if (classSymbol == null)
            return null;

        // Check if the class implements IMapEndpoint
        var implementsIMapEndpoint = classSymbol.AllInterfaces.Any(i =>
          i.Name == "IMapEndpoint" &&
          i.ContainingNamespace.ToDisplayString() == "Svrooij.EndpointMapper");

        return implementsIMapEndpoint ? classSymbol : null;
    }

    private static bool IsSyntaxTargetForGenerateSelectGeneration(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDeclaration || classDeclaration.AttributeLists.Count == 0)
            return false;

        // Check if any attribute starts with "GenerateSelect" at syntax level
        return classDeclaration.AttributeLists.Any(attrList =>
            attrList.Attributes.Any(attr =>
            {
                var attrName = attr.Name.ToString();
                return attrName == "GenerateSelect" ||
                       attrName == "GenerateSelectAttribute" ||
                       attrName.EndsWith(".GenerateSelect") ||
                       attrName.EndsWith(".GenerateSelectAttribute");
            }));
    }

    private static INamedTypeSymbol? GetSemanticTargetForGenerateSelectGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

        if (classSymbol == null)
            return null;

        // Check if the class has the GenerateSelect attribute
        var hasGenerateSelectAttribute = classSymbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "GenerateSelectAttribute" &&
            attr.AttributeClass?.ContainingNamespace.ToDisplayString() == "Svrooij.EndpointMapper");

        return hasGenerateSelectAttribute ? classSymbol : null;
    }

    private static string NormalizeIdentifier(string input)
    {
        var sb = new StringBuilder();
        foreach (char c in input)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (c == '.' || c == '-' || c == '_')
                sb.Append('_');
        }

        var result = sb.ToString();
        if (result.Length == 0 || !char.IsLetter(result[0]))
            result = "Project" + result;

        return result;
    }

    private static void AddMappedEndpointsSource(Compilation compilation, ImmutableArray<INamedTypeSymbol> endpointClasses, SourceProductionContext context)
    {
        if (endpointClasses.IsEmpty)
            return;

        var assemblyName = compilation.AssemblyName ?? "Unknown";
        var normalizedAssemblyName = NormalizeIdentifier(assemblyName);

        var sourceBuilder = new StringBuilder();
        sourceBuilder.AppendLine("// <auto-generated />");
        sourceBuilder.AppendLine("using Microsoft.AspNetCore.Builder;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("namespace Svrooij.EndpointMapper;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("/// <summary>");
        sourceBuilder.AppendLine($"/// Extension methods to map all endpoints from assembly '{assemblyName}'.");
        sourceBuilder.AppendLine("/// </summary>");
        sourceBuilder.AppendLine($"public static class {normalizedAssemblyName}EndpointMapperExtensions");
        sourceBuilder.AppendLine("{");
        sourceBuilder.AppendLine("  /// <summary>");
        sourceBuilder.AppendLine($"  /// Extension method to map all endpoints from assembly '{assemblyName}'.");
        sourceBuilder.AppendLine("  /// </summary>");
        sourceBuilder.AppendLine("  /// <param name=\"app\">The WebApplication to map the endpoints to.</param>");
        sourceBuilder.AppendLine("  /// <returns>The WebApplication with mapped endpoints for chaining.</returns>");
        sourceBuilder.AppendLine($"  public static WebApplication MapEndpointsFrom{normalizedAssemblyName}(this WebApplication app)");
        sourceBuilder.AppendLine("  {");

        foreach (var endpointClass in endpointClasses)
        {
            var fullTypeName = endpointClass.ToDisplayString();
            sourceBuilder.AppendLine($"    new {fullTypeName}().MapEndpoint(app);");
        }

        sourceBuilder.AppendLine("    return app;");
        sourceBuilder.AppendLine("  }");
        sourceBuilder.AppendLine("}");

        context.AddSource("EndpointMapperExtensions.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }

    private static void ExecuteSelectDtoGeneration(Compilation compilation, ImmutableArray<INamedTypeSymbol> selectDtoClasses, SourceProductionContext context)
    {
        if (selectDtoClasses.IsEmpty)
            return;

        var sourceBuilder = new StringBuilder();
        sourceBuilder.AppendLine("// <auto-generated />");
        sourceBuilder.AppendLine("using System;");
        sourceBuilder.AppendLine("using System.Linq;");
        sourceBuilder.AppendLine();

        // Group extensions by namespace
        var groupedByNamespace = selectDtoClasses
            .GroupBy(c => c.ContainingNamespace.ToDisplayString())
            .ToList();

        foreach (var namespaceGroup in groupedByNamespace)
        {
            var namespaceName = namespaceGroup.Key;
            sourceBuilder.AppendLine($"namespace {namespaceName}");
            sourceBuilder.AppendLine("{");

            foreach (var dtoClass in namespaceGroup)
            {
                // Get the GenerateSelect attribute
                var generateSelectAttribute = dtoClass.GetAttributes()
                    .FirstOrDefault(attr =>
                        attr.AttributeClass?.Name == "GenerateSelectAttribute" &&
                        attr.AttributeClass?.ContainingNamespace.ToDisplayString() == "Svrooij.EndpointMapper");

                if (generateSelectAttribute == null)
                    continue;

                // Extract the entity type from the attribute constructor argument
                if (generateSelectAttribute.ConstructorArguments.Length != 1)
                    continue;

                var entityTypeArg = generateSelectAttribute.ConstructorArguments[0];
                if (entityTypeArg.Value is not INamedTypeSymbol entityType)
                    continue;

                var dtoClassName = dtoClass.Name;
                var dtoFullName = dtoClass.ToDisplayString();
                var entityFullName = entityType.ToDisplayString();

                // Generate the extension method
                SelectDtoTextGenerator.GenerateSelectDtoExtensionMethod(sourceBuilder, dtoClassName, dtoFullName, entityFullName, dtoClass, entityType);
            }
            sourceBuilder.AppendLine("}");
        }

        context.AddSource("SelectDtoExtensions.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }

}