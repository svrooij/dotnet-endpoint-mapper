# 🚀 Svrooij.EndpointMapper

**Stop writing boilerplate. Start building APIs.** ⚡

A powerful .NET source generator that eliminates repetitive code when building ASP.NET Core minimal APIs. Write less, ship more, and enjoy not having to write boilerplate code.

> [!NOTE]
> Currently this project is an **experimental prototype**. Features might be added or changed in future releases.

## Why You'll Love It

✨ **Zero Runtime Overhead** – All magic happens at compile time, not runtime  
🎯 **Type-Safe** – Full IntelliSense support with generated code  
📦 **Less Boilerplate** – Auto-discover and register endpoints effortlessly  
🔍 **Smart DTO Mapping** – Only select what you need from your database  
✅ **FluentValidation Integration** – Validation filters generated automatically  
🛠️ **Easy to Use** – Minimal setup, maximum productivity

## Get Started in Seconds

Install the NuGet package:

```bash
dotnet add package Svrooij.EndpointMapper
```

Done! No configuration needed. The source generator does the rest. ✨

<details>
<summary>Or add it manually to your project file</summary>

```xml
<ItemGroup>
  <PackageReference Include="Svrooij.EndpointMapper" Version="1.0.0" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```
</details>

## Features

Being a source generator, this library eliminates boilerplate by generating code at compile time. No reflection, no runtime discovery—just blazing-fast, type-safe endpoint management.

- [Endpoint Mapping](#-easy-endpoint-mapping)
- [Selective DTO Mapping](#-selective-dto-mapping)
- [FluentValidation Integration](#-fluentvalidation-endpoint-filter)

### 🎯 Easy Endpoint Mapping

Never write repetitive endpoint registration again. Simply implement `IMapEndpoint` and watch it get automatically discovered and registered.

```csharp
public class WeatherEndpoint : IMapEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/weather", () => "Sunny")
           .WithName("GetWeather")
           .WithOpenApi();
           
        app.MapPost("/weather", (WeatherRequest request) => 
            Results.Ok($"Weather updated to {request.Description}"))
           .WithName("UpdateWeather")
           .WithOpenApi();
    }
}

// In Program.cs - that's it!
app.MapEndpointFromMyApi(); // ✨ Auto-generated extension method
```

**What you get:**
- 🔍 Automatic discovery of all `IMapEndpoint` implementations
- 🏎️ Compile-time code generation (zero runtime cost)
- 📁 Organized, maintainable code structure
- 📦 Project-specific extension methods

### 💡 Selective DTO Mapping

Stop fetching entire database records just to return a few fields. Generate optimized property selectors at compile time—no reflection, no LINQ expressions. [Original idea](https://github.com/riok/mapperly/issues/1098).

```csharp
// Your domain model
public class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? Address { get; set; }
}

// Your DTO with automatic mapping generation
[Svrooij.EndpointMapper.GenerateSelect(typeof(User))]
public class UserDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}
```

**The magic:**
- ✨ Generates two extension methods automatically
- 📊 `SelectUserDto(IQueryable<User>, properties)` – for Entity Framework queries
- 🔄 `SelectUserDto(User, properties)` – for object mapping
- 🚀 Pure, optimized code generation (no reflection at runtime)
- 🏍️ Select `null` for not selected columns, before querying the database.

**Real-world example:**

```csharp
// Database query
var userDtos = await dbContext.Users
    .SelectUserDto("Id,Name")  // 🎯 Only fetch these columns!
    .ToListAsync();
```

Perfect for APIs where clients can request specific fields!

> [!TIP]
> This feature shines when you have large entities with many properties but your API consumers only need a few. Save bandwidth, reduce N+1 queries, and let the compiler do the heavy lifting.

### ✅ FluentValidation Endpoint Filter

Validation code tends to be verbose and repetitive. Not anymore. Automatically generate endpoint filters for all your `AbstractValidator<T>` implementations.

```csharp
public class FluentValidationEndpoint : Svrooij.EndpointMapper.IMapEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/users", PostNewUserAsync)
           .WithOpenApi()
           .WithTags("Users")
           .AddValidationWithUserDtoValidator(); // ✨ Auto-generated!
    }

    private static async Task<IResult> PostNewUserAsync(UserDto userDto) 
        => Results.Ok($"User {userDto.Name} created!");
}

// Your validator
public class UserDtoValidator : AbstractValidator<UserDto>
{
    public UserDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2).WithMessage("At least 2 characters");
    }
}
```

**What's generated:**

- 🛡️ `UserDtoValidatorFilter : IEndpointFilter`
- 🔧 `AddValidationWithUserDtoValidator()` extension method

> [!INFO]
> See the [complete example](https://github.com/svrooij/dotnet-endpoint-mapper/blob/main/sample/WebApiWithEndpointMapper/Endpoints/FluentValidationEndpoint.cs) for more details.

#### FluentValidation schema generation

In addition to generating filters, the source generator also produces OpenAPI schema definitions for your validators. This means that your API documentation will automatically include validation rules, making it easier for clients to understand how to interact with your endpoints.
The following code (using the `GenerateSchema` attribute) will automatically generate an OpenAPI schema for the `UserDto` based on the rules defined in the `UserDtoValidator`. You have to add the `SchemaTransformer` to your OpenAPI configuration.

```csharp
[Svrooij.EndpointMapper.GenerateSchema(typeof(UserDtoValidator))]
public class UserDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}

internal class UserDtoValidator : AbstractValidator<UserDto>
{
    public UserDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters long.")
            .MaximumLength(100);
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Length(5,200).WithMessage("Email must be between 5 and 200 characters long.")
            .EmailAddress().WithMessage("A valid email is required.");
    }
}
```

Add the `SchemaTransformer` to your OpenAPI configuration:

```csharp
builder.Services.AddOpenApi(api =>
{
    api.AddSchemaTransformer<WebApiWithEndpointMapper.Dto.FluentValidationSchemaTransformer>();
});
```

Code generated:

```csharp
public class FluentValidationSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        string? key = null;
        if (context.JsonTypeInfo.Type == typeof(WebApiWithEndpointMapper.Dto.UserDto))
        {
            key = null;

            key = schema.Properties.Keys.FirstOrDefault(k => k.Equals("Name", StringComparison.OrdinalIgnoreCase));
            if (key != null)
            {
              schema.Properties[key].MinLength = 2;
              schema.Properties[key].MaxLength = 100;
              schema.Properties[key].Nullable = false;
              schema.Required.Add(key);
            }

            key = schema.Properties.Keys.FirstOrDefault(k => k.Equals("Email", StringComparison.OrdinalIgnoreCase));
            if (key != null)
            {
              schema.Properties[key].Format = "email";
              schema.Properties[key].MinLength = 5;
              schema.Properties[key].MaxLength = 200;
              schema.Properties[key].Nullable = false;
              schema.Required.Add(key);
            }
        }
        return Task.CompletedTask;
    }
}
```

## Examples & Samples

Want to see it in action? Check out the [complete sample project](https://github.com/svrooij/dotnet-endpoint-mapper/tree/main/sample/WebApiWithEndpointMapper) with:

- 📝 Multiple endpoint examples
- 🔍 Selective DTO mapping usage
- ✅ FluentValidation integration
- 🧪 Full test suite

## Roadmap & Contributing

This is an active open-source project! Have ideas? Found a bug? **Contributions are welcome!**

Feel free to:
- 🐛 [Report issues](https://github.com/svrooij/dotnet-endpoint-mapper/issues)
- 💡 [Suggest features](https://github.com/svrooij/dotnet-endpoint-mapper/discussions)
- 🔧 [Submit pull requests](https://github.com/svrooij/dotnet-endpoint-mapper/pulls)

## License

MIT License – see the [LICENSE file](LICENSE.txt) for details.

## Source Generators inspiration

- [Andrew Lock Source Generators series](https://andrewlock.net/series/creating-a-source-generator/)
- [Microsoft Docs on Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [PowerShell Source generator](https://github.com/svrooij/PowerShell.DependencyInjection/tree/main/src/Svrooij.PowerShell.DI)

---

**Happy building! 🎉**
