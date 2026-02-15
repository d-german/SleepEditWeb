import {
    findNodeById,
    isDescendantNode,
    normalizeNodeKind
} from "./protocol-shared-utils.js";

export function loadCollapsedIdSet(storageKey) {
    try {
        const raw = localStorage.getItem(storageKey);
        if (!raw) {
            return new Set();
        }

        const parsed = JSON.parse(raw);
        if (!Array.isArray(parsed)) {
            return new Set();
        }

        return new Set(
            parsed
                .map(item => Number(item))
                .filter(item => Number.isFinite(item))
        );
    } catch {
        return new Set();
    }
}

export function saveCollapsedIdSet(storageKey, ids) {
    try {
        localStorage.setItem(storageKey, JSON.stringify(Array.from(ids)));
    } catch {
        // Ignore localStorage failures and continue with in-memory UI state.
    }
}

export function reconcileCollapsedSectionIds(sections, collapsedSectionIds) {
    const validSectionIds = new Set(
        (sections || [])
            .map(section => Number(section.id))
            .filter(id => Number.isFinite(id))
    );

    const next = new Set();
    collapsedSectionIds.forEach(id => {
        if (validSectionIds.has(id)) {
            next.add(id);
        }
    });

    return next;
}

export function areAllSectionsCollapsed(sections, collapsedSectionIds) {
    if (!sections || !sections.length) {
        return false;
    }

    return sections.every(section => collapsedSectionIds.has(section.id));
}

export function findOwningSectionId(sections, nodeId) {
    if (!Number.isFinite(Number(nodeId))) {
        return null;
    }

    for (const section of sections || []) {
        if (section.id === nodeId || isDescendantNode(section.children || [], nodeId)) {
            return section.id;
        }
    }

    return null;
}

export function findParentId(nodes, targetId, parentId = 0) {
    for (const node of nodes || []) {
        if (node.id === targetId) {
            return parentId;
        }

        const childParentId = findParentId(node.children || [], targetId, node.id);
        if (childParentId != null) {
            return childParentId;
        }
    }

    return null;
}

export function findIndexInParent(sections, parentId, nodeId) {
    if (parentId === 0) {
        return (sections || []).findIndex(node => node.id === nodeId);
    }

    const parent = findNodeById(sections || [], parentId);
    if (!parent) {
        return -1;
    }

    return (parent.children || []).findIndex(node => node.id === nodeId);
}

export function renderProtocolTree({
    treeHost,
    sections,
    selectedNodeId,
    collapsedSectionIds,
    onToggleSection,
    onSelectNode,
    onOpenContextMenu,
    dragDrop,
    pendingScrollToNodeId
}) {
    treeHost.innerHTML = "";

    const root = document.createElement("ul");
    root.className = "protocol-tree-root";

    (sections || []).forEach(section => {
        root.appendChild(buildNode({
            node: section,
            depth: 0,
            selectedNodeId,
            collapsedSectionIds,
            onToggleSection,
            onSelectNode,
            onOpenContextMenu,
            dragDrop
        }));
    });

    treeHost.appendChild(root);
    root.addEventListener("dragover", dragDrop.onDragOver);
    root.addEventListener("drop", dragDrop.onDropOnRoot);
    scrollPendingNodeIntoView(treeHost, selectedNodeId, pendingScrollToNodeId);
}

function buildNode({
    node,
    depth,
    selectedNodeId,
    collapsedSectionIds,
    onToggleSection,
    onSelectNode,
    onOpenContextMenu,
    dragDrop
}) {
    const nodeKind = normalizeNodeKind(node.kind);
    const isSectionNode = nodeKind === "section" && depth === 0;
    const isCollapsedSection = isSectionNode && collapsedSectionIds.has(node.id);

    const item = document.createElement("li");
    item.className = `protocol-tree-item ${isSectionNode ? "protocol-tree-item-section" : ""} ${isCollapsedSection ? "is-collapsed" : ""}`.trim();

    const row = document.createElement("div");
    row.className = "protocol-tree-row";

    const toggle = document.createElement("button");
    toggle.type = "button";
    toggle.className = `protocol-tree-toggle ${isSectionNode ? "" : "is-hidden"}`.trim();
    toggle.setAttribute("aria-label", isCollapsedSection ? "Expand section" : "Collapse section");
    toggle.textContent = isCollapsedSection ? "▸" : "▾";
    if (isSectionNode) {
        toggle.addEventListener("click", event => {
            event.preventDefault();
            event.stopPropagation();
            onToggleSection(node.id);
        });
    }
    row.appendChild(toggle);

    const button = document.createElement("button");
    button.type = "button";
    button.className = `protocol-tree-node protocol-tree-node-${nodeKind || "node"} ${selectedNodeId === node.id ? "is-selected" : ""}`.trim();
    button.style.paddingLeft = `${10 + depth * 14}px`;
    button.dataset.nodeId = String(node.id);
    button.title = "Drag to reorder";
    button.draggable = true;

    const dragHandle = document.createElement("span");
    dragHandle.className = "protocol-tree-drag-handle";
    dragHandle.textContent = "⋮⋮";

    const label = document.createElement("span");
    label.className = "protocol-tree-node-label";
    label.textContent = node.text || "(empty)";

    button.appendChild(dragHandle);
    button.appendChild(label);
    button.addEventListener("click", () => onSelectNode(node.id));
    button.addEventListener("contextmenu", event => {
        event.preventDefault();
        onOpenContextMenu(node.id, event.clientX, event.clientY);
    });
    button.addEventListener("dragstart", event => dragDrop.onDragStart(event, node.id));
    button.addEventListener("dragover", dragDrop.onDragOver);
    button.addEventListener("dragenter", event => dragDrop.onDragEnter(event, button));
    button.addEventListener("dragleave", event => dragDrop.onDragLeave(event, button));
    button.addEventListener("drop", event => dragDrop.onDropOnNode(event, node));
    button.addEventListener("dragend", dragDrop.onDragEnd);
    row.appendChild(button);
    item.appendChild(row);

    if (node.children && node.children.length && !isCollapsedSection) {
        const childList = document.createElement("ul");
        childList.className = "protocol-tree-children";
        node.children.forEach(child => {
            childList.appendChild(buildNode({
                node: child,
                depth: depth + 1,
                selectedNodeId,
                collapsedSectionIds,
                onToggleSection,
                onSelectNode,
                onOpenContextMenu,
                dragDrop
            }));
        });
        item.appendChild(childList);
    }

    return item;
}

function scrollPendingNodeIntoView(treeHost, selectedNodeId, pendingScrollToNodeId) {
    if (pendingScrollToNodeId == null || selectedNodeId !== pendingScrollToNodeId) {
        return;
    }

    const nodeButton = treeHost.querySelector(`.protocol-tree-node[data-node-id="${pendingScrollToNodeId}"]`);
    if (!nodeButton) {
        return;
    }

    scrollNodeIntoContainerView(nodeButton, treeHost);
}

function scrollNodeIntoContainerView(node, fallbackContainer) {
    const container = findScrollableAncestor(node, fallbackContainer);
    if (!container) {
        return;
    }

    const nodeRect = node.getBoundingClientRect();
    const containerRect = container.getBoundingClientRect();
    const padding = 8;

    if (nodeRect.top < containerRect.top) {
        container.scrollTop += nodeRect.top - containerRect.top - padding;
        return;
    }

    if (nodeRect.bottom > containerRect.bottom) {
        container.scrollTop += nodeRect.bottom - containerRect.bottom + padding;
    }
}

function findScrollableAncestor(element, fallback) {
    let current = element?.parentElement || null;
    while (current) {
        const style = window.getComputedStyle(current);
        const overflowY = style.overflowY;
        const canScrollY = (overflowY === "auto" || overflowY === "scroll" || overflowY === "overlay")
            && current.scrollHeight > current.clientHeight;
        if (canScrollY) {
            return current;
        }

        current = current.parentElement;
    }

    return fallback || null;
}
