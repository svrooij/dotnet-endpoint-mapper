using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Svrooij.EndpointMapper.Tests;

public class GenerateSelectAnalyzerTests
{
    [Test]
    public async Task Analyzer_should_not_report_for_properties_from_base_class()
    {
        // Arrange - Create source code with an endpoint class missing a parameterless constructor
        var sourceCode = """
            #nullable enable
            namespace TestAssembly {

                public abstract class BaseEntity
                {
                    public int Id { get; set; }
                }
            
                public class MyEntity : BaseEntity
                {
                    public string Name { get; set; } = string.Empty;
                    public string Email { get; set; } = string.Empty;
                }

                [Svrooij.EndpointMapper.GenerateSelect(typeof(MyEntity))]
                public class MyEntityDto
                {
                    public int Id { get; set; }
                    public string? Name { get; set; }
                    public string? Email { get; set; }
                }
            }
            """; ;

        // Act - Run the analyzer
        var diagnostics = await AnalyzerHelper.RunAnalyzer(sourceCode, addGenerateSelect: true);

        // Assert - Verify that the diagnostic is reported
        await Assert.That(diagnostics).Count().IsEqualTo(0);
        //await Assert.That(diagnostics.Length).IsEqualTo(1);
        //var diagnostic = diagnostics[0];
        //await Assert.That(diagnostic.Id).IsEqualTo("SVEM001");
        //await Assert.That(diagnostic.GetMessage()).Contains("InvalidEndpoint");
    }




}