cmake_minimum_required(VERSION 3.20)
project(PCTama VERSION 1.0.0 LANGUAGES CSharp)

# Enable CSharp language support
enable_language(CSharp)

# Set .NET version
set(DOTNET_TARGET_FRAMEWORK "net8.0")
set(CMAKE_CSharp_FLAGS "/langversion:latest")

# Find dotnet CLI
find_program(DOTNET_EXECUTABLE dotnet REQUIRED)

# Add subdirectories
add_subdirectory(src)
add_subdirectory(tests)

# Custom target for building all .NET projects
add_custom_target(build_all
    COMMAND ${CMAKE_COMMAND} -E echo "Building all PCTama projects..."
    DEPENDS PCTama.AppHost PCTama.Controller PCTama.TextMCP PCTama.ActorMCP
)

# Custom target for running tests
add_custom_target(test_all
    COMMAND ${DOTNET_EXECUTABLE} test ${CMAKE_BINARY_DIR}/tests/PCTama.Tests/PCTama.Tests.csproj
    DEPENDS PCTama.Tests
    WORKING_DIRECTORY ${CMAKE_SOURCE_DIR}
)

# Custom target for restore
add_custom_target(restore_all
    COMMAND ${DOTNET_EXECUTABLE} restore ${CMAKE_SOURCE_DIR}/PCTama.sln
    WORKING_DIRECTORY ${CMAKE_SOURCE_DIR}
)
