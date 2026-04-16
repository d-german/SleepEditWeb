# Multi-Protocol Feature Implementation

## Status: Complete (all 11 tasks done)
## Branch: feature/db-only-protocol-persistence
## Build: 0 errors, 0 warnings | Tests: 120/120 passing

## Architecture Summary

The system now supports multiple named protocols instead of a single "current" protocol.

### Key Components
- **SavedProtocolMetadata** — record with ProtocolId, Name, CreatedUtc, LastModifiedUtc, IsDefault
- **IProtocolRepository** — extended with 7 new methods: SaveProtocol, GetProtocol, ListProtocols, DeleteProtocol, RenameProtocol, SetDefaultProtocol, GetDefaultProtocol
- **LiteDbProtocolRepository** — implements all methods, uses `saved_protocols` collection, `EnsureMigration()` for legacy→multi migration
- **IProtocolManagementService** — orchestrator: CreateProtocol, ListProtocols, LoadProtocol, DeleteProtocol, RenameProtocol, SetDefaultProtocol, GetActiveProtocolId. Auto-saves on protocol switch.
- **ProtocolEditorSessionStore** — tracks ActiveProtocolId in HTTP session
- **ProtocolStarterService** — Create(Guid) and CreateSeedDocument() methods added
- **ProtocolEditorController** — 5 new endpoints (CreateProtocol, ListProtocols, LoadProtocol, DeleteProtocol, RenameProtocol). SaveXml/SetDefaultProtocol updated for active protocol.
- **ProtocolEditorResponseMapper** — includes activeProtocolId in response
- **ProtocolSelector.razor/.razor.cs** — new Blazor component for protocol CRUD
- **ProtocolEditorShell** — integrates ProtocolSelector, shows active protocol name, handles OnProtocolSwitched
- **ProtocolToolbar** — saves to active protocol ID, "Set As Default" button removed (handled by ProtocolSelector)

### LiteDB Collections
- `saved_protocols` — named protocol metadata + default flag
- `protocol_versions` — version history with optional ProtocolId tag
- `current_protocol` — legacy, retained for backward compat

### Migration
- `EnsureMigration()` runs lazily on first ListProtocols/GetDefaultProtocol call
- Copies current_protocol into saved_protocols as default
- Idempotent, uses `_migrationChecked` boolean flag
