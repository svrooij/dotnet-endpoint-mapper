
using System.ComponentModel;
using WebApiWithEndpointMapper.Dto;
using WebApiWithEndpointMapper.Models;

namespace WebApiWithEndpointMapper.Endpoints;

public class SelectEndpoint : Svrooij.EndpointMapper.IMapEndpoint
{
    private static readonly IQueryable<User> Users = new List<User>
    {
        new User { Id = 1, Name = "Alice", Email = "alice@fakedomain.gone", UserLevel = 2 },
        new User { Id = 2, Name = "Bob", Email = "bob@fakedomain.gone", UserLevel = 3 },
        new User { Id = 3, Name = "Charlie", Email = "charlie@fakedomain.gone"}
    }.AsQueryable();
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/users", ([Description("Query only select properties")] string? select) =>
        {
            var users = Users.SelectUserDto(select ?? "");
            return Results.Ok(users);
        })
            .Produces<IEnumerable<UserDto>>(StatusCodes.Status200OK)
            .WithOpenApi()
            .WithTags("Users");
    }
}

