import {
    getProtocolEditorState,
    postProtocolEditorAction,
    uploadProtocolEditorXml,
    saveProtocolEditorXml,
    setProtocolEditorDefault
} from "./protocol-editor-api.js";
import {
    createProtocolEditorState,
    applyProtocolEditorStatePayload
} from "./protocol-editor-store.js";
import {
    findNodeById,
    isDescendantNode
} from "./protocol-shared-utils.js";
import {
    loadCollapsedIdSet,
    saveCollapsedIdSet,
    reconcileCollapsedSectionIds,
    areAllSectionsCollapsed,
    findOwningSectionId,
    findParentId,
    findIndexInParent,
    renderProtocolTree
} from "./protocol-tree.js";
import { createProtocolDragDropController } from "./protocol-drag-drop.js";
import { createProtocolLinkPickerController } from "./protocol-link-picker.js";

const COLLAPSE_STORAGE_KEY = "protocolEditor.collapsedSections";
const LINK_PICKER_COLLAPSE_STORAGE_KEY = "protocolEditor.linkPickerCollapsedSections";

export function initializeProtocolEditor(config) {
    const token = document.querySelector("input[name=\"__RequestVerificationToken\"]")?.value || "";
    const elements = resolveElements();
    if (!elements.treeHost) {
        return;
    }

    let state = createProtocolEditorState(config.initialDocument);
    let selectedNodeId = null;
    let pendingScrollToNodeId = null;
    let collapsedSectionIds = loadCollapsedIdSet(COLLAPSE_STORAGE_KEY);

    const dragDrop = createProtocolDragDropController({
        treeHost: elements.treeHost,
        getSections: () => state.document.sections || [],
        setSelectedNodeId: value => {
            selectedNodeId = value;
        },
        postMoveNode: request => postState("MoveNode", request, { skipRender: true }),
        setStatus,
        findParentId,
        findIndexInParent,
        requestRender: renderAll
    });

    const linkPicker = createProtocolLinkPickerController({
        nodeContextMenu: elements.nodeContextMenu,
        contextMenuSelectLinkBtn: elements.contextMenuSelectLinkBtn,
        contextMenuClearLinkBtn: elements.contextMenuClearLinkBtn,
        linkPickerModalEl: elements.linkPickerModalEl,
        linkPickerSourceLabel: elements.linkPickerSourceLabel,
        linkPickerSearchInput: elements.linkPickerSearchInput,
        linkPickerTree: elements.linkPickerTree,
        collapseStorageKey: LINK_PICKER_COLLAPSE_STORAGE_KEY,
        getSections: () => state.document.sections || [],
        setSelectedNodeId: value => {
            selectedNodeId = value;
            renderAll();
        },
        updateNodeLink,
        setStatus
    });

    bindEvents();
    linkPicker.initialize();
    initialize();

    async function initialize() {
        await refreshState();
        renderAll();
    }

    function bindEvents() {
        document.getElementById("addSectionBtn").addEventListener("click", onAddSection);
        document.getElementById("addChildBtn").addEventListener("click", onAddChild);
        document.getElementById("removeNodeBtn").addEventListener("click", onRemoveNode);
        document.getElementById("undoBtn").addEventListener("click", () => postState("Undo", {}));
        document.getElementById("redoBtn").addEventListener("click", () => postState("Redo", {}));
        document.getElementById("resetBtn").addEventListener("click", onReset);
        elements.toggleAllSectionsBtn.addEventListener("click", onToggleAllSections);
        document.getElementById("importXmlBtn").addEventListener("click", onImportXml);
        elements.importXmlFileInput.addEventListener("change", onImportXmlSelected);
        document.getElementById("saveXmlBtn").addEventListener("click", onSaveXml);
        document.getElementById("setDefaultProtocolBtn").addEventListener("click", onSetDefaultProtocol);
        document.getElementById("exportXmlBtn").addEventListener("click", onExportXml);
        document.getElementById("addSubTextBtn").addEventListener("click", onAddSubText);
        document.getElementById("removeSubTextBtn").addEventListener("click", onRemoveSubText);

        elements.nodeTextInput.addEventListener("blur", commitNodeUpdate);
        elements.linkIdInput.addEventListener("blur", commitNodeUpdate);
        elements.linkTextInput.addEventListener("blur", commitNodeUpdate);
        document.addEventListener("keydown", onKeydown);
        document.addEventListener("click", linkPicker.handleDocumentClick);
        document.addEventListener("scroll", linkPicker.closeNodeMenu, true);
    }

    async function refreshState() {
        const result = await getProtocolEditorState(config.stateUrl);
        if (!result.ok) {
            setStatus(result.error);
            return;
        }

        state = applyProtocolEditorStatePayload(state, result.payload);
        ensureSelection();
    }

    function ensureSelection() {
        if (selectedNodeId == null) {
            selectedNodeId = state.document.sections.length ? state.document.sections[0].id : null;
            return;
        }

        if (!findNodeById(state.document.sections, selectedNodeId)) {
            selectedNodeId = state.document.sections.length ? state.document.sections[0].id : null;
        }
    }

    function reconcileCollapsedSections() {
        const next = reconcileCollapsedSectionIds(state.document.sections || [], collapsedSectionIds);
        if (next.size !== collapsedSectionIds.size) {
            collapsedSectionIds = next;
            saveCollapsedIdSet(COLLAPSE_STORAGE_KEY, collapsedSectionIds);
        }
    }

    function renderAll() {
        ensureSelection();
        linkPicker.ensureContextNodeExists();
        reconcileCollapsedSections();
        renderTree();
        renderDetails();
        updateToggleAllSectionsButton();
        setStatus(`Undo: ${state.undoCount} | Redo: ${state.redoCount}`);
    }

    function renderTree() {
        renderProtocolTree({
            treeHost: elements.treeHost,
            sections: state.document.sections || [],
            selectedNodeId,
            collapsedSectionIds,
            pendingScrollToNodeId,
            onToggleSection: toggleSectionCollapsed,
            onSelectNode: nodeId => {
                selectedNodeId = nodeId;
                renderAll();
            },
            onOpenContextMenu: (nodeId, x, y) => {
                linkPicker.openNodeMenu(nodeId, x, y);
            },
            dragDrop
        });

        pendingScrollToNodeId = null;
    }

    function toggleSectionCollapsed(sectionId) {
        if (collapsedSectionIds.has(sectionId)) {
            collapsedSectionIds.delete(sectionId);
            const section = findNodeById(state.document.sections, sectionId);
            const firstChild = section && section.children && section.children.length ? section.children[0] : null;
            if (firstChild) {
                pendingScrollToNodeId = firstChild.id;
            }
            saveCollapsedIdSet(COLLAPSE_STORAGE_KEY, collapsedSectionIds);
            renderAll();
            return;
        }

        collapsedSectionIds.add(sectionId);
        const section = findNodeById(state.document.sections, sectionId);
        if (section && isDescendantNode(section.children || [], selectedNodeId)) {
            selectedNodeId = sectionId;
        }
        saveCollapsedIdSet(COLLAPSE_STORAGE_KEY, collapsedSectionIds);
        renderAll();
    }

    function onToggleAllSections() {
        const sections = state.document.sections || [];
        if (!sections.length) {
            return;
        }

        if (areAllSectionsCollapsed(sections, collapsedSectionIds)) {
            collapsedSectionIds = new Set();
            saveCollapsedIdSet(COLLAPSE_STORAGE_KEY, collapsedSectionIds);
            renderAll();
            return;
        }

        collapsedSectionIds = new Set(sections.map(section => section.id));
        const owningSectionId = findOwningSectionId(sections, selectedNodeId);
        if (owningSectionId != null) {
            selectedNodeId = owningSectionId;
        }

        saveCollapsedIdSet(COLLAPSE_STORAGE_KEY, collapsedSectionIds);
        renderAll();
    }

    function updateToggleAllSectionsButton() {
        const sections = state.document.sections || [];
        elements.toggleAllSectionsBtn.disabled = sections.length === 0;
        elements.toggleAllSectionsBtn.textContent = areAllSectionsCollapsed(sections, collapsedSectionIds)
            ? "Expand Sections"
            : "Collapse Sections";
    }

    function renderDetails() {
        const selected = getSelectedNode();
        if (!selected) {
            elements.statementIdInput.value = "";
            elements.nodeTextInput.value = "";
            elements.linkIdInput.value = "";
            elements.linkTextInput.value = "";
            elements.subTextList.innerHTML = "";
            setInputsEnabled(false);
            return;
        }

        setInputsEnabled(true);
        elements.statementIdInput.value = selected.id;
        elements.nodeTextInput.value = selected.text || "";
        elements.linkIdInput.value = selected.linkId ?? -1;
        elements.linkTextInput.value = selected.linkText || "";
        renderSubText(selected.subText || []);
    }

    function renderSubText(items) {
        elements.subTextList.innerHTML = "";
        items.forEach(value => {
            const option = document.createElement("option");
            option.value = value;
            option.textContent = value;
            elements.subTextList.appendChild(option);
        });
    }

    function setInputsEnabled(enabled) {
        elements.nodeTextInput.disabled = !enabled;
        elements.linkIdInput.disabled = !enabled;
        elements.linkTextInput.disabled = !enabled;
        elements.subTextInput.disabled = !enabled;
        elements.subTextList.disabled = !enabled;
        document.getElementById("addChildBtn").disabled = !enabled;
        document.getElementById("removeNodeBtn").disabled = !enabled;
        document.getElementById("addSubTextBtn").disabled = !enabled;
        document.getElementById("removeSubTextBtn").disabled = !enabled;
    }

    function getSelectedNode() {
        if (selectedNodeId == null) {
            return null;
        }

        return findNodeById(state.document.sections, selectedNodeId);
    }

    async function updateNodeLink(sourceNodeId, linkId, linkText) {
        const source = findNodeById(state.document.sections, sourceNodeId);
        if (!source) {
            setStatus("Source node not found.");
            return false;
        }

        selectedNodeId = source.id;
        return await postState("UpdateNode", {
            nodeId: source.id,
            text: source.text || "",
            linkId,
            linkText: linkText || ""
        });
    }

    async function commitNodeUpdate() {
        if (selectedNodeId == null) {
            return;
        }

        await postState("UpdateNode", {
            nodeId: selectedNodeId,
            text: elements.nodeTextInput.value || "",
            linkId: Number.isFinite(Number(elements.linkIdInput.value)) ? Number(elements.linkIdInput.value) : -1,
            linkText: elements.linkTextInput.value || ""
        });
    }

    async function onAddSection() {
        const text = prompt("Section name:", "New Section");
        if (text == null) {
            return;
        }

        const sectionsBeforeAdd = state.document.sections || [];
        const sectionIdsBeforeAdd = new Set(sectionsBeforeAdd.map(section => section.id));
        const success = await postState("AddSection", { text }, { skipRender: true });
        if (!success) {
            return;
        }

        const sectionsAfterAdd = state.document.sections || [];
        const addedSection = sectionsAfterAdd.find(section => !sectionIdsBeforeAdd.has(section.id));
        const sectionToSelect = addedSection || sectionsAfterAdd[sectionsAfterAdd.length - 1];
        if (sectionToSelect) {
            selectedNodeId = sectionToSelect.id;
            pendingScrollToNodeId = sectionToSelect.id;
        }

        renderAll();
    }

    async function onAddChild() {
        if (selectedNodeId == null) {
            return;
        }

        const text = prompt("Child statement:", "New Node");
        if (text == null) {
            return;
        }

        await postState("AddChild", {
            parentId: selectedNodeId,
            text
        });
    }

    async function onRemoveNode() {
        if (selectedNodeId == null) {
            return;
        }

        if (!confirm("Remove selected node?")) {
            return;
        }

        await postState("RemoveNode", {
            nodeId: selectedNodeId
        });
        ensureSelection();
        renderAll();
    }

    async function onAddSubText() {
        if (selectedNodeId == null) {
            return;
        }

        const value = (elements.subTextInput.value || "").trim();
        if (!value) {
            return;
        }

        await postState("AddSubText", {
            nodeId: selectedNodeId,
            value
        });
        elements.subTextInput.value = "";
    }

    async function onRemoveSubText() {
        if (selectedNodeId == null || !elements.subTextList.value) {
            return;
        }

        await postState("RemoveSubText", {
            nodeId: selectedNodeId,
            value: elements.subTextList.value
        });
    }

    async function onReset() {
        if (!confirm("Reset protocol to starter content?")) {
            return;
        }

        await postState("Reset", {});
        ensureSelection();
        renderAll();
    }

    function onExportXml() {
        window.open(config.exportXmlUrl, "_blank");
    }

    function onImportXml() {
        elements.importXmlFileInput.click();
    }

    async function onImportXmlSelected() {
        const file = elements.importXmlFileInput.files && elements.importXmlFileInput.files[0];
        if (!file) {
            return;
        }

        try {
            const result = await uploadProtocolEditorXml(file, token, config.importXmlUploadUrl);
            if (!result.ok) {
                setStatus(result.error);
                return;
            }

            const next = result.payload;
            state = applyProtocolEditorStatePayload(state, next);
            ensureSelection();
            renderAll();
            setStatus(`Imported protocol and saved to ${next.savedPath || "server storage"}`);
        } catch {
            setStatus("Unable to reach server while importing protocol.");
        } finally {
            elements.importXmlFileInput.value = "";
        }
    }

    async function onSaveXml() {
        const result = await saveProtocolEditorXml(token, config.saveXmlUrl);
        if (!result.ok) {
            setStatus(result.error);
            return;
        }

        const payload = result.payload;
        state = applyProtocolEditorStatePayload(state, payload);
        ensureSelection();
        renderAll();
        setStatus(`Saved protocol to ${payload.savedPath || "configured path"}`);
    }

    async function onSetDefaultProtocol() {
        const result = await setProtocolEditorDefault(token, config.setDefaultProtocolUrl);
        if (!result.ok) {
            setStatus(result.error);
            return;
        }

        const payload = result.payload;
        state = applyProtocolEditorStatePayload(state, payload);
        ensureSelection();
        renderAll();
        setStatus(`Set default protocol at ${payload.defaultPath || "configured path"}`);
    }

    async function onKeydown(event) {
        if (event.key === "Escape") {
            linkPicker.handleEscape();
            return;
        }

        if (!event.ctrlKey) {
            return;
        }

        if (event.key.toLowerCase() === "z") {
            event.preventDefault();
            await postState("Undo", {});
            return;
        }

        if (event.key.toLowerCase() === "y") {
            event.preventDefault();
            await postState("Redo", {});
        }
    }

    async function postState(action, payload, options) {
        const settings = options || {};
        const result = await postProtocolEditorAction(action, payload, token, config.actionRouteTemplate);
        if (!result.ok) {
            setStatus(result.error);
            return false;
        }

        state = applyProtocolEditorStatePayload(state, result.payload);
        ensureSelection();
        if (!settings.skipRender) {
            renderAll();
        }

        return true;
    }

    function setStatus(message) {
        elements.statusEl.textContent = message;
    }
}

function resolveElements() {
    return {
        statusEl: document.getElementById("protocolEditorStatus"),
        treeHost: document.getElementById("protocolTree"),
        nodeTextInput: document.getElementById("nodeTextInput"),
        statementIdInput: document.getElementById("statementIdInput"),
        linkIdInput: document.getElementById("linkIdInput"),
        linkTextInput: document.getElementById("linkTextInput"),
        subTextInput: document.getElementById("subTextInput"),
        subTextList: document.getElementById("subTextList"),
        importXmlFileInput: document.getElementById("importXmlFileInput"),
        nodeContextMenu: document.getElementById("nodeContextMenu"),
        contextMenuSelectLinkBtn: document.getElementById("contextMenuSelectLinkBtn"),
        contextMenuClearLinkBtn: document.getElementById("contextMenuClearLinkBtn"),
        linkPickerModalEl: document.getElementById("linkPickerModal"),
        linkPickerSourceLabel: document.getElementById("linkPickerSourceLabel"),
        linkPickerSearchInput: document.getElementById("linkPickerSearchInput"),
        linkPickerTree: document.getElementById("linkPickerTree"),
        toggleAllSectionsBtn: document.getElementById("toggleAllSectionsBtn")
    };
}
