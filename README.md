# Svrooij.EndpointMapper

A .NET source generator that automatically discovers and registers API endpoints in ASP.NET Core applications using a clean, organized approach.

## Overview

This source generator provides a simple way to organize your API endpoints into separate classes while automatically generating the registration code. It scans for classes implementing the `IMapEndpoint` interface and generates an extension method to register all endpoints at once.

## Features

- 🚀 **Automatic Discovery**: Finds all classes implementing `IMapEndpoint` in your project
- 🔧 **Source Generation**: Generates registration code at compile time
- 📁 **Clean Organization**: Keep endpoints in separate files/classes
- ⚡ **Zero Runtime Overhead**: All discovery happens at compile time
- 🎯 **Project-Specific**: Generates unique extension methods per project
- 📃 **Dto mapping**: In web api projects, it is very common to be able to select which fields you want to return.

## Installation

Install the NuGet package in your ASP.NET Core project:

```bash
dotnet add package Svrooij.EndpointMapper
```

Or add it manually to your project file:

```xml
<ItemGroup>
  <PackageReference Include="Svrooij.EndpointMapper" Version="1.0.0" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

## Usage

### 1. Create Endpoint Classes

Create classes that implement the `IMapEndpoint` interface (needs a constructor without parameters):

```csharp
using Svrooij.EndpointMapper;

namespace MyApi.Endpoints;

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

public class UserEndpoint : IMapEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/users", () => new[] { "Alice", "Bob" })
           .WithName("GetUsers")
           .WithOpenApi();
    }
}
```

### 2. Register All Endpoints

In your `Program.cs`, the source generator will create an extension method named `MapEndpointFrom{YourProjectName}()`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// This method is generated automatically
app.MapEndpointFromMyApi(); // Replace "MyApi" with your actual project name

app.Run();
```

## How It Works

1. **Interface Implementation**: You implement `IMapEndpoint` in your endpoint classes
2. **Compile-Time Discovery**: The source generator scans your project for implementing classes
3. **Code Generation**: An extension method is generated with code to instantiate and register all endpoints
4. **Registration**: Call the generated extension method in your `Program.cs`

## Generated Code Example

For a project named "MyApi" with two endpoint classes, the generator creates:

```csharp
using Microsoft.AspNetCore.Builder;

namespace Svrooij.EndpointMapper;

public static class EndpointMapperExtensions
{
    public static WebApplication MapEndpointFromMyApi(this WebApplication app)
    {
        new MyApi.Endpoints.WeatherEndpoint().MapEndpoint(app);
        new MyApi.Endpoints.UserEndpoint().MapEndpoint(app);
        return app;
    }
}
```

## Benefits

### ✅ **Clean Separation of Concerns**
Each endpoint class handles its own routing logic, making code more maintainable and testable.

### ✅ **Automatic Registration**
No need to manually register each endpoint class - the source generator handles it automatically.

### ✅ **Compile-Time Safety**
All endpoint discovery happens at compile time, so you'll know immediately if there are issues.

### ✅ **IntelliSense Support**
The generated extension method appears in IntelliSense with full type safety.

### ✅ **Performance**
Zero runtime overhead - all reflection and discovery happens at compile time.

## DTO Mapping Support

If you add an attribute to your DTO with the source type, the generator will create an extension method to map from the source type to the DTO type.
This method allows you to specify which field should be mapped, non-nullable properties are always mapped.

```csharp
// The source type
public class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
}

// The DTO with the attribute
[Svrooij.EndpointMapper.GenerateSelect(typeof(User))]
public class UserDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
}

// This will generate the following extension method for use with Enttity Framework:
public static IQueryable<UserDto> SelectUserDto(this IQueryable<User> query, string properties)
// And this extension just to map individual objects
public static UserDto SelectUserDto(this User entity, string properties)
```

These extensions are not using linq expressions but in fact pre-generated code that boils down to the following:

```csharp
/// <summary>
/// Maps a single User instance to UserDto using a pre-parsed bitmask.
/// This internal method is used by the IQueryable extension to avoid parsing the properties string multiple times.
/// </summary>
/// <param name="entity">The User instance</param>
/// <param name="selectedProps">Bitmask of selected properties</param>
/// <returns>A UserDto instance with selected properties populated</returns>
internal static UserDto SelectUserDto(this WebApiWithEndpointMapper.Endpoints.User entity, long selectedProps)
{
    return new WebApiWithEndpointMapper.Endpoints.UserDto
    {
        Id = entity.Id,
        Name = (selectedProps & NameFlag) != 0 ? entity.Name : null,
        Email = (selectedProps & EmailFlag) != 0 ? entity.Email : null,
    };
}

/// <summary>
/// Parses the properties string and returns a bitmask of selected properties.
/// </summary>
private static long ParseProperties(string properties)
{
    long flags = 0;
    if (string.IsNullOrEmpty(properties)) return flags;

    var selected = new System.Collections.Generic.HashSet<string>(
        properties
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(p => p.ToLowerInvariant())
        .Where(p => ValidProperties.Contains(p)),
        System.StringComparer.OrdinalIgnoreCase
    );

    if (selected.Contains("name")) flags |= NameFlag;
    if (selected.Contains("email")) flags |= EmailFlag;

    return flags;
}
```

## Project Structure Example

```
MyApi/
├── Program.cs
├── Endpoints/
│   ├── WeatherEndpoint.cs
│   ├── UserEndpoint.cs
│   └── ProductEndpoint.cs
└── Models/
    ├── WeatherRequest.cs
    └── UserModel.cs
```

## Requirements

- .NET 8.0 or later

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
