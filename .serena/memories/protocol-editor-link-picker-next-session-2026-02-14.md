# Protocol Editor Next Session Handoff (2026-02-14)

## Why this memory
User asked to preserve a concrete implementation plan for legacy-style node link selection due low remaining context.

## Current implemented state (already done)
- Sleep Note Editor restored as default landing route.
- Protocol Editor moved to secondary nav position.
- Protocol Editor supports:
  - Export XML
  - Save XML to server path
  - Import XML from server path
  - Import XML from local browser file upload (`ImportXmlUpload` endpoint)
  - Set current loaded protocol as default (`SetDefaultProtocol` endpoint/button)
- Startup load now prefers `DefaultProtocolPath` then `StartupProtocolPath`.
- Config keys under `ProtocolEditor`:
  - `DefaultProtocolPath`
  - `StartupProtocolPath`
  - `SaveProtocolPath`
- Tests passing after latest changes.

## Requested next enhancement (legacy parity)
Implement right-click node context menu + link-node chooser modal like WinForms:
- Right-click tree node opens context menu.
- Menu includes `Select Link...`.
- Selecting it opens modal tree picker of all nodes.
- User chooses target node and app sets source node `LinkId` + `LinkText` automatically.

## Proposed implementation details
1. Add custom context menu in Protocol Editor tree UI.
   - Trigger on node `contextmenu` event.
   - Keep existing left-click selection behavior.
2. Add Bootstrap modal "Select Link node".
   - Render hierarchical node tree with search/filter.
   - Show node text and ID.
3. Add client methods (small, low complexity):
   - `openNodeMenu(nodeId, x, y)`
   - `openLinkPicker(sourceNodeId)`
   - `selectLinkTarget(targetNodeId)`
   - `clearNodeLink(sourceNodeId)`
4. Reuse existing backend endpoint:
   - Use `UpdateNode` POST (no new domain action required) to set `linkId/linkText`.
5. Optional validation guard:
   - Disallow self-link and optionally descendant-link targets in UI.
6. Keep manual Link Id/Text fields as fallback for admin power users.

## SOLID / quality guardrails for implementation
- Keep JS functions narrowly scoped (single responsibility).
- Avoid large condition-heavy handlers; extract helpers to keep cyclomatic complexity around 5 or less.
- Reuse existing server operations instead of adding redundant endpoints.
- Add or update tests where practical for link assignment behavior.

## Suggested first coding steps next session
1. Add modal markup + hidden context menu markup in `Views/ProtocolEditor/Index.cshtml`.
2. Add tree right-click wiring and menu positioning logic.
3. Add modal tree builder and target selection logic.
4. Route selection through existing `UpdateNode` flow.
5. Validate with `dotnet test SleepEditWeb.sln`.

## User intent reminder
This feature is for convenience parity with the legacy protocol editor workflow and should feel fast and direct for technicians.
