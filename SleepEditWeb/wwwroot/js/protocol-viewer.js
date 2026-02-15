import {
    buildNodeRelationshipMaps,
    findNodeById,
    flattenNodes,
    hasNodeLink,
    toDateInputValue,
    formatStudyDate
} from "./protocol-shared-utils.js";

const STORAGE_KEYS = {
    techNames: "protocolViewer.techNames",
    maskStyles: "protocolViewer.maskStyles",
    maskSizes: "protocolViewer.maskSizes"
};

export function initializeProtocolViewer(config) {
    const elements = resolveElements();
    if (!elements.statusEl) {
        return;
    }

    const { nodeLookup, parentLookup, sectionLookup } = buildNodeRelationshipMaps(config.documentModel.sections);
    const subTextSelections = new Map();
    const checkedNodeIds = new Set();

    let techNames = loadList(STORAGE_KEYS.techNames, config.initialTechNames);
    let maskStyles = loadList(STORAGE_KEYS.maskStyles, config.initialMaskStyles);
    let maskSizes = loadList(STORAGE_KEYS.maskSizes, config.initialMaskSizes);

    bindEvents();
    renderAllSections();
    refreshLookupSelects();
    elements.studyDateInput.value = toDateInputValue(config.initialStudyDate);
    setStatus("Ready");

    function bindEvents() {
        document.getElementById("addTechNameBtn").addEventListener("click", () => addLookupValue("Tech Name", techNames, STORAGE_KEYS.techNames, refreshLookupSelects));
        document.getElementById("removeTechNameBtn").addEventListener("click", () => removeLookupValue("Tech Name", techNames, STORAGE_KEYS.techNames, refreshLookupSelects));
        document.getElementById("clearTechNameBtn").addEventListener("click", () => clearLookupValues("Tech Name", techNames, STORAGE_KEYS.techNames, refreshLookupSelects));

        document.getElementById("addMaskStyleBtn").addEventListener("click", () => addLookupValue("Mask Style", maskStyles, STORAGE_KEYS.maskStyles, refreshLookupSelects));
        document.getElementById("removeMaskStyleBtn").addEventListener("click", () => removeLookupValue("Mask Style", maskStyles, STORAGE_KEYS.maskStyles, refreshLookupSelects));
        document.getElementById("clearMaskStyleBtn").addEventListener("click", () => clearLookupValues("Mask Style", maskStyles, STORAGE_KEYS.maskStyles, refreshLookupSelects));

        document.getElementById("addMaskSizeBtn").addEventListener("click", () => addLookupValue("Mask Size", maskSizes, STORAGE_KEYS.maskSizes, refreshLookupSelects));
        document.getElementById("removeMaskSizeBtn").addEventListener("click", () => removeLookupValue("Mask Size", maskSizes, STORAGE_KEYS.maskSizes, refreshLookupSelects));
        document.getElementById("clearMaskSizeBtn").addEventListener("click", () => clearLookupValues("Mask Size", maskSizes, STORAGE_KEYS.maskSizes, refreshLookupSelects));

        document.getElementById("toggleSelectAllBtn").addEventListener("click", toggleSelectAllNodes);
        elements.gotoSectionSelect.addEventListener("change", onGotoSectionChanged);
        document.getElementById("protocolViewerOkBtn").addEventListener("click", onOk);
        document.getElementById("protocolViewerCancelBtn").addEventListener("click", onCancel);
    }

    function renderAllSections() {
        (config.documentModel.sections || []).forEach(section => {
            const host = document.querySelector(`[data-section-id="${section.id}"]`);
            if (!host) {
                return;
            }

            host.innerHTML = "";
            flattenNodes(section.children || []).forEach(entry => {
                host.appendChild(buildNodeRow(entry.node, entry.depth));
            });
        });
    }

    function buildNodeRow(node, depth) {
        const row = document.createElement("div");
        row.className = "protocol-node-row";
        row.dataset.nodeId = String(node.id);
        row.style.paddingLeft = `${depth * 24}px`;

        const checkbox = document.createElement("input");
        checkbox.type = "checkbox";
        checkbox.className = "form-check-input protocol-node-checkbox";
        checkbox.id = `protocol-node-${node.id}`;
        checkbox.addEventListener("change", () => onNodeCheckedChanged(node.id, checkbox.checked));

        const label = document.createElement("label");
        label.className = "protocol-node-label";
        label.setAttribute("for", checkbox.id);
        label.textContent = node.text || "(empty)";

        row.appendChild(checkbox);
        row.appendChild(label);

        if ((node.subText || []).length > 0) {
            row.appendChild(buildSubTextSelect(node));
        }

        if (hasNodeLink(node)) {
            row.appendChild(buildNodeLink(node));
        }

        return row;
    }

    function buildSubTextSelect(node) {
        const select = document.createElement("select");
        select.className = "form-select form-select-sm protocol-node-subtext";

        const empty = document.createElement("option");
        empty.value = "";
        empty.textContent = "";
        select.appendChild(empty);

        (node.subText || []).forEach(value => {
            const option = document.createElement("option");
            option.value = value;
            option.textContent = value;
            select.appendChild(option);
        });

        select.addEventListener("change", () => {
            if (!select.value) {
                subTextSelections.delete(node.id);
                return;
            }

            subTextSelections.set(node.id, select.value);
        });

        return select;
    }

    function buildNodeLink(node) {
        const link = document.createElement("button");
        link.type = "button";
        link.className = "btn btn-link p-0 protocol-viewer-link";
        link.textContent = node.linkText;
        link.addEventListener("click", () => gotoLinkedNode(node.linkId));
        return link;
    }

    function onNodeCheckedChanged(nodeId, isChecked) {
        updateCheckedSet(nodeId, isChecked);
        if (isChecked) {
            checkAncestors(nodeId);
        }
    }

    function updateCheckedSet(nodeId, isChecked) {
        if (isChecked) {
            checkedNodeIds.add(nodeId);
            return;
        }

        checkedNodeIds.delete(nodeId);
    }

    function checkAncestors(nodeId) {
        let current = parentLookup.get(nodeId);
        while (current != null) {
            checkedNodeIds.add(current);
            const ancestorCheckbox = document.getElementById(`protocol-node-${current}`);
            if (ancestorCheckbox) {
                ancestorCheckbox.checked = true;
            }

            current = parentLookup.get(current);
        }
    }

    function toggleSelectAllNodes() {
        const checkboxes = document.querySelectorAll(".protocol-node-checkbox");
        checkboxes.forEach(checkbox => {
            checkbox.checked = !checkbox.checked;
            const nodeId = Number(checkbox.id.replace("protocol-node-", ""));
            updateCheckedSet(nodeId, checkbox.checked);
        });
    }

    function onGotoSectionChanged() {
        const tabKey = elements.gotoSectionSelect.value;
        if (!tabKey) {
            return;
        }

        showTab(tabKey);
    }

    function showTab(tabKey) {
        const button = document.querySelector(`[data-tab-key="${tabKey}"]`);
        if (!button || !window.bootstrap) {
            return;
        }

        const tab = window.bootstrap.Tab.getOrCreateInstance(button);
        tab.show();
    }

    function gotoLinkedNode(linkId) {
        const linkedNode = nodeLookup.get(linkId) || findNodeById(config.documentModel.sections, linkId);
        if (!linkedNode) {
            setStatus(`Linked node #${linkId} was not found.`);
            return;
        }

        const sectionId = sectionLookup.get(linkedNode.id);
        showTab(`section-${sectionId}`);
        highlightLinkedNode(linkedNode.id);
    }

    function highlightLinkedNode(nodeId) {
        const nodeRow = document.querySelector(`[data-node-id="${nodeId}"]`);
        if (!nodeRow) {
            return;
        }

        nodeRow.classList.add("protocol-node-highlight");
        nodeRow.scrollIntoView({ behavior: "smooth", block: "center" });
        window.setTimeout(() => nodeRow.classList.remove("protocol-node-highlight"), 2000);
    }

    function refreshLookupSelects() {
        bindSelectOptions(elements.techSelect, techNames);
        bindSelectOptions(elements.maskStyleSelect, maskStyles);
        bindSelectOptions(elements.maskSizeSelect, maskSizes);
    }

    function bindSelectOptions(select, values) {
        const previous = select.value;
        select.innerHTML = "";

        if (!values.length) {
            const option = document.createElement("option");
            option.value = "";
            option.textContent = "";
            select.appendChild(option);
            return;
        }

        values.forEach(value => {
            const option = document.createElement("option");
            option.value = value;
            option.textContent = value;
            select.appendChild(option);
        });

        if (values.includes(previous)) {
            select.value = previous;
        }
    }

    function addLookupValue(label, values, storageKey, refreshCallback) {
        const value = (prompt(`Enter ${label}`) || "").trim();
        if (!value || values.includes(value)) {
            return;
        }

        values.push(value);
        values.sort((a, b) => a.localeCompare(b));
        saveList(storageKey, values);
        refreshCallback();
    }

    function removeLookupValue(label, values, storageKey, refreshCallback) {
        const value = (prompt(`Remove ${label}`) || "").trim();
        if (!value) {
            return;
        }

        const next = values.filter(item => item.toLowerCase() !== value.toLowerCase());
        replaceList(values, next);
        saveList(storageKey, values);
        refreshCallback();
    }

    function clearLookupValues(label, values, storageKey, refreshCallback) {
        if (!confirm(`Clear all ${label} values?`)) {
            return;
        }

        replaceList(values, []);
        saveList(storageKey, values);
        refreshCallback();
    }

    function replaceList(target, source) {
        target.splice(0, target.length);
        source.forEach(value => target.push(value));
    }

    function onOk() {
        const content = composeOutput();
        if (window.parent && window.parent !== window) {
            window.parent.postMessage({
                type: "protocolViewer:done",
                content
            }, "*");
            setStatus("Protocol content sent to editor.");
            return;
        }

        navigator.clipboard.writeText(content)
            .then(() => setStatus("Protocol content copied to clipboard."))
            .catch(() => setStatus("Protocol content ready. Copy from page source if needed."));
    }

    function onCancel() {
        if (window.parent && window.parent !== window) {
            window.parent.postMessage({
                type: "protocolViewer:cancel"
            }, "*");
        }

        setStatus("Cancelled.");
    }

    function composeOutput() {
        const lines = [];
        appendHeader(lines);
        appendCpapInfo(lines);
        appendProtocolItems(lines);
        return lines.join("\n").trim();
    }

    function appendHeader(lines) {
        lines.push(config.documentModel.text || "Protocol");
        lines.push(`Technician: ${elements.techSelect.value || ""}`);
        lines.push(`Date of Study: ${formatStudyDate(elements.studyDateInput.value)}`);
        lines.push(`Patient Name: ${elements.patientNameInput.value || ""}`);
        lines.push("Technician Documentation");
        lines.push("");
    }

    function appendCpapInfo(lines) {
        const cpapLines = [];
        if (document.getElementById("cpapNoUnit").checked) {
            cpapLines.push("Pt does not have a CPAP/BiPAP unit.");
        }
        if (document.getElementById("cpapNoBring").checked) {
            cpapLines.push("Pt has machine but did not bring it.");
        }
        if (document.getElementById("cpapHasMachine").checked) {
            cpapLines.push("Pt has and brought a CPAP/BiPAP.");
        }

        if (elements.maskStyleSelect.value && elements.maskSizeSelect.value) {
            cpapLines.push(`Mask Style: ${elements.maskStyleSelect.value} Mask Size: ${elements.maskSizeSelect.value}.`);
        } else if (elements.maskStyleSelect.value) {
            cpapLines.push(`Mask Style: ${elements.maskStyleSelect.value}.`);
        }

        if (document.getElementById("cpapHeatedHumidity").checked) {
            cpapLines.push("Heated Humidity was used.");
        }
        if (document.getElementById("cpapChinStrap").checked) {
            cpapLines.push("A chin strap was used.");
        }

        lines.push("CPAP/BIPAP Info:");
        if (!cpapLines.length) {
            lines.push("None.");
        } else {
            cpapLines.forEach(line => lines.push(line));
        }

        if (document.getElementById("arrivedOnO2").checked) {
            lines.push("The patient arrived on O2.");
        }
        lines.push("");
    }

    function appendProtocolItems(lines) {
        (config.documentModel.sections || []).forEach(section => {
            const sectionLines = [];
            flattenNodes(section.children || []).forEach(entry => {
                if (!checkedNodeIds.has(entry.node.id)) {
                    return;
                }

                const prefix = " ".repeat(entry.depth * 2);
                sectionLines.push(`${prefix}${buildOutputText(entry.node)}`);
            });

            if (!sectionLines.length) {
                return;
            }

            lines.push(section.text || "Section");
            sectionLines.forEach(line => lines.push(line));
            lines.push("");
        });
    }

    function buildOutputText(node) {
        const parts = [node.text || ""];
        const subText = subTextSelections.get(node.id) || "";
        if (subText) {
            parts.push(subText);
        }
        if (node.linkText) {
            parts.push(node.linkText);
        }

        return parts.filter(part => !!part).join(" ").trim();
    }

    function setStatus(message) {
        elements.statusEl.textContent = message;
    }
}

function resolveElements() {
    return {
        statusEl: document.getElementById("protocolViewerStatus"),
        gotoSectionSelect: document.getElementById("gotoSectionSelect"),
        techSelect: document.getElementById("studyTechName"),
        studyDateInput: document.getElementById("studyDate"),
        patientNameInput: document.getElementById("patientName"),
        maskStyleSelect: document.getElementById("maskStyleSelect"),
        maskSizeSelect: document.getElementById("maskSizeSelect")
    };
}

function loadList(storageKey, fallback) {
    const raw = localStorage.getItem(storageKey);
    if (!raw) {
        return [...fallback];
    }

    try {
        const parsed = JSON.parse(raw);
        if (!Array.isArray(parsed)) {
            return [...fallback];
        }

        return parsed
            .map(value => typeof value === "string" ? value.trim() : "")
            .filter(value => !!value);
    } catch {
        return [...fallback];
    }
}

function saveList(storageKey, values) {
    localStorage.setItem(storageKey, JSON.stringify(values));
}
