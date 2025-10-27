
namespace WebApiWithEndpointMapper.Endpoints;
public class MyFirstEndpoint : Svrooij.EndpointMapper.IMapEndpoint
{
  public void MapEndpoint(IEndpointRouteBuilder app)
  {
    app
      .MapGet("/myfirstendpoint", () => "Hello from MyFirstEndpoint!")
      .WithOpenApi();
  }
}