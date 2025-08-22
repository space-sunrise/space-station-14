# Space Station 14 - SUNRISE

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Essential Setup (REQUIRED - Run in Order)
- Install .NET 9.0 SDK: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 9.0.100` -- takes 60-90 seconds. NEVER CANCEL.
- Add .NET to PATH: `export PATH="/home/runner/.dotnet:$PATH"`
- Run initial setup: `python3 RUN_THIS.py` -- takes 1-2 seconds. Sets up git hooks.
- Initialize submodules: `git submodule update --init --recursive` -- takes 5-10 seconds. NEVER CANCEL.

### Core Build Process
- **CRITICAL**: Some components require Microsoft.DotNet.RemoteExecutor package that may fail due to network issues with Azure DevOps feeds. This is NORMAL and documented.
- Build individual core projects (RECOMMENDED approach):
  - `dotnet restore Content.Shared/Content.Shared.csproj` -- takes 2 seconds
  - `dotnet restore Content.Server/Content.Server.csproj` -- takes 2 seconds  
  - `dotnet restore Content.Client/Content.Client.csproj` -- takes 2 seconds
  - `dotnet build Content.Shared/Content.Shared.csproj --configuration DebugOpt --no-restore` -- takes 60-90 seconds. NEVER CANCEL. Set timeout to 120+ seconds.
  - `dotnet build Content.Server/Content.Server.csproj --configuration DebugOpt --no-restore` -- takes 30-45 seconds. NEVER CANCEL. Set timeout to 90+ seconds.
  - `dotnet build Content.Client/Content.Client.csproj --configuration DebugOpt --no-restore` -- takes 30-45 seconds. NEVER CANCEL. Set timeout to 90+ seconds.

### Running Applications
- **ALWAYS build core components first before running**.
- Run server: `dotnet run --project Content.Server --configuration DebugOpt --no-build`
- Run client: `dotnet run --project Content.Client --configuration DebugOpt --no-build`
- Alternative scripts: `./runserver.sh` or `./runclient.sh` (builds automatically)
- Tools versions: `./runserver-Tools.sh` or `./runclient-Tools.sh`

### Testing
- **LIMITATION**: Full test suite requires Microsoft.DotNet.RemoteExecutor package that may not be available due to network connectivity to Azure DevOps feeds.
- Basic tests: `dotnet test Content.Shared/Content.Shared.csproj --no-build --configuration DebugOpt` -- works but limited coverage
- Integration tests in Content.IntegrationTests may fail due to dependency issues
- **Workaround**: Test individual components and run manual server startup validation

### Validation Steps
- **ALWAYS run these validation steps after making changes**:
- Build core components as described above
- Start server briefly: `timeout 30s dotnet run --project Content.Server --configuration DebugOpt --no-build` to verify startup
- Check for build warnings and errors
- **Manual Testing**: Run server and verify it loads maps and initializes systems correctly

## Common Issues and Workarounds

### Network Dependency Issues
- **SYMPTOM**: `Failed to download package 'Microsoft.DotNet.RemoteExecutor.8.0.0-beta.24059.4' from 'https://pkgs.dev.azure.com/dnceng'`
- **CAUSE**: Network connectivity issues with Azure DevOps package feeds
- **WORKAROUND**: Build individual core projects instead of full solution
- **IMPACT**: Some tools (YAMLLinter, full test suite) may not work, but core game components work fine

### Build Configuration Issues  
- **Use DebugOpt configuration** for most development work
- **Use Tools configuration** for running development tools
- **Never use default Debug** configuration as it may have performance issues

## Project Structure

### Core Components
- **Content.Shared**: Shared game logic and data structures between client and server
- **Content.Server**: Server-side game logic, systems, and networking
- **Content.Client**: Client-side rendering, UI, input handling
- **Content.Tests**: Unit tests for shared components
- **Content.IntegrationTests**: Integration tests (may fail due to dependency issues)

### Additional Components  
- **Content.Tools**: Development and content creation tools
- **Content.YAMLLinter**: YAML validation tool (may fail due to dependency issues)
- **RobustToolbox**: Game engine (git submodule)
- **Resources**: Game content (prototypes, maps, textures, audio, etc.)

### Important Directories
- `/Resources`: All game content and assets
- `/Maps`: Game maps in YAML format
- `/Prototypes`: Game object definitions in YAML
- `/.github/workflows`: CI/CD configuration
- `/Tools`: Development scripts and utilities

### Key Files
- `SpaceStation14.sln`: Main solution file
- `global.json`: .NET SDK version specification (requires 9.0.100)
- `nuget.config`: Package source configuration (includes Azure DevOps feeds)
- `RUN_THIS.py`: Initial repository setup script
- `README.md`, `README.en.md`: Documentation
- Various run scripts: `runserver.sh`, `runclient.sh`, etc.

## Development Workflow

### Making Changes
- **ALWAYS** build and test core components after changes
- **ALWAYS** run server startup validation
- Focus on Content.Shared, Content.Server, or Content.Client depending on the change type
- Use DebugOpt configuration for development
- Expect build warnings (hundreds are normal) - focus on errors

### Changelog Documentation in Pull Requests
- **REQUIRED**: Include changelog entries in PR descriptions for player-visible changes
- **FORMAT**: Use the following template in PR descriptions:
- **AUTHOR**: Set author Copilot (AI)
```
:cl: Author
- add: Добавлено веселье.
- remove: Удалено веселье.
- tweak: Изменено веселье.
- fix: Исправлено веселье.
```
- **CHANGE TYPES**:
  - `add`: New features, content, or functionality
  - `remove`: Removed features, content, or functionality  
  - `tweak`: Modified existing features or balance changes
  - `fix`: Bug fixes or corrections
- **AUTOMATION**: Changelog entries are automatically processed and added to game changelogs after PR merge
- **LANGUAGE**: Write changelog messages in Russian for consistency with existing entries
- **GUIDELINES**: Keep messages brief, clear, and player-focused (what they will notice in-game)

### File Editing
- **YAML files**: Found in `/Resources/Prototypes` and `/Resources/Maps`
- **C# code**: Organized by component (Shared/Server/Client)
- **Assets**: Textures, audio, etc. in `/Resources`

### Common Commands Reference
```bash
# Setup (run once)
python3 RUN_THIS.py
git submodule update --init --recursive

# Build core (run after changes)
dotnet build Content.Shared/Content.Shared.csproj --configuration DebugOpt --no-restore
dotnet build Content.Server/Content.Server.csproj --configuration DebugOpt --no-restore  
dotnet build Content.Client/Content.Client.csproj --configuration DebugOpt --no-restore

# Run (for testing)
dotnet run --project Content.Server --configuration DebugOpt --no-build
dotnet run --project Content.Client --configuration DebugOpt --no-build

# Quick validation
timeout 30s dotnet run --project Content.Server --configuration DebugOpt --no-build
```

### Build Timing Expectations
- **NEVER CANCEL** builds - they can take 30-90 seconds per component
- Content.Shared: 60-90 seconds (largest component)
- Content.Server: 30-45 seconds  
- Content.Client: 30-45 seconds
- Set timeouts to 120+ seconds for builds to avoid premature cancellation
- **Total build time**: 2-4 minutes for all core components

## Critical Warnings
- **NEVER CANCEL BUILDS OR LONG-RUNNING COMMANDS** - Builds may take up to 90 seconds per component
- **ALWAYS SET TIMEOUTS TO 120+ SECONDS** for build commands
- **NETWORK DEPENDENCY ISSUES ARE NORMAL** - Use individual project builds as workaround
- **MANUAL VALIDATION IS REQUIRED** - Always test server startup after changes
- **BUILD WARNINGS ARE EXPECTED** - Focus on errors, not the hundreds of warnings

Fixes #2870.
