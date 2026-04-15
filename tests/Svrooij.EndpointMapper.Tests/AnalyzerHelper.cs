using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Svrooij.EndpointMapper.Tests;

internal class AnalyzerHelper
{
    internal static async Task<ImmutableArray<Diagnostic>> RunAnalyzer(string sourceCode, bool addGenerateSelect = false)
    {
        if (addGenerateSelect)
        {
            sourceCode = GenerateSelectAttributeCode + Environment.NewLine + sourceCode;
        }
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // Create basic references
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        };

        // Try to add ASP.NET Core references if available
        try
        {
            references.Add(MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("Microsoft.AspNetCore.Routing.Abstractions").Location));
            references.Add(MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("Microsoft.AspNetCore.Http.Abstractions").Location));
        }
        catch
        {
            // Skip if not available in test environment
        }

        var compilation = CSharpCompilation.Create("TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new EndpointMapperAnalyzer();
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);
        var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);

        var diagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync();
        return diagnostics.Where(d => d.Id.StartsWith("SVEM")).ToImmutableArray();
    }

    private const string GenerateSelectAttributeCode = """
        using System;
        namespace Svrooij.EndpointMapper
        {
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
            public sealed class GenerateSelectAttribute : Attribute
            {
                public Type EntityType { get; }
                public GenerateSelectAttribute(Type entityType)
                {
                    EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
                }
            }
        }
        """;
}
