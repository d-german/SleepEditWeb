# Protocol Editor Reference Contract

This document captures the legacy protocol editor behavior and XML shape from:

- `sleepEdit_sln_only_20260213_090126/sleepEdit/protocols/protocol.xml`
- `sleepEdit_sln_only_20260213_090126/sleepEdit/protocols/protocol.xsd`
- `sleepEdit_sln_only_20260213_090126/sleepEdit/ProtocolEditUi.cs`
- `sleepEdit_sln_only_20260213_090126/Protocol/Protocols/Protocols/*.cs`

## Top-Level Sections (In Order)

1. Diagnostic Polysomnogram:
2. Split Night Polysomnogram:
3. CPAP Titration Polysomnogram:
4. BiPAP Titration Polysomnogram:
5. Supplemental Oxygen:
6. Respiratory Event Determination:
7. Post-Op Polysomnogram:
8. Treatment Intolerance:
9. Oral Appliance Protocol:
10. Ventilator:
11. CPAP/BIPAP Failure:
12. End of Study:

## XML Structure Contract

Hierarchy:

- `Protocol`
- `Section` (0..n)
- `SubSection` (recursive, up to 3 nested levels in XSD)

Node fields in written order (`ProtocolWriter.writeNode`):

1. `Id`
2. `LinkId`
3. `LinkText`
4. `text`
5. `SubText` (0..n, optional)

Important notes:

- Root `Protocol` has `text` = protocol title (for current sample: `Saint Luke's Protocol`).
- Root and all descendants use the same field names.
- `SubText` is written only when there are entries (`Element.SubTextList.Count > 0`).
- Sample file has:
  - 145 total nodes (1 root + 12 sections + 132 subsections)
  - 145 unique IDs (no duplicates)
  - Max observed subsection nesting depth of 2 below section nodes

## XSD Type Expectations

From `protocol.xsd`:

- `Protocol/Id` and `Protocol/LinkId`: `xs:byte`
- `Section/Id`: `xs:unsignedByte`
- `Section/LinkId`: `xs:byte`
- first-level `SubSection/Id`: `xs:unsignedByte`
- first-level `SubSection/LinkId`: `xs:short`
- deeper-level `SubSection/LinkId`: `xs:short`, deepest branch narrows to `xs:unsignedByte`

Implementation detail:

- Legacy runtime uses `int` for `Id`/`LinkId` in `ProtocolNode`.
- `UniqueId.GetId()` uses `Guid.NewGuid().GetHashCode()` (full `int` range), which does not align with strict byte-range XSD typing.
- For compatibility in the web editor, preserve the XML element names/order and section/subsection hierarchy; validate whether consumers enforce strict XSD numeric ranges before constraining IDs.

## Legacy Behavior Contract

### Add Node

- Adds child via `AddCmd`.
- Assigns a new ID (`UniqueId.GetId()`).
- Pushes command to undo stack.

### Remove Node

- Removes selected node via `RemoveCmd`.
- Clears link references in other nodes where `LinkId == removedNode.Id`.
- Undo restores removed node and prior link IDs.

### Drag/Drop Move

- Drag source is cloned for DnD payload.
- Invalid moves are blocked when:
  - target is null
  - moving a section under non-root
  - target is self/parent/descendant
- Move operation (`MoveCmd`) inserts under new parent and removes original from old parent.
- Undo restores original parent/index and removes moved copy.

### Reorder (Nudge Up/Down)

- Implemented with `NudgeUpCmd` and `NudgeDownCmd`.
- Undo restores original index.

### Undo/Redo

- Global stacks: `UndoManager.UndoStack` and `UndoManager.RedoStack`.
- `UndoCmd.Execute()` pops undo stack and calls `UndoExecute()`.
- `RedoCmd.Execute()` pops redo stack and re-executes command.

### Link Editing

- Link selection sets `currentNode.LinkId = selectedNode.Id`.
- UI also sets `LinkText` from selected node text.
- Legacy code explicitly marks undo/redo support for link assignment as TODO.

### SubText Editing

- Add/remove subtext mutates `Element.SubTextList`.
- Legacy code marks undo/redo for subtext edits as TODO.

## Edge Cases To Cover In New Editor

- Removing a node should clean up inbound links referencing the removed ID.
- Drag/drop must reject cycles and illegal root/section moves.
- Undo/redo must be deterministic across add/remove/move/nudge/link/subtext changes.
- Empty `LinkText` and empty `SubText` collections must serialize cleanly.
- XML export must preserve section order and node field order.
