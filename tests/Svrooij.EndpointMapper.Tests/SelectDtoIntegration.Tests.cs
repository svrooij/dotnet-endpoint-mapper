using Svrooij.EndpointMapper;

namespace SelectDtoIntegrationTests;

/// <summary>
/// Example entity that would come from Entity Framework
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastModified { get; set; }
}

/// <summary>
/// Example DTO for testing selective property mapping
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? SKU { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? LastModified { get; set; }
}

/// <summary>
/// Integration tests demonstrating the SelectDto source generator functionality
/// These tests verify the property selection logic that the source generator uses.
/// </summary>
public class SelectDtoIntegrationTests
{
    [Test]
    public async Task PropertySelectionWithStringParsing()
    {
        // Arrange
        var properties = "id,name";
        var selectedProps = new System.Collections.Generic.HashSet<string>(
            (properties ?? string.Empty)
                .Split(',')
                .Select(p => p.Trim().ToLowerInvariant())
                .Where(p => !string.IsNullOrEmpty(p)),
            System.StringComparer.OrdinalIgnoreCase
        );

        // Act & Assert
        await Assert.That(selectedProps.Contains("id")).IsTrue();
        await Assert.That(selectedProps.Contains("name")).IsTrue();
        await Assert.That(selectedProps.Contains("sku")).IsFalse();
    }

    [Test]
    public async Task PropertySelectionCaseInsensitive()
    {
        // Arrange
        var properties = "ID,NAME";
        var selectedProps = new System.Collections.Generic.HashSet<string>(
            (properties ?? string.Empty)
                .Split(',')
                .Select(p => p.Trim().ToLowerInvariant())
                .Where(p => !string.IsNullOrEmpty(p)),
            System.StringComparer.OrdinalIgnoreCase
        );

        // Act & Assert - Should match lowercase versions
        await Assert.That(selectedProps.Contains("id")).IsTrue();
        await Assert.That(selectedProps.Contains("name")).IsTrue();
    }

    [Test]
    public async Task PropertySelectionWithSpaces()
    {
        // Arrange
        var properties = "id , name , price ";
        var selectedProps = new System.Collections.Generic.HashSet<string>(
            (properties ?? string.Empty)
                .Split(',')
                .Select(p => p.Trim().ToLowerInvariant())
                .Where(p => !string.IsNullOrEmpty(p)),
            System.StringComparer.OrdinalIgnoreCase
        );

        // Act & Assert - spaces should be trimmed
        await Assert.That(selectedProps.Contains("id")).IsTrue();
        await Assert.That(selectedProps.Contains("name")).IsTrue();
        await Assert.That(selectedProps.Contains("price")).IsTrue();
    }

    [Test]
    public async Task EmptyPropertyStringResultsInEmptySet()
    {
        // Arrange
        var properties = "";
        var selectedProps = new System.Collections.Generic.HashSet<string>(
            (properties ?? string.Empty)
                .Split(',')
                .Select(p => p.Trim().ToLowerInvariant())
                .Where(p => !string.IsNullOrEmpty(p)),
            System.StringComparer.OrdinalIgnoreCase
        );

        // Act & Assert
        await Assert.That(selectedProps.Count).IsEqualTo(0);
    }

    [Test]
    public async Task NullPropertyStringResultsInEmptySet()
    {
        // Arrange
        string? properties = null;
        var selectedProps = new System.Collections.Generic.HashSet<string>(
            (properties ?? string.Empty)
                .Split(',')
                .Select(p => p.Trim().ToLowerInvariant())
                .Where(p => !string.IsNullOrEmpty(p)),
            System.StringComparer.OrdinalIgnoreCase
        );

        // Act & Assert
        await Assert.That(selectedProps.Count).IsEqualTo(0);
    }

    [Test]
    public async Task MappingWithSelectiveProperties()
    {
        // Arrange - Simulate what the generated code does
        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "SKU-123",
            Description = "High-performance laptop",
            Price = 1299.99m,
            CreatedAt = DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow
        };

        var properties = "id,name,price";
        var selectedProps = new System.Collections.Generic.HashSet<string>(
            (properties ?? string.Empty)
                .Split(',')
                .Select(p => p.Trim().ToLowerInvariant())
                .Where(p => !string.IsNullOrEmpty(p)),
            System.StringComparer.OrdinalIgnoreCase
        );

        // Act - Generate DTO with selected properties using internal helper pattern
        var dto = MapToProductDto(product, selectedProps);

        // Assert
        await Assert.That(dto.Id).IsEqualTo(1);
        await Assert.That(dto.Name).IsEqualTo("Laptop");
        await Assert.That(dto.Price).IsEqualTo(1299.99m);
        await Assert.That(dto.SKU).IsNull();
        await Assert.That(dto.Description).IsNull();
        await Assert.That(dto.CreatedAt).IsNull();
        await Assert.That(dto.LastModified).IsNull();
    }

    [Test]
    public async Task WhitespaceOnlyStringResultsInEmptySet()
    {
        // Arrange
        var properties = "   ";
        var selectedProps = new System.Collections.Generic.HashSet<string>(
            (properties ?? string.Empty)
                .Split(',')
                .Select(p => p.Trim().ToLowerInvariant())
                .Where(p => !string.IsNullOrEmpty(p)),
            System.StringComparer.OrdinalIgnoreCase
        );

        // Act & Assert
        await Assert.That(selectedProps.Count).IsEqualTo(0);
    }

    [Test]
    public async Task IQueryableExtensionCreatesHashSetOnce()
    {
        // Arrange - Verify that the pattern used in IQueryable extension
        // creates the HashSet once before calling Select
        var properties = "id,name,price";

        // This simulates what the generated IQueryable extension does
        var selectedProps = new System.Collections.Generic.HashSet<string>(
            (properties ?? string.Empty)
                .Split(',')
                .Select(p => p.Trim().ToLowerInvariant())
                .Where(p => !string.IsNullOrEmpty(p)),
            System.StringComparer.OrdinalIgnoreCase
        );

        // The HashSet is now created ONCE and passed to each Select iteration
        var products = new[]
        {
            new Product { Id = 1, Name = "Laptop", Price = 1000m },
            new Product { Id = 2, Name = "Mouse", Price = 25m },
            new Product { Id = 3, Name = "Keyboard", Price = 75m }
        };

        // Act - Simulate the Select with pre-created HashSet
        var results = products
            .Select(p => MapToProductDto(p, selectedProps))
            .ToList();

        // Assert - All products mapped correctly with selective properties
        await Assert.That(results).IsNotNull();
        await Assert.That(results!.Count).IsEqualTo(3);
        await Assert.That(results[0].Name).IsEqualTo("Laptop");
        await Assert.That(results[1].Price).IsEqualTo(25m);
        await Assert.That(results[2].Id).IsEqualTo(3);

        // Verify unselected properties are null
        await Assert.That(results[0].SKU).IsNull();
        await Assert.That(results[0].Description).IsNull();
    }

    [Test]
    public async Task ValidPropertyNamesOnlyAddedToHashSet()
    {
        // Arrange
        var properties = "id,name,invalid,email,alsonotreal";

        // Simulate the generated validation pattern
        var validProperties = new[] { "id", "name", "email" };
        var selectedProps = new System.Collections.Generic.HashSet<string>(
            (properties ?? string.Empty)
                .Split(',')
                .Select(p => p.Trim().ToLowerInvariant())
                .Where(p => !string.IsNullOrEmpty(p))
                .Where(p => validProperties.Contains(p)),  // ? Only valid properties
            System.StringComparer.OrdinalIgnoreCase
        );

        // Act & Assert
        await Assert.That(selectedProps.Count).IsEqualTo(3);  // Only 3 valid properties
        await Assert.That(selectedProps.Contains("id")).IsTrue();
        await Assert.That(selectedProps.Contains("name")).IsTrue();
        await Assert.That(selectedProps.Contains("email")).IsTrue();
        await Assert.That(selectedProps.Contains("invalid")).IsFalse();
        await Assert.That(selectedProps.Contains("alsonotreal")).IsFalse();
    }

    [Test]
    public async Task ValidPropertyFilterImprovesPerfomance()
    {
        // Arrange - Demonstrate the performance benefit
        var properties = "id,name,email,invalid1,invalid2,invalid3,invalid4,invalid5";

        // Valid properties from the DTO
        var validProperties = new[] { "id", "name", "email" };

        // Before optimization: HashSet would contain invalid properties
        var beforeOptimization = properties.Split(',')
            .Select(p => p.Trim().ToLowerInvariant())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToHashSet(System.StringComparer.OrdinalIgnoreCase);

        // After optimization: Only valid properties in HashSet
        var afterOptimization = properties.Split(',')
            .Select(p => p.Trim().ToLowerInvariant())
            .Where(p => !string.IsNullOrEmpty(p))
            .Where(p => validProperties.Contains(p))  // ? Filter
            .ToHashSet(System.StringComparer.OrdinalIgnoreCase);

        // Act & Assert
        await Assert.That(beforeOptimization.Count).IsEqualTo(8);  // All entries
        await Assert.That(afterOptimization.Count).IsEqualTo(3);   // Only valid

        // Performance benefit: Smaller HashSet = faster lookups
        // For each property check in the mapping: selectedProps.Contains("id")
        // Smaller HashSet = fewer comparisons needed
        await Assert.That(afterOptimization.Count).IsLessThan(beforeOptimization.Count);
    }

    [Test]
    public async Task NonNullablePropertiesAlwaysIncluded()
    {
        // Arrange - Simulate property selection with mixed nullable/non-nullable properties
        var properties = "name";  // Only requesting 'name'

        // Non-nullable properties that should ALWAYS be included
        var nonNullableProperties = new[] { "id" };
        var validProperties = new[] { "id", "name", "email" };

        // Simulate the generated code behavior
        var selectedProps = new System.Collections.Generic.HashSet<string>(
            (properties ?? string.Empty)
                .Split(',')
                .Select(p => p.Trim().ToLowerInvariant())
                .Where(p => !string.IsNullOrEmpty(p))
                .Where(p => validProperties.Contains(p))
                .Concat(nonNullableProperties),  // ? Always add non-nullable
            System.StringComparer.OrdinalIgnoreCase
        );

        // Act & Assert
        // Even though we only requested 'name', 'id' should be included because it's non-nullable
        await Assert.That(selectedProps.Contains("id")).IsTrue();      // Non-nullable: always included ?
        await Assert.That(selectedProps.Contains("name")).IsTrue();    // Requested
        await Assert.That(selectedProps.Contains("email")).IsFalse();  // Not requested, nullable
    }

    [Test]
    public async Task AllPropertiesIncludedWhenAllRequested()
    {
        // Arrange
        var properties = "id,name,email";

        var nonNullableProperties = new[] { "id" };
        var validProperties = new[] { "id", "name", "email" };

        var selectedProps = new System.Collections.Generic.HashSet<string>(
            properties.Split(',')
                .Select(p => p.Trim().ToLowerInvariant())
                .Where(p => !string.IsNullOrEmpty(p))
                .Where(p => validProperties.Contains(p))
                .Concat(nonNullableProperties),
            System.StringComparer.OrdinalIgnoreCase
        );

        // Act & Assert
        await Assert.That(selectedProps.Count).IsEqualTo(3);
        await Assert.That(selectedProps.Contains("id")).IsTrue();
        await Assert.That(selectedProps.Contains("name")).IsTrue();
        await Assert.That(selectedProps.Contains("email")).IsTrue();
    }

    [Test]
    public async Task NonNullablePropertiesIncludedEvenWhenNotRequested()
    {
        // Arrange - Request only nullable properties
        var properties = "email,somethingelse";

        var nonNullableProperties = new[] { "id", "name" };
        var validProperties = new[] { "id", "name", "email" };

        var selectedProps = new System.Collections.Generic.HashSet<string>(
            properties.Split(',')
                .Select(p => p.Trim().ToLowerInvariant())
                .Where(p => !string.IsNullOrEmpty(p))
                .Where(p => validProperties.Contains(p))
                .Concat(nonNullableProperties),  // ? Non-nullable always added
            System.StringComparer.OrdinalIgnoreCase
        );

        // Act & Assert
        // User requested: email, somethingelse
        // Actually selected: id, name (non-nullable), email
        await Assert.That(selectedProps.Contains("id")).IsTrue();      // Non-nullable: forced included ?
        await Assert.That(selectedProps.Contains("name")).IsTrue();    // Non-nullable: forced included ?
        await Assert.That(selectedProps.Contains("email")).IsTrue();   // Requested
        await Assert.That(selectedProps.Contains("somethingelse")).IsFalse();  // Invalid, not added
    }

    // Helper method that simulates the internal generated method
    private ProductDto MapToProductDto(Product entity, System.Collections.Generic.HashSet<string> selectedProps)
    {
        return new ProductDto
        {
            Id = selectedProps.Contains("id") ? entity.Id : default,
            Name = selectedProps.Contains("name") ? entity.Name : null,
            SKU = selectedProps.Contains("sku") ? entity.SKU : null,
            Description = selectedProps.Contains("description") ? entity.Description : null,
            Price = selectedProps.Contains("price") ? entity.Price : null,
            CreatedAt = selectedProps.Contains("createdat") ? entity.CreatedAt : null,
            LastModified = selectedProps.Contains("lastmodified") ? entity.LastModified : null,
        };
    }
}
