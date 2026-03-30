export function buildNodeRelationshipMaps(sections) {
    const nodeLookup = new Map();
    const parentLookup = new Map();
    const sectionLookup = new Map();

    for (const section of sections || []) {
        if (!section || typeof section.id !== "number") {
            continue;
        }

        nodeLookup.set(section.id, section);
        sectionLookup.set(section.id, section.id);
        walkNodes(section.children || [], section.id, null, nodeLookup, parentLookup, sectionLookup);
    }

    return { nodeLookup, parentLookup, sectionLookup };
}

export function findNodeById(nodes, nodeId) {
    for (const node of nodes || []) {
        if (!node) {
            continue;
        }

        if (node.id === nodeId) {
            return node;
        }

        const found = findNodeById(node.children || [], nodeId);
        if (found) {
            return found;
        }
    }

    return null;
}

export function flattenNodes(nodes, depth = 0) {
    const entries = [];

    for (const node of nodes || []) {
        if (!node) {
            continue;
        }

        entries.push({ node, depth });
        entries.push(...flattenNodes(node.children || [], depth + 1));
    }

    return entries;
}

export function hasNodeLink(node) {
    return Number.isInteger(node?.linkId)
        && node.linkId > 0
        && typeof node.linkText === "string"
        && node.linkText.trim().length > 0;
}

export function toDateInputValue(value) {
    if (typeof value !== "string" || value.trim().length === 0) {
        return "";
    }

    const trimmed = value.trim();
    if (/^\d{4}-\d{2}-\d{2}$/.test(trimmed)) {
        return trimmed;
    }

    const parsed = parseUsDate(trimmed);
    if (!parsed) {
        return "";
    }

    return `${parsed.year}-${pad(parsed.month)}-${pad(parsed.day)}`;
}

export function formatStudyDate(value) {
    if (typeof value !== "string" || value.trim().length === 0) {
        return "";
    }

    const trimmed = value.trim();
    if (/^\d{4}-\d{2}-\d{2}$/.test(trimmed)) {
        const [year, month, day] = trimmed.split("-").map(Number);
        return `${month}/${day}/${year}`;
    }

    const parsed = parseUsDate(trimmed);
    if (!parsed) {
        return trimmed;
    }

    return `${parsed.month}/${parsed.day}/${parsed.year}`;
}

function walkNodes(nodes, sectionId, parentId, nodeLookup, parentLookup, sectionLookup) {
    for (const node of nodes || []) {
        if (!node || typeof node.id !== "number") {
            continue;
        }

        nodeLookup.set(node.id, node);
        sectionLookup.set(node.id, sectionId);

        if (parentId != null) {
            parentLookup.set(node.id, parentId);
        }

        walkNodes(node.children || [], sectionId, node.id, nodeLookup, parentLookup, sectionLookup);
    }
}

function parseUsDate(value) {
    const match = /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/.exec(value);
    if (!match) {
        return null;
    }

    const month = Number(match[1]);
    const day = Number(match[2]);
    const year = Number(match[3]);
    if (!Number.isInteger(month) || !Number.isInteger(day) || !Number.isInteger(year)) {
        return null;
    }

    if (month < 1 || month > 12 || day < 1 || day > 31) {
        return null;
    }

    return { month, day, year };
}

function pad(value) {
    return String(value).padStart(2, "0");
}
