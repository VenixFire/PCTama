using PCTama.Controller.Services;
using Microsoft.Extensions.Configuration;

namespace PCTama.Tests.Controller;

public class McpClientServiceTests
{
    [Fact]
    public async Task McpClientService_Initializes_Successfully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<McpClientService>>();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json", optional: true)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpConfiguration:LocalLlmEndpoint"] = "http://localhost:11434",
                ["McpConfiguration:ModelName"] = "llama2",
                ["McpConfiguration:McpServers:0:Name"] = "text",
                ["McpConfiguration:McpServers:0:Endpoint"] = "http://textmcp",
                ["McpConfiguration:McpServers:0:Type"] = "Input",
                ["McpConfiguration:McpServers:0:Enabled"] = "true"
            })
            .Build();

        mockHttpClientFactory
            .Setup(f => f.CreateClient("textmcp"))
            .Returns(new HttpClient());

        // Act
        var service = new McpClientService(
            mockLogger.Object,
            configuration,
            mockHttpClientFactory.Object);

        // Assert
        Assert.NotNull(service);
        var status = await service.GetStatusAsync();
        Assert.NotNull(status);
        Assert.Contains("LlmEndpoint", status.Keys);
    }

    [Fact]
    public async Task GetStatusAsync_Returns_Configuration_Details()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<McpClientService>>();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpConfiguration:LocalLlmEndpoint"] = "http://localhost:11434",
                ["McpConfiguration:ModelName"] = "llama2"
            })
            .Build();

        var service = new McpClientService(
            mockLogger.Object,
            configuration,
            mockHttpClientFactory.Object);

        // Act
        var status = await service.GetStatusAsync();

        // Assert
        Assert.NotNull(status);
        Assert.Equal("http://localhost:11434", status["LlmEndpoint"]);
        Assert.Equal("llama2", status["ModelName"]);
    }
}
