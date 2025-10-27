namespace WebApiWithEndpointMapper.Tests;

public class EndpointIntegrationTests
{
  private readonly SampleWebApiWithEndpointMapperFactory _factory = new();

  [Test]
  public async Task MyFirstEndpoint_ReturnsExpectedResponse()
  {
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var myFirstEndpointResponse = await client.GetAsync("/myfirstendpoint");

    // Assert
    await Assert.That(myFirstEndpointResponse.IsSuccessStatusCode).IsTrue();
    var content = await myFirstEndpointResponse.Content.ReadAsStringAsync();
    await Assert.That(content).IsEqualTo("Hello from MyFirstEndpoint!");
  }
}