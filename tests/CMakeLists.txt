# Tests directory CMakeLists.txt

# Test project
add_custom_target(PCTama.Tests ALL
    COMMAND ${DOTNET_EXECUTABLE} build ${CMAKE_CURRENT_SOURCE_DIR}/PCTama.Tests/PCTama.Tests.csproj 
            --configuration $<CONFIG>
            --output ${CMAKE_BINARY_DIR}/bin/$<CONFIG>/PCTama.Tests
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/PCTama.Tests
    COMMENT "Building PCTama.Tests"
    DEPENDS PCTama.Controller PCTama.TextMCP PCTama.ActorMCP
)
