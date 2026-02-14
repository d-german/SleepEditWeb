# Protocol Editor Usage Notes

## Entry Points

- Landing page route: `/` (default now maps to `ProtocolEditor/Index`).
- Direct route: `/ProtocolEditor`.
- XML export endpoint: `/ProtocolEditor/ExportXml`.

## Starter Content

- Protocol title: `Saint Luke's Protocol`.
- Includes all 12 legacy top-level sections in legacy order.
- Includes seeded nested diagnostic statements and a sample cross-section link.

## Editor Controls

- `Add Section`: creates a new section under protocol root.
- `Add Child`: creates a nested node under selected node.
- `Remove`: removes selected node and clears inbound link references.
- `Undo` / `Redo`: restore/reapply prior mutations.
- `Reset`: reloads hardcoded starter protocol.
- `Export XML`: opens generated XML output in a new tab.

## Tree Interaction

- Click any node to load its details in the right pane.
- Drag and drop behavior:
  - Section nodes reorder at protocol root.
  - Subsection nodes can move between valid parents.
  - Invalid drop targets are blocked.

## Detail Panels

- Statement text is editable.
- Link fields (`Link Id`, `Link Text`) are editable.
- SubText entries support add/remove operations.

## Validation Status

- Build: `dotnet build SleepEditWeb.sln` passes.
- Tests: `dotnet test SleepEditWeb.sln` passes.
- Protocol test coverage includes:
  - XML serialization/deserialization hierarchy and field order.
  - Mutation paths and invalid move handling.
  - Undo/redo determinism.
