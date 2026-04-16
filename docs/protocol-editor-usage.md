# Protocol Editor Usage Notes

## Entry Points

- Landing page route: `/` (default maps to `SleepNoteEditor/Index`).
- Direct route: `/ProtocolEditor`.
- Admin route: `/Admin/Medications?secretKey=<key>` with `Protocol Editor` as the first tab.
- Protocol export endpoint (transport remains XML): `/ProtocolEditor/ExportXml`.

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
- `Import Protocol`: uploads protocol content from file (XML transport currently).
- `Save Protocol`: persists current protocol to the active saved protocol.
- `Export Protocol`: opens generated protocol output in a new tab (XML transport currently).

## Tree Interaction

- Click any node to load its details in the right pane.
- Right-click any node to open a context menu with `Select Link...` and `Clear Link`.
- Link picker flow: right-click source node -> `Select Link...` -> choose a target node in the modal list; the source node's `Link Id` and `Link Text` are filled automatically from that target.
- Clear-link flow: right-click source node -> `Clear Link` to clear the source node link values.
- Drag and drop behavior:
  - Section nodes reorder at protocol root.
  - Subsection nodes can move between valid parents.
  - Invalid drop targets are blocked.
- New drag affordance and hierarchy cues:
  - Nodes display a visible drag handle.
  - Active drag source uses dashed styling.
  - Drop targets show stronger highlight feedback.
  - Section rows use stronger visual emphasis than child rows.
- Add-section visibility:
  - After `Add Section`, the created section is auto-selected and scrolled into view.
- Section collapse:
  - Top-level sections include collapse/expand toggles.
  - Collapse state persists in browser localStorage (`protocolEditor.collapsedSections`).
  - If a selected child becomes hidden by collapsing, selection is moved to the section node.

## Multi-Protocol Management

The Protocol Editor supports multiple independent protocols. Each protocol has its own document tree, name, and metadata.

### Protocol Selector

The Protocol Selector panel appears between the page header and the toolbar. It shows all saved protocols with action buttons.

#### Creating a Protocol

1. Click the **New** button in the Protocol Selector header.
2. Enter a name in the inline input field.
3. Click **Create** (or press Enter) to create the protocol with a seed document.
4. The editor automatically switches to the new protocol.

#### Switching Protocols

1. Click on any protocol name in the Protocol Selector list.
2. The current protocol is auto-saved before switching.
3. The editor loads the selected protocol's document tree.
4. The active protocol name is displayed in the page subtitle ("Editing: Protocol Name").

#### Renaming a Protocol

1. Click the pencil icon (✏️) next to the protocol name.
2. Edit the name in the inline input field.
3. Click **Save** to confirm, or press Escape to cancel.

#### Deleting a Protocol

1. Click the trash icon (🗑️) next to the protocol name.
2. Confirm the deletion in the browser dialog.
3. **Note**: You cannot delete the active protocol or the default protocol. Switch to a different protocol first.

#### Setting a Default Protocol

1. Click the star icon (⭐) next to a non-default protocol.
2. The protocol is marked as default (shown with a green "Default" badge).
3. The Protocol Viewer will load the default protocol for patients/viewers.
4. Only one protocol can be default at a time.

### Protocol Badges

- **Default** (green): This protocol is served by the Protocol Viewer.
- **Active** (blue): This protocol is currently loaded in the editor.

### Import/Export with Multi-Protocol

- **Import Protocol**: Imports XML content into the currently active protocol, replacing its document tree.
- **Export Protocol**: Exports the currently active protocol's document tree as XML.
- **Save Protocol**: Saves the current editor state to the active protocol in the database.

## Detail Panels

- Statement text is editable.
- Link fields (`Link Id`, `Link Text`) are editable as a manual fallback when not using the context-menu link picker.
- SubText entries support add/remove operations.

## Validation Status

- Build: `dotnet build SleepEditWeb.sln` passes.
- Tests: `dotnet test SleepEditWeb.sln` passes (120 tests).
- Protocol test coverage includes:
  - XML serialization/deserialization hierarchy and field order.
  - Mutation paths and invalid move handling.
  - Undo/redo determinism.
  - Route contract stability for protocol editor endpoints.
  - Admin tab order and active-pane markup expectations.
  - User-facing protocol wording while retaining XML endpoint contracts.

## QA Matrix (Current)

- Completed by automated/regression checks:
  - Admin tab order and active-pane expectations.
  - Protocol editor label wording and endpoint contract stability.
  - Protocol editor build/test regression suite.
- Manual browser validation still recommended before release:
  - Admin embedded protocol editor tab interactions.
  - Drag/drop visual affordance usability in both themes.
  - Section collapse/expand persistence and add-section auto-scroll behavior.
