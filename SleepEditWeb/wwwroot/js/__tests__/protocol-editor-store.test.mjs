import test from "node:test";
import assert from "node:assert/strict";

import {
    applyProtocolEditorStatePayload,
    createProtocolEditorState
} from "../protocol-editor-store.js";

const baseDocument = {
    text: "Protocol",
    sections: []
};

test("createProtocolEditorState initializes counters and timestamps", () => {
    const state = createProtocolEditorState(baseDocument);

    assert.equal(state.document, baseDocument);
    assert.equal(state.undoCount, 0);
    assert.equal(state.redoCount, 0);
    assert.equal(state.lastUpdatedUtc, null);
});

test("applyProtocolEditorStatePayload keeps current state when payload is invalid", () => {
    const state = createProtocolEditorState(baseDocument);
    const next = applyProtocolEditorStatePayload(state, null);

    assert.equal(next, state);
});

test("applyProtocolEditorStatePayload updates state from payload", () => {
    const state = {
        document: baseDocument,
        undoCount: 1,
        redoCount: 2,
        lastUpdatedUtc: "2026-02-15T00:00:00Z"
    };

    const payload = {
        document: { text: "Updated", sections: [{ id: 1, text: "A", children: [] }] },
        undoCount: 5,
        redoCount: 1,
        lastUpdatedUtc: "2026-02-16T00:00:00Z"
    };

    const next = applyProtocolEditorStatePayload(state, payload);

    assert.notEqual(next, state);
    assert.equal(next.document.text, "Updated");
    assert.equal(next.undoCount, 5);
    assert.equal(next.redoCount, 1);
    assert.equal(next.lastUpdatedUtc, "2026-02-16T00:00:00Z");
});
