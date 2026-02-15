@echo off
REM PCTama Build Script for Windows
REM This script provides a simple interface to build and run PCTama

setlocal enabledelayedexpansion

if "%1"=="" goto help
if /I "%1"=="help" goto help
if /I "%1"=="restore" goto restore
if /I "%1"=="build" goto build
if /I "%1"=="release" goto release
if /I "%1"=="test" goto test
if /I "%1"=="clean" goto clean
if /I "%1"=="run" goto run
if /I "%1"=="cmake" goto cmake

goto help

:help
echo PCTama Build Script
echo.
echo Usage: build.bat [command]
echo.
echo Commands:
echo   restore     - Restore NuGet packages
echo   build       - Build the solution (Debug)
echo   release     - Build the solution (Release)
echo   test        - Run tests
echo   clean       - Clean build artifacts
echo   run         - Run the Aspire AppHost
echo   cmake       - Build using CMake
echo   help        - Show this help message
echo.
goto end

:restore
echo Restoring packages...
dotnet restore PCTama.sln
goto end

:build
echo Building solution (Debug)...
dotnet restore PCTama.sln
dotnet build PCTama.sln --configuration Debug
goto end

:release
echo Building solution (Release)...
dotnet restore PCTama.sln
dotnet build PCTama.sln --configuration Release
goto end

:test
echo Running tests...
dotnet test tests\PCTama.Tests\PCTama.Tests.csproj --verbosity normal
goto end

:clean
echo Cleaning build artifacts...
dotnet clean PCTama.sln
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
if exist build rmdir /s /q build
for /d /r %%d in (bin,obj) do @if exist "%%d" rmdir /s /q "%%d"
goto end

:run
echo Running PCTama AppHost...
cd src\PCTama.AppHost
set ASPIRE_ALLOW_UNSECURED_TRANSPORT=true
set ASPNETCORE_URLS=http://localhost:15000
set DOTNET_DASHBOARD_OTLP_ENDPOINT_URL=http://localhost:18889
echo Aspire Dashboard will be at: http://localhost:15000
dotnet run
goto end

:cmake
echo Building with CMake...
cmake -B build
cmake --build build
goto end

:end
endlocal
