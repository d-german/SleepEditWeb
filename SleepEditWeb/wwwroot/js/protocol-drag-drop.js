import {
    findNodeById,
    normalizeNodeKind
} from "./protocol-shared-utils.js";

export function createProtocolDragDropController({
    treeHost,
    getSections,
    setSelectedNodeId,
    postMoveNode,
    setStatus,
    findParentId,
    findIndexInParent,
    requestRender
}) {
    let dragNodeId = null;

    function onDragStart(event, nodeId) {
        dragNodeId = nodeId;
        const source = treeHost.querySelector(`.protocol-tree-node[data-node-id="${nodeId}"]`);
        if (source) {
            source.classList.add("is-dragging");
        }
        event.dataTransfer.effectAllowed = "move";
        event.dataTransfer.setData("text/plain", String(nodeId));
    }

    function onDragOver(event) {
        event.preventDefault();
        event.dataTransfer.dropEffect = "move";
    }

    function onDragEnter(event, element) {
        event.preventDefault();
        element.classList.add("is-drop-target");
    }

    function onDragLeave(event, element) {
        if (event.currentTarget.contains(event.relatedTarget)) {
            return;
        }

        element.classList.remove("is-drop-target");
    }

    async function onDropOnNode(event, targetNode) {
        event.preventDefault();
        clearDropIndicators();

        const draggedId = Number(event.dataTransfer.getData("text/plain") || dragNodeId);
        if (!Number.isFinite(draggedId) || draggedId === targetNode.id) {
            return;
        }

        const sections = getSections();
        const dragged = findNodeById(sections, draggedId);
        if (!dragged) {
            return;
        }

        const moveRequest = buildMoveRequest(sections, dragged, targetNode, findParentId, findIndexInParent);
        if (!moveRequest) {
            setStatus("Invalid drop target.");
            return;
        }

        const moved = await postMoveNode(moveRequest);
        if (!moved) {
            return;
        }

        setSelectedNodeId(draggedId);
        requestRender();
    }

    async function onDropOnRoot(event) {
        event.preventDefault();
        clearDropIndicators();

        const draggedId = Number(event.dataTransfer.getData("text/plain") || dragNodeId);
        if (!Number.isFinite(draggedId)) {
            return;
        }

        const sections = getSections();
        const dragged = findNodeById(sections, draggedId);
        if (!dragged || normalizeNodeKind(dragged.kind) !== "section") {
            return;
        }

        const moved = await postMoveNode({
            nodeId: draggedId,
            parentId: 0,
            targetIndex: sections.length
        });
        if (!moved) {
            return;
        }

        setSelectedNodeId(draggedId);
        requestRender();
    }

    function onDragEnd() {
        dragNodeId = null;
        clearDropIndicators();
    }

    function clearDropIndicators() {
        treeHost.querySelectorAll(".is-drop-target, .is-dragging").forEach(element => {
            element.classList.remove("is-drop-target", "is-dragging");
        });
    }

    return {
        onDragStart,
        onDragOver,
        onDragEnter,
        onDragLeave,
        onDropOnNode,
        onDropOnRoot,
        onDragEnd
    };
}

function buildMoveRequest(sections, dragged, target, findParentId, findIndexInParent) {
    const draggedKind = normalizeNodeKind(dragged.kind);
    const targetKind = normalizeNodeKind(target.kind);

    if (draggedKind === "section") {
        if (targetKind !== "section") {
            return null;
        }

        const targetIndex = findIndexInParent(sections, 0, target.id);
        if (targetIndex < 0) {
            return null;
        }

        return {
            nodeId: dragged.id,
            parentId: 0,
            targetIndex
        };
    }

    if (targetKind === "section") {
        return {
            nodeId: dragged.id,
            parentId: target.id,
            targetIndex: (target.children || []).length
        };
    }

    const targetParentId = findParentId(sections, target.id, 0);
    if (targetParentId == null) {
        return null;
    }

    const targetIndex = findIndexInParent(sections, targetParentId, target.id);
    if (targetIndex < 0) {
        return null;
    }

    return {
        nodeId: dragged.id,
        parentId: targetParentId,
        targetIndex
    };
}
