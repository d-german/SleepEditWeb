# DB-Only Protocol Persistence - Session State

**Date:** April 16, 2026  
**Branch:** `feature/db-only-protocol-persistence`  
**Status:** Branch committed & pushed to origin. Uncommitted changes from code review pending.

---

## What This Branch Does

Migrated protocol persistence from XML file-based storage to **database-only (LiteDB)** storage. Protocols are now saved/retrieved exclusively from the LiteDB database. XML files are only used for **import/export** (not as the primary storage).

### Committed Changes (7 commits ahead of main)
1. `1a8e3ad` - Extend `IProtocolRepository` with `GetCurrentProtocol` and `SaveCurrentProtocol`
2. `49b92fb` - Update `ProtocolStarterService` for DB-first loading; remove file scanning
3. `33fff3a` - Update `ProtocolEditorSessionStore` to use `GetCurrentProtocol` instead of `GetLatestVersion`
4. `3bd91ad` - Update `ProtocolEditorController` save operations for DB persistence; remove file-based save/default/import
5. `236d637` - Update Blazor `ProtocolToolbar` for DB persistence; remove file store dependencies
6. `bf60a1d` - Remove dead file persistence code: `PathPolicy`, `FileStore`, `StartupOptions`, path configs
7. `0ecfdb5` - Update deployment docs for DB-only protocol persistence

---

## Uncommitted Changes (Code Review Fixes)

Three files with uncommitted changes from a code review. These need to be committed:

### 1. `LiteDbProtocolRepository.cs` — Transactional Safety & Refactoring
- **`SaveCurrentProtocol`** now wraps the current-protocol upsert AND the version insert in a **LiteDB transaction** (`BeginTrans`/`Commit`/`Rollback`), making the two writes atomic
- Extracted two private helpers to eliminate code duplication:
  - `CreateVersionEntity(xml, source, note, savedUtc)` — static factory for `ProtocolVersionEntity`
  - `InsertVersionEntity(entity, document)` — handles collection insert + index
- `SaveVersion` method's inline entity creation replaced with calls to these helpers

### 2. `ProtocolEditorController.cs` — Error Handling
- Added try-catch blocks around `_repository.SaveCurrentProtocol()` in three endpoints:
  - **`SaveXml`** — catches `IOException`, `UnauthorizedAccessException`, `LiteException` → returns 500 `"Failed to save protocol."`
  - **`SetDefaultProtocol`** — same pattern → returns 500 `"Failed to set default protocol."`
  - **`ImportXmlUpload`** — catches those plus `ObjectDisposedException` → returns 500 `"Failed to import uploaded XML."`
- Added `using LiteDB;` import

### 3. `ProtocolEditorControllerTests.cs` — 4 New Failure-Path Tests
- `SaveXml_WhenRepositoryPersistenceFails_ReturnsInternalServerError`
- `SetDefaultProtocol_WhenRepositoryPersistenceFails_ReturnsInternalServerError`
- `ImportXmlUpload_WhenUploadReadFails_ReturnsInternalServerError`
- `ImportXmlUpload_WhenRepositoryPersistenceFails_ReturnsInternalServerError`

---

## Next Steps

1. **Commit the uncommitted changes** with a message like: "Add transactional safety, error handling, and failure-path tests for protocol persistence"
2. **Merge to main** — the branch was being prepared for merge to main to test deployment
3. **Multi-protocol support** — the next planned feature is allowing users to create/manage multiple protocols (not just one), with the ability to load different protocols separately. This was discussed as the next feature after this branch lands.

---

## Multi-Protocol Feature Discussion (Next Feature)

The user asked about feasibility of supporting multiple protocols:
- Currently there's only one protocol (the "current" one) with no way to create a new separate protocol
- The desired behavior: create a new protocol without modifying the existing one, load different protocols to view/edit
- This was deemed feasible and a comprehensive task list was requested for it
- The task list for multi-protocol support should be created fresh (previous tasks were cleared)

---

## Key Architecture Context

- **`IProtocolRepository`** — interface in `Infrastructure/ProtocolPersistence/`
- **`LiteDbProtocolRepository`** — implementation using LiteDB
- **Collections:** `protocol_versions` (version history), `current_protocol` (singleton current doc)
- **`ProtocolEditorController`** — MVC controller handling save/load/import/export
- **`ProtocolEditorSessionStore`** — in-memory session state, loads from DB on init
- **`ProtocolStarterService`** — handles initial protocol loading on app start
- Protocols are `ProtocolDocument` objects serialized to/from XML via `IProtocolXmlService`
