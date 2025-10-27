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

Create classes that implement the `IMapEndpoint` interface:

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
