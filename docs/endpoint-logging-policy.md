# Endpoint Logging Policy

This document defines the minimum logging standard for `SleepEditWeb` controllers and services using `ILogger`.

## Goals

- Make production failures diagnosable without attaching a debugger.
- Keep logs structured, consistent, and low-noise.
- Avoid leaking sensitive data (PII, secrets, raw payloads).

## Core Rules

1. Use structured logging placeholders, not string interpolation.
2. Log endpoint entry and endpoint outcome for every controller action.
3. Log validation failures and branch decisions that affect behavior.
4. Log exceptions with context and exception object.
5. Never log raw request bodies, auth tokens, or full patient free-text notes.

## Required Logging Points

### Controller Action

- Entry log (`Information`): action name + key identifiers/paths.
- Validation failure (`Warning`): what failed and why.
- Business decision (`Information`): resolved path/selected mode/fallback chosen.
- Success (`Information`): action completed with key result location/id.
- Exception (`Warning` or `Error`): include exception and safe context.

### Service Method

- Entry (`Debug` or `Information`) for non-trivial operations.
- Key branch outcomes (`Information`) for fallback/decision points.
- Expected invalid input (`Warning`) when service rejects data.
- Exception paths (`Warning`/`Error`) with operation context.

## Log Levels

- `Trace`: high-volume internal details; avoid in normal production.
- `Debug`: method-level flow details for diagnostics.
- `Information`: normal lifecycle events (entry/success/decisions).
- `Warning`: recoverable failures, validation issues, degraded behavior.
- `Error`: operation failed and user-visible behavior impacted.
- `Critical`: application/service cannot continue.

## Event ID Taxonomy

Use `EventId` where practical for searchable categories:

- `1000-1099`: Protocol Editor endpoints
- `1100-1199`: Protocol Viewer endpoints
- `1200-1299`: Sleep Note Editor endpoints
- `1300-1399`: Admin and Medication endpoints
- `2000-2999`: Protocol services
- `3000-3999`: Sleep Note and medication services

If event IDs are not yet introduced for a file, keep message templates stable so migration is easy.

## Message Template Conventions

- Start with operation verb: `SaveXml requested`, `ImportXml completed`.
- Include stable nouns: `path`, `nodeId`, `count`, `mode`, `fileName`.
- Prefer singular responsibility per message.
- Keep templates reusable and consistent:
  - `"{Operation} requested. Resolved path: {Path}"`
  - `"{Operation} aborted because {Reason}"`
  - `"{Operation} completed successfully at path: {Path}"`

## Sensitive Data Rules

Do not log:

- Patient names, MRNs, DOBs, or patient narrative contents.
- Medication narrative free-text and generated note body.
- Request verification tokens, auth headers, cookies.
- Full uploaded file content.

Allowed:

- File names (if not patient-identifying), file sizes, sanitized server paths.
- Node IDs, section IDs, counts, operation mode enums.

## Complexity and SOLID Constraints

- Keep methods at cyclomatic complexity around `<= 5` where practical.
- Extract helper methods when logging introduces extra branches.
- Keep logging concerns localized; do not mix domain mutations with complex formatting logic.

## Controller Example

```csharp
_logger.LogInformation("SaveXml requested. Resolved save path: {Path}", savePath);

if (!File.Exists(importPath))
{
    _logger.LogWarning("ImportXml aborted because file was not found at path: {Path}", importPath);
    return BadRequest(...);
}

_logger.LogWarning(ex, "SetDefaultProtocol failed for path: {Path}", defaultPath);
```

## Verification Checklist

- Every endpoint has entry + outcome logs.
- Every `BadRequest` and caught exception has a log with safe context.
- No sensitive data appears in templates or placeholders.
- Build and tests pass after instrumentation.
