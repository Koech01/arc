using System.Net;
using Arc.Api.Controllers;
using System.Net.Http.Json;
namespace Arc.IntegrationTests.Api;
using Microsoft.AspNetCore.Mvc.Testing;


public sealed class HealthEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_ReturnsServiceHealthArray()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var services = await response.Content.ReadFromJsonAsync<ServiceHealthDto[]>();
        Assert.NotNull(services);
        Assert.NotEmpty(services);

        var apiGateway = services.FirstOrDefault(s => s.Name == "API Gateway");
        Assert.NotNull(apiGateway);
        Assert.NotEmpty(apiGateway.Status);
        Assert.True(apiGateway.Uptime >= 0);

        var database = services.FirstOrDefault(s => s.Name == "Database");
        Assert.NotNull(database);
        Assert.NotEmpty(database.Status);

        var taskQueue = services.FirstOrDefault(s => s.Name == "Task Queue");
        Assert.NotNull(taskQueue);
        Assert.NotEmpty(taskQueue.Status);
    }
}