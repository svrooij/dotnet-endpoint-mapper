using Microsoft.AspNetCore.Mvc.Testing;

namespace WebApiWithEndpointMapper.Tests;

// This factory can be used to create a test server for the Sample Web API project that uses EndpointMapper.
// The WebApplicationFactory just needs a reference to any public class in the target project to set up the host.
public class SampleWebApiWithEndpointMapperFactory : WebApplicationFactory<WebApiWithEndpointMapper.Endpoints.MyFirstEndpoint>
{
  // You can override configuration methods here if needed to customize the test server.
  // but for basic tests, the default implementation is sufficient.
}