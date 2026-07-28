---
trigger: always_on
---

# Rule: Testing and verification

Run verification at the end of code changes. Choose the narrowest command that proves the changed area is healthy.

## 1. YAML, FTL, and prototypes

If the change touches prototypes, YAML, or FTL files, run the YAML linter:

```powershell
dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj --no-build
```

If the linter project has not been built yet, build it first:

```powershell
dotnet build Content.YAMLLinter/Content.YAMLLinter.csproj --configuration Release --no-restore /m
```

## 2. C# changes

If the change touches C# code, build the changed project:

```powershell
dotnet build <relative/path/to/project.csproj> --configuration Debug
```

For stricter CI-style verification, use:

```powershell
dotnet build <relative/path/to/project.csproj> --configuration Release --no-restore /m
```

## 3. Client changes

If the change touches client behavior, run the client to check runtime errors and IL verification:

```powershell
dotnet run --project Content.Client/Content.Client.csproj
```

or:

```powershell
dotnet run --project Content.Client/Content.Client.csproj --configuration Tools
```

Stop the client process before finishing the task.

## 4. Test commands

Use these commands by scope:

1. All solution tests:

   ```powershell
   dotnet test SpaceStation14.slnx --configuration DebugOpt --no-build
   ```

2. Specific test project:

   ```powershell
   dotnet test Content.Tests/Content.Tests.csproj --configuration DebugOpt --no-build
   ```

   ```powershell
   dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --configuration DebugOpt --no-build
   ```

3. Specific test:

   ```powershell
   dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --configuration DebugOpt --no-build --filter "FullyQualifiedName~GravityGridTest"
   ```

4. Publish artifacts when needed:

   ```powershell
   dotnet publish Content.Packaging/Content.Packaging.csproj --configuration Release -r win-x64
   ```

   ```powershell
   dotnet publish Content.Packaging/Content.Packaging.csproj --configuration Release -p:PublishProfile=<ProfileName>
   ```

## 5. Local resource limits

Never run more than two test commands at the same time. Prefer one test command at a time to avoid slowing down the user's machine.

Always stop any long-running process started during verification before completing the task.
