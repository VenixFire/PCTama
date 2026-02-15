using PCTama.ActorMCP.Services;
using PCTama.ActorMCP.Models;

namespace PCTama.Tests.ActorMCP;

public class ActorServiceTests
{
    [Fact]
    public void ActorService_Initializes_Successfully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ActorService>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ActorMcpConfiguration:DisplayType"] = "WinUI3",
                ["ActorMcpConfiguration:WindowWidth"] = "400",
                ["ActorMcpConfiguration:WindowHeight"] = "300",
                ["ActorMcpConfiguration:WindowTitle"] = "PCTama Actor"
            })
            .Build();

        // Act
        var service = new ActorService(mockLogger.Object, configuration);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void GetQueueCount_Returns_Zero_Initially()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ActorService>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ActorMcpConfiguration:DisplayType"] = "WinUI3"
            })
            .Build();

        var service = new ActorService(mockLogger.Object, configuration);

        // Act
        var count = service.GetQueueCount();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task EnqueueActionAsync_Adds_Action_To_Queue()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ActorService>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ActorMcpConfiguration:DisplayType"] = "WinUI3"
            })
            .Build();

        var service = new ActorService(mockLogger.Object, configuration);
        var action = new ActionRequest
        {
            Action = "say",
            Text = "Hello, world!"
        };

        // Act
        var result = await service.EnqueueActionAsync(action);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(1, service.GetQueueCount());
    }

    [Fact]
    public async Task EnqueueActionAsync_Returns_Success_Result()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ActorService>>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ActorMcpConfiguration:DisplayType"] = "WinUI3"
            })
            .Build();

        var service = new ActorService(mockLogger.Object, configuration);
        var action = new ActionRequest
        {
            Action = "display",
            Text = "Test message"
        };

        // Act
        var result = await service.EnqueueActionAsync(action);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("enqueued successfully", result.Message);
    }
}
