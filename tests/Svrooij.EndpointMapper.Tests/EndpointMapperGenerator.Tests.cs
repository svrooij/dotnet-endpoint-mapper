using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Svrooij.EndpointMapper.Tests;

public class EndpointMapperGeneratorTests
{
    [Test]
    public async Task Generator_should_be_constructed_without_parameters()
    {
        var generator = new EndpointMapperGenerator();
        await Assert.That(generator).IsNotNull();
    }

    [Test]
    public async Task Generator_should_add_IMapEndpoint_to_generated_code()
    {
        // Arrange - Create source code without any endpoint implementations
        var sourceCode = """
            namespace TestProject;
            
            public class RegularClass
            {
                public void SomeMethod() { }
            }
            """;

        // Act - Run the generator
        var result = await RunGenerator(sourceCode, "TestProject");

        // Assert - Verify IMapEndpoint interface is generated
        var generatedSources = result.Results[0].GeneratedSources;
        await Assert.That(generatedSources.Length).IsGreaterThanOrEqualTo(1); // Should at least generate IMapEndpoint.g.cs

        var interfaceSource = generatedSources.FirstOrDefault(s => s.HintName.Contains("IMapEndpoint"));
        await Assert.That(interfaceSource.SourceText).IsNotNull();

        var generatedText = interfaceSource.SourceText.ToString();
        await Assert.That(generatedText).Contains("interface IMapEndpoint");
        await Assert.That(generatedText).Contains("void MapEndpoint(IEndpointRouteBuilder app)");
        await Assert.That(generatedText).Contains("namespace Svrooij.EndpointMapper");
    }

    [Test]
    public async Task Generator_should_not_generate_MapEndpointsFrom_extension_when_no_endpoints_found()
    {
        // Arrange - Create source code without any endpoint implementations
        var sourceCode = """
            namespace TestProject;
            
            public class RegularClass
            {
                public void SomeMethod() { }
            }
            """;

        // Act - Run the generator
        var result = await RunGenerator(sourceCode, "TestProject");

        // Assert - Verify only IMapEndpoint interface is generated, no extension method
        var generatedSources = result.Results[0].GeneratedSources;
        await Assert.That(generatedSources.Length).IsGreaterThanOrEqualTo(1); // Should still at least generate IMapEndpoint.g.cs
        
        var interfaceSource = generatedSources.FirstOrDefault(s => s.HintName.Contains("IMapEndpoint"));
        await Assert.That(interfaceSource.SourceText).IsNotNull();

        // Verify no EndpointMapperExtensions file is generated
        var extensionSource = generatedSources.FirstOrDefault(s => s.HintName.Contains("EndpointMapperExtensions"));
        await Assert.That(extensionSource.HintName).IsNull();
        await Assert.That(extensionSource.SourceText).IsNull();
    }

    [Test]
    public async Task Generator_should_generate_MapEndpointsFrom_extension_with_multiple_endpoints()
    {
        // Arrange - Create source code with multiple endpoint implementations
        var sourceCode = """
            using Microsoft.AspNetCore.Routing;
            
            namespace TestProject.Endpoints {
            
                public class WeatherEndpoint : Svrooij.EndpointMapper.IMapEndpoint
                {
                    public void MapEndpoint(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/weather", () => "Sunny");
                    }
                }
                
                public class UserEndpoint : Svrooij.EndpointMapper.IMapEndpoint
                {
                    public void MapEndpoint(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/users", () => new[] { "Alice", "Bob" });
                    }
                }
            }
            
            namespace TestProject.Other {
            
                public class ProductEndpoint : Svrooij.EndpointMapper.IMapEndpoint
                {
                    public void MapEndpoint(IEndpointRouteBuilder app)
                    {
                        app.MapGet("/products", () => new[] { "Laptop", "Phone" });
                    }
                }
                
                public class RegularClass
                {
                    public void SomeMethod() { }
                }
            }
            """;

        // Act - Run the generator
        var result = await RunGenerator(sourceCode, "TestProject");

        // Assert - Verify both IMapEndpoint interface and extension method are generated
        var generatedSources = result.Results[0].GeneratedSources;
        await Assert.That(generatedSources.Length).IsEqualTo(2); // IMapEndpoint.g.cs + EndpointMapperExtensions.g.cs
        
        // Verify interface is generated
        var interfaceSource = generatedSources.FirstOrDefault(s => s.HintName.Contains("IMapEndpoint"));
        await Assert.That(interfaceSource.SourceText).IsNotNull();
        
        // Verify extension method is generated
        var extensionSource = generatedSources.FirstOrDefault(s => s.HintName.Contains("EndpointMapperExtensions"));
        await Assert.That(extensionSource.SourceText).IsNotNull();
        
        var extensionText = extensionSource.SourceText.ToString();
        await Assert.That(extensionText).Contains("MapEndpointsFromTestProject");
        await Assert.That(extensionText).Contains("public static WebApplication MapEndpointsFromTestProject(this WebApplication app)");
        
        // Verify all three endpoint classes are registered
        await Assert.That(extensionText).Contains("new TestProject.Endpoints.WeatherEndpoint().MapEndpoint(app);");
        await Assert.That(extensionText).Contains("new TestProject.Endpoints.UserEndpoint().MapEndpoint(app);");
        await Assert.That(extensionText).Contains("new TestProject.Other.ProductEndpoint().MapEndpoint(app);");
        
        await Assert.That(extensionText).Contains("return app;");
    }

    [Test]
    public async Task Generator_should_generate_MapEndpointsFrom_extension_with_single_endpoint()
    {
        // Arrange - Create source code with a single endpoint implementation
        var sourceCode = """
            using Microsoft.AspNetCore.Routing;
            
            namespace TestProject.Endpoints;
            
            public class WeatherEndpoint : Svrooij.EndpointMapper.IMapEndpoint
            {
                public void MapEndpoint(IEndpointRouteBuilder app)
                {
                    app.MapGet("/weather", () => "Sunny");
                }
            }
            
            public class RegularClass
            {
                public void SomeMethod() { }
            }
            """;

        // Act - Run the generator
        var result = await RunGenerator(sourceCode, "TestProject");

        // Assert - Verify both IMapEndpoint interface and extension method are generated
        var generatedSources = result.Results[0].GeneratedSources;
        await Assert.That(generatedSources.Length).IsEqualTo(2); // IMapEndpoint.g.cs + EndpointMapperExtensions.g.cs
        
        // Verify interface is generated
        var interfaceSource = generatedSources.FirstOrDefault(s => s.HintName.Contains("IMapEndpoint"));
        await Assert.That(interfaceSource.SourceText).IsNotNull();
        
        // Verify extension method is generated
        var extensionSource = generatedSources.FirstOrDefault(s => s.HintName.Contains("EndpointMapperExtensions"));
        await Assert.That(extensionSource.SourceText).IsNotNull();
        
        var extensionText = extensionSource.SourceText.ToString();
        await Assert.That(extensionText).Contains("MapEndpointsFromTestProject");
        await Assert.That(extensionText).Contains("public static WebApplication MapEndpointsFromTestProject(this WebApplication app)");
        await Assert.That(extensionText).Contains("new TestProject.Endpoints.WeatherEndpoint().MapEndpoint(app);");
        await Assert.That(extensionText).Contains("return app;");
    }

    private Task<GeneratorDriverRunResult> RunGenerator(string sourceCode, string assemblyName)
    {
        // Parse source code
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // Create references
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        };

        // Create compilation
        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Run generator
        var generator = new EndpointMapperGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        return Task.FromResult(driver.GetRunResult());
    }
}