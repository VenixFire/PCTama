# Contributing to PCTama

Thank you for your interest in contributing to PCTama! This document provides guidelines and instructions for contributing.

## 🚀 Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/yourusername/PCTama.git`
3. Create a feature branch: `git checkout -b feature/my-new-feature`
4. Make your changes
5. Run tests: `dotnet test`
6. Commit your changes: `git commit -am 'Add some feature'`
7. Push to the branch: `git push origin feature/my-new-feature`
8. Submit a pull request

## 🏗️ Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- CMake 3.20 or later
- Visual Studio 2022 or VS Code with C# extension
- Git

### Building

```bash
# Using the build script (recommended)
./build.sh build      # Linux/macOS
build.bat build       # Windows

# Or using dotnet directly
dotnet restore
dotnet build
```

### Running Tests

```bash
# All tests
dotnet test

# Specific test project
dotnet test tests/PCTama.Tests/PCTama.Tests.csproj

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📝 Code Style

- Follow the .editorconfig settings in the repository
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and concise
- Use async/await for asynchronous operations

### C# Conventions

```csharp
// Good
public async Task<ActionResult> ProcessActionAsync(ActionRequest request)
{
    // Implementation
}

// Bad
public ActionResult process(ActionRequest r)
{
    // Implementation
}
```

## 🧪 Testing Guidelines

- Write tests for all new features
- Maintain or improve code coverage
- Use descriptive test names that indicate what is being tested
- Follow the Arrange-Act-Assert pattern

```csharp
[Fact]
public async Task EnqueueActionAsync_Adds_Action_To_Queue()
{
    // Arrange
    var service = CreateTestService();
    var action = new ActionRequest { Action = "test" };

    // Act
    var result = await service.EnqueueActionAsync(action);

    // Assert
    Assert.True(result.Success);
    Assert.Equal(1, service.GetQueueCount());
}
```

## 📦 Adding New Features

### Adding a New MCP Service

1. Create a new project in `src/` directory
2. Implement the MCP service following the pattern of TextMCP or ActorMCP
3. Add project reference to AppHost
4. Update CMakeLists.txt
5. Add tests
6. Update documentation

### Adding a New Input Source

1. Add configuration model in `Models/` directory
2. Implement service in `Services/` directory
3. Add controller endpoints
4. Update appsettings.json with configuration options
5. Add tests
6. Update README.md

## 🐛 Bug Reports

When filing a bug report, please include:

- Version of PCTama
- Operating system and version
- .NET version (`dotnet --version`)
- Steps to reproduce
- Expected behavior
- Actual behavior
- Any error messages or stack traces

## 💡 Feature Requests

Feature requests are welcome! Please:

- Check if the feature has already been requested
- Provide a clear description of the feature
- Explain the use case
- Consider how it fits with the project's goals

## 📋 Pull Request Process

1. Ensure your code follows the project's style guidelines
2. Update README.md with details of changes (if applicable)
3. Add tests for new functionality
4. Ensure all tests pass
5. Update the documentation
6. Reference any related issues in the PR description

### PR Checklist

- [ ] Code builds without errors
- [ ] All tests pass
- [ ] New tests added for new functionality
- [ ] Documentation updated
- [ ] Follows code style guidelines
- [ ] No merge conflicts

## 🔍 Code Review

All submissions require review. We follow these principles:

- Be respectful and constructive
- Focus on the code, not the person
- Explain your reasoning
- Be open to discussion

## 📄 License

By contributing to PCTama, you agree that your contributions will be licensed under the same license as the project.

## 🙏 Questions?

Feel free to open an issue with your question or reach out to the maintainers.

Thank you for contributing to PCTama! 🎉
