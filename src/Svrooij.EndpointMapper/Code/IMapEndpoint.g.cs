using Microsoft.AspNetCore.Routing;
namespace Svrooij.EndpointMapper
{
  /// <summary>
  /// Implement this interface on a class to have its endpoint(s) mapped by the source generator.
  /// </summary>
  internal interface IMapEndpoint
  {
    /// <summary>
    /// Maps the endpoint(s) to the provided IEndpointRouteBuilder.
    /// </summary>
    /// <param name="app"></param>
    /// <remarks>It is advised to only map a single endpoint per class for clarity, but this is just a recommendation.</remarks>
    static abstract void MapEndpoint(IEndpointRouteBuilder app);
  }
}