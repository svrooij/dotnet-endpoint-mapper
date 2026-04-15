using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Svrooij.EndpointMapper.Tests;

public class SelectDtoGeneratorTests
{
    [Test]
    public async Task Generator_should_generate_Select_extensions_when_GenerateSelectAttribute_is_applied()
    {
        // Arrange - Create source code with a DTO using GenerateSelect attribute
        var sourceCode = """
            using Svrooij.EndpointMapper;

            namespace TestProject;
            
            public class MyEntity
            {
                public int Id { get; set; }
                public string Name { get; set; } = string.Empty;
                public string Email { get; set; } = string.Empty;
            }

            [GenerateSelect(typeof(MyEntity))]
            public class MyEntityDto
            {
                public int Id { get; set; }
                public string? Name { get; set; }
                public string? Email { get; set; }
            }
            """;

        // Act - Run the generator
        var result = await RunGenerator(sourceCode, "TestProject");

        // Assert - Verify SelectDtoExtensions are generated
        var generatedSources = result.Results[1].GeneratedSources;
        var selectDtoSource = generatedSources.FirstOrDefault(s => s.HintName.Contains("SelectDtoExtensions"));
        await Assert.That(selectDtoSource.SourceText).IsNotNull();

        var generatedText = selectDtoSource.SourceText.ToString();

        // Verify the extension class is generated
        await Assert.That(generatedText).Contains("public static class MyEntityDtoExtensions");

        // Verify IQueryable extension method is generated
        await Assert.That(generatedText).Contains("public static IQueryable<MyEntityDto> SelectMyEntityDto(");
        await Assert.That(generatedText).Contains("this IQueryable<TestProject.MyEntity> query");

        // Verify single entity extension methods are generated
        await Assert.That(generatedText).Contains("public static MyEntityDto SelectMyEntityDto(this TestProject.MyEntity entity, string properties)");
        // This is for hashset compares which only happens if an object has more then 64 props
        //await Assert.That(generatedText).Contains("internal static MyEntityDto SelectMyEntityDto(this TestProject.MyEntity entity, System.Collections.Generic.HashSet<string> selectedProps)");
        await Assert.That(generatedText).Contains("internal static MyEntityDto SelectMyEntityDto(this TestProject.MyEntity entity, long selectedProps)");

        // Verify property selection logic
        //await Assert.That(generatedText).Contains("new System.Collections.Generic.HashSet<string>(");
        //await Assert.That(generatedText).Contains("selectedProps.Contains");

        // Verify properties are included
        await Assert.That(generatedText).Contains("Id");
        await Assert.That(generatedText).Contains("Name");
        await Assert.That(generatedText).Contains("Email");

        await Assert.That(generatedText).Contains("(selectedProps & NameFlag) != 0");
    }

    [Test]
    public async Task Generator_should_not_generate_Select_extensions_when_GenerateSelectAttribute_is_not_applied()
    {
        // Arrange - Create source code without GenerateSelect attribute
        var sourceCode = """
            namespace TestProject;
            
            public class RegularClass
            {
                public void SomeMethod() { }
            }
            """;

        // Act - Run the generator
        var result = await RunGenerator(sourceCode, "TestProject");

        // Assert - Verify SelectDtoExtensions are NOT generated
        var generatedSources = result.Results[1].GeneratedSources;
        var selectDtoSource = generatedSources.FirstOrDefault(s => s.HintName.Contains("SelectDtoExtensions"));

        // Should be empty or not exist when no [GenerateSelect] attributes found
        await Assert.That(selectDtoSource.SourceText == null || selectDtoSource.SourceText.ToString().Trim().Length == 0).IsTrue();
    }

    [Test]
    public async Task Generator_should_generate_GenerateSelectAttribute()
    {
        // Arrange
        var sourceCode = """
            namespace TestProject;
            
            public class RegularClass { }
            """;

        // Act - Run the generator
        var result = await RunGenerator(sourceCode, "TestProject");

        // Assert - Verify GenerateSelectAttribute is always generated
        var generatedSources = result.Results[0].GeneratedSources;

        var generateSelectAttribute = generatedSources.FirstOrDefault(s => s.HintName.Contains("GenerateSelectAttribute"));
        await Assert.That(generateSelectAttribute.SourceText).IsNotNull();
        await Assert.That(generateSelectAttribute.SourceText.ToString()).Contains("[AttributeUsage(AttributeTargets.Class");
        await Assert.That(generateSelectAttribute.SourceText.ToString()).Contains("public sealed class GenerateSelectAttribute");
    }

    [Test]
    public async Task Generator_should_handle_multiple_GenerateSelect_attributes()
    {
        // Arrange - Create source code with multiple DTO attributes
        var sourceCode = """
            using Svrooij.EndpointMapper;

            namespace TestProject;
            
            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; } = string.Empty;
            }

            public class Product
            {
                public int Id { get; set; }
                public string SKU { get; set; } = string.Empty;
                public decimal Price { get; set; }
            }

            [GenerateSelect(typeof(User))]
            public class UserDto
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }

            [GenerateSelect(typeof(Product))]
            public class ProductDto
            {
                public int Id { get; set; }
                public string? SKU { get; set; }
                public decimal? Price { get; set; }
            }
            """;

        // Act - Run the generator
        var result = await RunGenerator(sourceCode, "TestProject");

        // Assert - Verify both extension classes are generated
        var generatedSources = result.Results[1].GeneratedSources;
        var selectDtoSource = generatedSources.FirstOrDefault(s => s.HintName.Contains("SelectDtoExtensions"));
        await Assert.That(selectDtoSource.SourceText).IsNotNull();

        var generatedText = selectDtoSource.SourceText.ToString();

        // Verify both extension classes are generated
        await Assert.That(generatedText).Contains("public static class UserDtoExtensions");
        await Assert.That(generatedText).Contains("public static class ProductDtoExtensions");

        // Verify both dto extension methods
        await Assert.That(generatedText).Contains("public static IQueryable<UserDto> SelectUserDto(");
        await Assert.That(generatedText).Contains("public static UserDto SelectUserDto(");
        await Assert.That(generatedText).Contains("public static IQueryable<ProductDto> SelectProductDto(");
        await Assert.That(generatedText).Contains("public static ProductDto SelectProductDto(");
    }

    [Test]
    public async Task Generator_should_filter_invalid_property_names_in_hashset()
    {
        // Arrange - Create source code with a DTO
        var sourceCode = """
            using Svrooij.EndpointMapper;

            namespace TestProject;
            
            public class MyEntity
            {
                public int Id { get; set; }
                public string Name { get; set; } = string.Empty;
                public string Email { get; set; } = string.Empty;
            }

            [GenerateSelect(typeof(MyEntity))]
            public class MyEntityDto
            {
                public int Id { get; set; }
                public string? Name { get; set; }
                public string? Email { get; set; }
            }
            """;

        // Act - Run the generator
        var result = await RunGenerator(sourceCode, "TestProject");

        // Assert - Verify the generated code validates property names
        var generatedSources = result.Results[1].GeneratedSources;
        var selectDtoSource = generatedSources.FirstOrDefault(s => s.HintName.Contains("SelectDtoExtensions"));
        await Assert.That(selectDtoSource.SourceText).IsNotNull();

        var generatedText = selectDtoSource.SourceText.ToString();

        // Verify that valid property names are embedded in the generated code
        await Assert.That(generatedText).Contains("Id = entity.Id");
        await Assert.That(generatedText).Contains("\"name\"");
        await Assert.That(generatedText).Contains("\"email\"");

        // Verify the filter clause is present
        await Assert.That(generatedText).Contains(".Where(p => ValidProperties.Contains(p))");
    }

    [Test]
    public async Task Generator_should_use_SelectExpression_when_attribute_is_applied()
    {
        // Arrange - DTO with a [SelectExpression] on a property that has no counterpart on the entity
        var sourceCode = """
            using Svrooij.EndpointMapper;

            namespace TestProject;

            public class Order
            {
                public int Id { get; set; }
                public string CustomerName { get; set; } = string.Empty;
                public decimal SubTotal { get; set; }
                public decimal Tax { get; set; }
            }

            [GenerateSelect(typeof(Order))]
            public class OrderDto
            {
                public int Id { get; set; }

                // Always-computed non-nullable: no matching property on Order
                [SelectExpression("e.SubTotal + e.Tax")]
                public decimal TotalPrice { get; set; }

                // Selectable nullable computed property
                [SelectExpression("e.CustomerName.ToUpper()")]
                public string? CustomerDisplay { get; set; }
            }
            """;

        // Act
        var result = await RunGenerator(sourceCode, "TestProject");

        // Assert
        var generatedSources = result.Results[1].GeneratedSources;
        var selectDtoSource = generatedSources.FirstOrDefault(s => s.HintName.Contains("SelectDtoExtensions"));
        await Assert.That(selectDtoSource.SourceText).IsNotNull();

        var generatedText = selectDtoSource.SourceText.ToString();

        // Non-nullable computed property uses the expression directly (always included)
        await Assert.That(generatedText).Contains("TotalPrice = entity.SubTotal + entity.Tax,");

        // Nullable computed property uses the expression behind the bitmask guard
        await Assert.That(generatedText).Contains("CustomerDisplay = (selectedProps & CustomerDisplayFlag) != 0 ? entity.CustomerName.ToUpper() : null,");

        // The computed nullable property must be in ValidProperties so it can be requested by name
        await Assert.That(generatedText).Contains("\"customerdisplay\"");
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
        var resourceGenerator = new EmbeddedResourceGenerator();
        var selectDtoGenerator = new SelectDtoGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(resourceGenerator, selectDtoGenerator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        return Task.FromResult(driver.GetRunResult());
    }
}
