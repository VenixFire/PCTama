using Microsoft.AspNetCore.Mvc.Testing;
using PCTama.Controller;
using System.Net;
using System.Net.Http.Json;

namespace PCTama.Tests.Controller;

public class ControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_Endpoint_Returns_Healthy_Status()
    {
        // Act
        var response = await _client.GetAsync("/api/controller/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", content);
    }

    [Fact]
    public async Task Status_Endpoint_Returns_MCP_Information()
    {
        // Act
        var response = await _client.GetAsync("/api/controller/status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(status);
    }

    [Fact]
    public async Task Root_Health_Endpoint_Is_Available()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Alive_Endpoint_Is_Available()
    {
        // Act
        var response = await _client.GetAsync("/alive");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
