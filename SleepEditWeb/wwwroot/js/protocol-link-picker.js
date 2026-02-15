import {
    findNodeById,
    flattenNodes,
    isDescendantNode
} from "./protocol-shared-utils.js";
import {
    loadCollapsedIdSet,
    saveCollapsedIdSet
} from "./protocol-tree.js";

export function createProtocolLinkPickerController({
    nodeContextMenu,
    contextMenuSelectLinkBtn,
    contextMenuClearLinkBtn,
    linkPickerModalEl,
    linkPickerSourceLabel,
    linkPickerSearchInput,
    linkPickerTree,
    collapseStorageKey,
    getSections,
    setSelectedNodeId,
    updateNodeLink,
    setStatus
}) {
    let contextMenuNodeId = null;
    let linkPickerSourceNodeId = null;
    let linkPickerModal = null;
    let collapsedSectionIds = loadCollapsedIdSet(collapseStorageKey);

    function initialize() {
        contextMenuSelectLinkBtn.addEventListener("click", onContextMenuSelectLink);
        contextMenuClearLinkBtn.addEventListener("click", onContextMenuClearLink);
        linkPickerSearchInput.addEventListener("input", renderLinkPickerTargets);

        linkPickerModal = createLinkPickerModal(linkPickerModalEl);
        if (!linkPickerModalEl) {
            return;
        }

        linkPickerModalEl.addEventListener("shown.bs.modal", () => linkPickerSearchInput.focus());
        linkPickerModalEl.addEventListener("hidden.bs.modal", resetLinkPickerState);
    }

    function ensureContextNodeExists() {
        const sections = getSections();

        if (contextMenuNodeId != null && !findNodeById(sections, contextMenuNodeId)) {
            closeNodeMenu();
        }

        if (linkPickerSourceNodeId != null && !findNodeById(sections, linkPickerSourceNodeId)) {
            closeLinkPicker();
        }
    }

    function openNodeMenu(nodeId, x, y) {
        contextMenuNodeId = nodeId;
        nodeContextMenu.style.left = `${x}px`;
        nodeContextMenu.style.top = `${y}px`;
        nodeContextMenu.style.display = "block";
    }

    function closeNodeMenu() {
        contextMenuNodeId = null;
        nodeContextMenu.style.display = "none";
    }

    function handleDocumentClick(event) {
        if (nodeContextMenu.contains(event.target)) {
            return;
        }

        closeNodeMenu();
    }

    function handleEscape() {
        closeNodeMenu();
        if (linkPickerModal) {
            linkPickerModal.hide();
        }
    }

    async function onContextMenuSelectLink() {
        if (contextMenuNodeId == null) {
            return;
        }

        openLinkPicker(contextMenuNodeId);
        closeNodeMenu();
    }

    async function onContextMenuClearLink() {
        if (contextMenuNodeId == null) {
            return;
        }

        const updated = await clearNodeLink(contextMenuNodeId);
        if (updated) {
            setStatus(`Cleared link for #${contextMenuNodeId}.`);
        }
        closeNodeMenu();
    }

    function openLinkPicker(sourceNodeId) {
        const source = findNodeById(getSections(), sourceNodeId);
        if (!source) {
            return;
        }

        linkPickerSourceNodeId = sourceNodeId;
        setSelectedNodeId(sourceNodeId);
        linkPickerSearchInput.value = "";
        linkPickerSourceLabel.textContent = `Source: #${source.id} ${source.text || "(empty)"}`;
        renderLinkPickerTargets();
        if (linkPickerModal) {
            linkPickerModal.show();
        }
    }

    function closeLinkPicker() {
        if (linkPickerModal) {
            linkPickerModal.hide();
            return;
        }

        resetLinkPickerState();
    }

    function resetLinkPickerState() {
        linkPickerSourceNodeId = null;
        linkPickerSourceLabel.textContent = "";
        linkPickerSearchInput.value = "";
        linkPickerTree.innerHTML = "";
    }

    function renderLinkPickerTargets() {
        linkPickerTree.innerHTML = "";
        reconcileCollapsedSections();

        const source = getLinkPickerSourceNode();
        if (!source) {
            const empty = document.createElement("div");
            empty.className = "text-muted small";
            empty.textContent = "Select a source node to choose a link target.";
            linkPickerTree.appendChild(empty);
            return;
        }

        const filter = normalizeFilter(linkPickerSearchInput.value);
        const sections = getSections();
        sections.forEach(section => {
            const group = buildLinkPickerSectionGroup(section, source.id, filter);
            if (group) {
                linkPickerTree.appendChild(group);
            }
        });

        if (!linkPickerTree.children.length) {
            const empty = document.createElement("div");
            empty.className = "text-muted small";
            empty.textContent = "No matching nodes found.";
            linkPickerTree.appendChild(empty);
        }
    }

    function reconcileCollapsedSections() {
        const validSectionIds = new Set(
            getSections()
                .map(section => Number(section.id))
                .filter(id => Number.isFinite(id))
        );

        const next = new Set();
        collapsedSectionIds.forEach(id => {
            if (validSectionIds.has(id)) {
                next.add(id);
            }
        });

        if (next.size !== collapsedSectionIds.size) {
            collapsedSectionIds = next;
            saveCollapsedIdSet(collapseStorageKey, collapsedSectionIds);
        }
    }

    function getLinkPickerSourceNode() {
        if (linkPickerSourceNodeId == null) {
            return null;
        }

        return findNodeById(getSections(), linkPickerSourceNodeId);
    }

    function normalizeFilter(value) {
        return (value || "").trim().toLowerCase();
    }

    function buildLinkPickerSectionGroup(section, sourceId, filter) {
        const entries = flattenNodes([section])
            .filter(entry => isCandidateTarget(sourceId, entry.node, filter));
        if (!entries.length) {
            return null;
        }

        const canSelectSection = entries.some(entry => entry.node.id === section.id);
        const childEntries = entries.filter(entry => entry.node.id !== section.id);
        const isCollapsed = collapsedSectionIds.has(section.id);

        const group = document.createElement("div");
        group.className = "list-group protocol-link-picker-list protocol-link-picker-section-group";
        group.appendChild(buildSectionHeader(section, canSelectSection, isCollapsed));
        if (!isCollapsed) {
            childEntries.forEach(entry => group.appendChild(buildLinkTargetButton(entry)));
        }

        return group;
    }

    function buildSectionHeader(section, canSelectSection, isCollapsed) {
        const header = document.createElement("div");
        header.className = "list-group-item protocol-link-picker-item protocol-link-picker-section-header";

        const toggle = document.createElement("button");
        toggle.type = "button";
        toggle.className = "protocol-link-picker-section-toggle";
        toggle.setAttribute("aria-label", isCollapsed ? "Expand section" : "Collapse section");
        toggle.textContent = isCollapsed ? "▸" : "▾";
        toggle.addEventListener("click", event => {
            event.preventDefault();
            event.stopPropagation();
            toggleSectionCollapsed(section.id);
            renderLinkPickerTargets();
        });
        header.appendChild(toggle);

        if (canSelectSection) {
            const selectButton = document.createElement("button");
            selectButton.type = "button";
            selectButton.className = "protocol-link-picker-section-target";
            selectButton.textContent = `#${section.id} ${section.text || "(empty)"}`;
            selectButton.addEventListener("click", async () => {
                await selectLinkTarget(section.id);
            });
            header.appendChild(selectButton);
            return header;
        }

        const label = document.createElement("span");
        label.className = "protocol-link-picker-section-label";
        label.textContent = `#${section.id} ${section.text || "(empty)"}`;
        header.appendChild(label);
        return header;
    }

    function buildLinkTargetButton(entry) {
        const button = document.createElement("button");
        button.type = "button";
        button.className = "list-group-item list-group-item-action protocol-link-picker-item";
        button.style.paddingLeft = `${entry.depth * 18 + 12}px`;
        button.textContent = `#${entry.node.id} ${entry.node.text || "(empty)"}`;
        button.addEventListener("click", async () => {
            await selectLinkTarget(entry.node.id);
        });
        return button;
    }

    function isCandidateTarget(sourceId, targetNode, filter) {
        if (!targetNode || targetNode.id === sourceId) {
            return false;
        }

        const source = findNodeById(getSections(), sourceId);
        if (!source) {
            return false;
        }

        if (isDescendantNode(source.children || [], targetNode.id)) {
            return false;
        }

        if (!filter) {
            return true;
        }

        const targetText = `${targetNode.id} ${targetNode.text || ""}`.toLowerCase();
        return targetText.includes(filter);
    }

    async function selectLinkTarget(targetNodeId) {
        const source = getLinkPickerSourceNode();
        if (!source) {
            return;
        }

        if (source.id === targetNodeId || isDescendantNode(source.children || [], targetNodeId)) {
            setStatus("Invalid link target selected.");
            return;
        }

        const target = findNodeById(getSections(), targetNodeId);
        if (!target) {
            setStatus("Target node not found.");
            return;
        }

        const updated = await updateNodeLink(source.id, target.id, target.text || "");
        if (!updated) {
            return;
        }

        closeLinkPicker();
        setStatus(`Linked #${source.id} to #${target.id}.`);
    }

    async function clearNodeLink(sourceNodeId) {
        return await updateNodeLink(sourceNodeId, -1, "");
    }

    function toggleSectionCollapsed(sectionId) {
        if (collapsedSectionIds.has(sectionId)) {
            collapsedSectionIds.delete(sectionId);
        } else {
            collapsedSectionIds.add(sectionId);
        }

        saveCollapsedIdSet(collapseStorageKey, collapsedSectionIds);
    }

    return {
        initialize,
        ensureContextNodeExists,
        openNodeMenu,
        closeNodeMenu,
        closeLinkPicker,
        handleDocumentClick,
        handleEscape
    };
}

function createLinkPickerModal(linkPickerModalEl) {
    if (!linkPickerModalEl || !window.bootstrap || !window.bootstrap.Modal) {
        return null;
    }

    return new window.bootstrap.Modal(linkPickerModalEl);
}
