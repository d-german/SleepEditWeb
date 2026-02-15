# SleepEditWeb — Code Style & Conventions

## C# Conventions
- **Nullable references**: Enabled (`<Nullable>enable</Nullable>`) — all reference types must be explicitly nullable or non-nullable
- **Implicit usings**: Enabled — standard `System.*` and `Microsoft.AspNetCore.*` namespaces auto-imported
- **Naming**: PascalCase for public members, camelCase for private fields/locals, `I` prefix for interfaces
- **Static methods mandate**: Any method that does not access instance state MUST be declared `static`
- **SOLID principles**: Strictly followed — single responsibility per class, interface segregation, dependency inversion via constructor injection
- **Functional style preferred**: Favor immutability, pure functions, LINQ/higher-order functions over imperative loops and mutable state

## Architecture Patterns
- **DI registration** in `Program.cs` — Scoped for per-request services, Singleton for repositories
- **Options pattern** for configuration (`IOptions<T>` bound from `appsettings.json` sections)
- **Session storage** for transient editor state (`ISession` with JSON serialization)
- **Request DTOs** as nested classes inside controllers
- **Anti-forgery** via `[ValidateAntiForgeryToken]` on all POST endpoints

## Frontend Conventions
- **Inline JavaScript** in Razor views (no separate JS modules currently)
- **Bootstrap 5** for layout and components
- **Bootstrap Icons** (`bi bi-*`) for iconography
- **CSS custom properties** for theming (light/dark via `[data-theme="dark"]`)
- **fetch()** for AJAX calls to backend endpoints
- **localStorage** for persisting UI preferences (collapsed sections, tech names, etc.)

## Testing Conventions
- **NUnit 4** test framework with `[Test]`, `[TestFixture]` attributes
- **Moq** for mocking interfaces
- **In-memory test doubles** for session stores (e.g., `InMemoryProtocolEditorSessionStore`)
- **Test naming**: `MethodName_Scenario_ExpectedResult` pattern
- **No integration tests currently** — all tests are unit tests with mocked dependencies

## Task Completion
When completing a task:
1. Build the solution: `dotnet build SleepEditWeb.sln`
2. Run tests: `dotnet test SleepEditWeb.Tests/SleepEditWeb.Tests.csproj`
3. Resolve any nullable warnings
4. Ensure new code follows SOLID and functional patterns
