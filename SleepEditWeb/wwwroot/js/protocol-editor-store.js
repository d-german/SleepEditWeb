export function createProtocolEditorState(initialDocument) {
    return {
        document: initialDocument,
        undoCount: 0,
        redoCount: 0,
        lastUpdatedUtc: null
    };
}

export function applyProtocolEditorStatePayload(currentState, payload) {
    if (!payload || !payload.document) {
        return currentState;
    }

    return {
        ...currentState,
        document: payload.document,
        undoCount: payload.undoCount ?? currentState.undoCount,
        redoCount: payload.redoCount ?? currentState.redoCount,
        lastUpdatedUtc: payload.lastUpdatedUtc ?? currentState.lastUpdatedUtc
    };
}
