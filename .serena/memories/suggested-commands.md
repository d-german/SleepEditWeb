# SleepEditWeb — Suggested Commands

## Build & Run
```powershell
# Build solution
dotnet build SleepEditWeb.sln

# Run web app (development)
dotnet run --project SleepEditWeb/SleepEditWeb.csproj

# Run with watch (hot-reload)
dotnet watch --project SleepEditWeb/SleepEditWeb.csproj
```

## Testing
```powershell
# Run all tests
dotnet test SleepEditWeb.Tests/SleepEditWeb.Tests.csproj

# Run with verbose output
dotnet test SleepEditWeb.Tests/SleepEditWeb.Tests.csproj --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~ProtocolEditorServiceTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Docker
```powershell
# Build Docker image
docker build -t sleepeditweb .

# Run container
docker run -p 8080:8080 sleepeditweb
```

## System Utilities (Windows / PowerShell)
```powershell
# File/directory operations
Get-ChildItem, Test-Path, New-Item, Copy-Item, Move-Item, Remove-Item

# Search
Select-String -Path "**/*.cs" -Pattern "pattern"   # grep equivalent
Get-ChildItem -Recurse -Filter "*.cs"               # find equivalent

# Git
git status; git log --oneline -10; git diff
```

## Task Completion Checklist
When completing a coding task, always:
1. `dotnet build SleepEditWeb.sln` — verify no build errors
2. `dotnet test SleepEditWeb.Tests/SleepEditWeb.Tests.csproj` — verify all tests pass
3. Check for nullable warnings and resolve them
