# Installing .NET SDK

## You need .NET 8.0 SDK to build PCTama

## Windows

Open PowerShell and run:

```bash
winget install Microsoft.DotNet.SDK.8

# Verify installation
dotnet --version
```

If prompted, restart your terminal after installation.

### Method 1: Homebrew (Recommended)

Open a new terminal window and run:

```bash
# Install .NET 8
brew install dotnet@8

# Add to PATH
echo 'export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc

# Verify installation
dotnet --version
```

### Method 2: Direct Download

1. Visit https://dotnet.microsoft.com/download/dotnet/8.0
2. Download "macOS Arm64 Installer" (for M1/M2/M3 Mac) or "macOS x64 Installer"
3. Run the downloaded `.pkg` file
4. Follow the installation wizard
5. Open a new terminal and verify: `dotnet --version`

### Method 3: Microsoft Install Script

```bash
# Download and run Microsoft's installer
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0

# Add to PATH
echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.zshrc
source ~/.zshrc

# Verify installation
dotnet --version
```

## After Installation

Once .NET is installed, return to the PCTama directory and run:

```bash
cd /Users/venix/Programming/DesktopPet
./build.sh build
```

## Troubleshooting

**If `dotnet` command is not found after installing:**

1. Close your current terminal
2. Open a new terminal window
3. Try again: `dotnet --version`

**If using Homebrew and it's taking too long:**

You can press Ctrl+C to cancel, then open a new terminal and run:
```bash
brew install dotnet@8
```

The installation may continue in the background even after pressing Ctrl+C.

**Check if already installed:**
```bash
ls -la /opt/homebrew/opt/dotnet@8/bin/dotnet
# or
ls -la ~/.dotnet/dotnet
```
