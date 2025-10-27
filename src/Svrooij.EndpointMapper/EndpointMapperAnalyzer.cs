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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MissingParameterlessConstructor);

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

        if (!implementsIMapEndpoint)
            return;

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

    private static bool HasParameterlessConstructor(INamedTypeSymbol classSymbol)
    {
        // If no constructors are explicitly declared, C# provides an implicit parameterless constructor
        var constructors = classSymbol.Constructors.Where(c => !c.IsStatic).ToArray();
        
        if (constructors.Length == 0)
            return true; // Implicit parameterless constructor

        // Check if any constructor has zero parameters
        return constructors.Any(c => c.Parameters.Length == 0);
    }
}