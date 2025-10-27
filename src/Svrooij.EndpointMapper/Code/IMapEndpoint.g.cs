using Microsoft.AspNetCore.Routing;
namespace Svrooij.EndpointMapper
{
  public interface IMapEndpoint
  {
    void MapEndpoint(IEndpointRouteBuilder app);
  }
}