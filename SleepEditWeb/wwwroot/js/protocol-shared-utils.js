export function normalizeNodeKind(kind) {
    if (typeof kind === "string") {
        return kind.toLowerCase();
    }

    if (typeof kind === "number") {
        if (kind === 1) {
            return "section";
        }

        if (kind === 2) {
            return "subsection";
        }

        return "root";
    }

    return "";
}

export function findNodeById(nodes, id) {
    for (const node of nodes || []) {
        if (node.id === id) {
            return node;
        }

        const childFound = findNodeById(node.children || [], id);
        if (childFound) {
            return childFound;
        }
    }

    return null;
}

export function isDescendantNode(nodes, targetId) {
    if (!Number.isFinite(Number(targetId))) {
        return false;
    }

    for (const node of nodes || []) {
        if (node.id === targetId || isDescendantNode(node.children || [], targetId)) {
            return true;
        }
    }

    return false;
}

export function flattenNodes(nodes, depth = 0, items = []) {
    (nodes || []).forEach(node => {
        items.push({ node, depth });
        flattenNodes(node.children || [], depth + 1, items);
    });

    return items;
}

export function hasNodeLink(node) {
    return Number(node?.linkId) > 0 && !!node?.linkText;
}

export function buildNodeRelationshipMaps(sections) {
    const nodeLookup = new Map();
    const parentLookup = new Map();
    const sectionLookup = new Map();

    const walk = (nodes, parentId, sectionId) => {
        (nodes || []).forEach(node => {
            const effectiveSectionId = sectionId ?? node.id;
            nodeLookup.set(node.id, node);
            parentLookup.set(node.id, parentId);
            sectionLookup.set(node.id, effectiveSectionId);
            walk(node.children || [], node.id, effectiveSectionId);
        });
    };

    walk(sections || [], null, null);

    return {
        nodeLookup,
        parentLookup,
        sectionLookup
    };
}

export function toDateInputValue(rawValue) {
    const parsed = new Date(rawValue);
    if (Number.isNaN(parsed.getTime())) {
        return new Date().toISOString().substring(0, 10);
    }

    return parsed.toISOString().substring(0, 10);
}

export function formatStudyDate(value) {
    if (!value) {
        return "";
    }

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
        return value;
    }

    return `${parsed.getMonth() + 1}/${parsed.getDate()}/${parsed.getFullYear()}`;
}
