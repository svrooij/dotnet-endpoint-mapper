using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Svrooij.EndpointMapper.Tests;

public class EndpointMapperAnalyzerTests
{
    [Test]
    public async Task Analyzer_should_be_constructed_without_parameters()
    {
        var analyzer = new EndpointMapperAnalyzer();
        await Assert.That(analyzer).IsNotNull();
    }

    [Test]
    public async Task Analyzer_should_report_diagnostic_for_missing_parameterless_constructor()
    {
        // Arrange - Create source code with an endpoint class missing a parameterless constructor
        var sourceCode = """
            using Microsoft.AspNetCore.Routing;
            
            namespace Svrooij.EndpointMapper
            {
                internal interface IMapEndpoint
                {
                    void MapEndpoint(IEndpointRouteBuilder app);
                }
            }
            
            namespace TestProject
            {
                public class InvalidEndpoint : Svrooij.EndpointMapper.IMapEndpoint
                {
                    public InvalidEndpoint(string name) { }
                    
                    public void MapEndpoint(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
                    {
                        app.MapGet("/invalid", () => "This is invalid");
                    }
                }
            }
            """;

        // Act - Run the analyzer
        var diagnostics = await RunAnalyzer(sourceCode);

        // Assert - Verify that the diagnostic is reported
        await Assert.That(diagnostics.Length).IsEqualTo(1);
        var diagnostic = diagnostics[0];
        await Assert.That(diagnostic.Id).IsEqualTo("SVEM001");
        await Assert.That(diagnostic.GetMessage()).Contains("InvalidEndpoint");
    }

    [Test]
    public async Task Analyzer_should_not_report_diagnostic_for_valid_parameterless_constructor()
    {
        // Arrange - Create source code with valid endpoint classes
        var sourceCode = """
            using Microsoft.AspNetCore.Routing;
            
            namespace Svrooij.EndpointMapper
            {
                internal interface IMapEndpoint
                {
                    void MapEndpoint(IEndpointRouteBuilder app);
                }
            }
            
            namespace TestProject
            {
                public class ValidEndpoint1 : Svrooij.EndpointMapper.IMapEndpoint
                {
                    // Implicit parameterless constructor
                    public void MapEndpoint(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
                    {
                        app.MapGet("/valid1", () => "Valid");
                    }
                }
                
                public class ValidEndpoint2 : Svrooij.EndpointMapper.IMapEndpoint
                {
                    public ValidEndpoint2() { } // Explicit parameterless constructor
                    
                    public void MapEndpoint(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
                    {
                        app.MapGet("/valid2", () => "Valid");
                    }
                }
                
                public class ValidEndpoint3 : Svrooij.EndpointMapper.IMapEndpoint
                {
                    public ValidEndpoint3() { } // Parameterless constructor
                    public ValidEndpoint3(string name) { } // Additional parameterized constructor
                    
                    public void MapEndpoint(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
                    {
                        app.MapGet("/valid3", () => "Valid");
                    }
                }
            }
            """;

        // Act - Run the analyzer
        var diagnostics = await RunAnalyzer(sourceCode);

        // Assert - Verify that no diagnostics are reported
        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    private async Task<ImmutableArray<Diagnostic>> RunAnalyzer(string sourceCode)
    {
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
}