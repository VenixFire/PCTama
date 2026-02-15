# Source directory CMakeLists.txt

# AppHost project
add_custom_target(PCTama.AppHost ALL
    COMMAND ${DOTNET_EXECUTABLE} build ${CMAKE_CURRENT_SOURCE_DIR}/PCTama.AppHost/PCTama.AppHost.csproj 
            --configuration $<CONFIG> 
            --output ${CMAKE_BINARY_DIR}/bin/$<CONFIG>/PCTama.AppHost
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/PCTama.AppHost
    COMMENT "Building PCTama.AppHost"
)

# ServiceDefaults project
add_custom_target(PCTama.ServiceDefaults ALL
    COMMAND ${DOTNET_EXECUTABLE} build ${CMAKE_CURRENT_SOURCE_DIR}/PCTama.ServiceDefaults/PCTama.ServiceDefaults.csproj 
            --configuration $<CONFIG>
            --output ${CMAKE_BINARY_DIR}/bin/$<CONFIG>/PCTama.ServiceDefaults
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/PCTama.ServiceDefaults
    COMMENT "Building PCTama.ServiceDefaults"
)

# Controller project
add_custom_target(PCTama.Controller ALL
    COMMAND ${DOTNET_EXECUTABLE} build ${CMAKE_CURRENT_SOURCE_DIR}/PCTama.Controller/PCTama.Controller.csproj 
            --configuration $<CONFIG>
            --output ${CMAKE_BINARY_DIR}/bin/$<CONFIG>/PCTama.Controller
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/PCTama.Controller
    COMMENT "Building PCTama.Controller"
    DEPENDS PCTama.ServiceDefaults
)

# TextMCP project
add_custom_target(PCTama.TextMCP ALL
    COMMAND ${DOTNET_EXECUTABLE} build ${CMAKE_CURRENT_SOURCE_DIR}/PCTama.TextMCP/PCTama.TextMCP.csproj 
            --configuration $<CONFIG>
            --output ${CMAKE_BINARY_DIR}/bin/$<CONFIG>/PCTama.TextMCP
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/PCTama.TextMCP
    COMMENT "Building PCTama.TextMCP"
)

# ActorMCP project
add_custom_target(PCTama.ActorMCP ALL
    COMMAND ${DOTNET_EXECUTABLE} build ${CMAKE_CURRENT_SOURCE_DIR}/PCTama.ActorMCP/PCTama.ActorMCP.csproj 
            --configuration $<CONFIG>
            --output ${CMAKE_BINARY_DIR}/bin/$<CONFIG>/PCTama.ActorMCP
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/PCTama.ActorMCP
    COMMENT "Building PCTama.ActorMCP"
)

# Set dependencies
add_dependencies(PCTama.AppHost PCTama.ServiceDefaults PCTama.Controller PCTama.TextMCP PCTama.ActorMCP)
add_dependencies(PCTama.Controller PCTama.ServiceDefaults)
