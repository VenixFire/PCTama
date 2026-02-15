using PCTama.TextMCP.Services;
using PCTama.TextMCP.Models;

namespace PCTama.Tests.TextMCP;

public class TextStreamServiceTests
{
    [Fact]
    public void TextStreamService_Initializes_Successfully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TextStreamService>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TextMcpConfiguration:Source"] = "OBSLocalVoice",
                ["TextMcpConfiguration:OBSLocalVoiceEndpoint"] = "ws://localhost:4455",
                ["TextMcpConfiguration:StreamingEnabled"] = "true",
                ["TextMcpConfiguration:BufferSize"] = "4096"
            })
            .Build();

        // Act
        var service = new TextStreamService(mockLogger.Object, configuration);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void GetBufferCount_Returns_Zero_Initially()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TextStreamService>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TextMcpConfiguration:Source"] = "OBSLocalVoice"
            })
            .Build();

        var service = new TextStreamService(mockLogger.Object, configuration);

        // Act
        var count = service.GetBufferCount();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetLatestTextAsync_Returns_Null_When_Buffer_Empty()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TextStreamService>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TextMcpConfiguration:Source"] = "OBSLocalVoice"
            })
            .Build();

        var service = new TextStreamService(mockLogger.Object, configuration);

        // Act
        var result = await service.GetLatestTextAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllTextAsync_Returns_Empty_List_Initially()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TextStreamService>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TextMcpConfiguration:Source"] = "OBSLocalVoice"
            })
            .Build();

        var service = new TextStreamService(mockLogger.Object, configuration);

        // Act
        var result = await service.GetAllTextAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
