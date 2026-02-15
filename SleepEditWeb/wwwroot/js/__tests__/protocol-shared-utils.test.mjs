import test from "node:test";
import assert from "node:assert/strict";

import {
    buildNodeRelationshipMaps,
    findNodeById,
    flattenNodes,
    formatStudyDate,
    hasNodeLink,
    isDescendantNode,
    normalizeNodeKind,
    toDateInputValue
} from "../protocol-shared-utils.js";

const sampleTree = [
    {
        id: 1,
        text: "Section 1",
        kind: "Section",
        children: [
            {
                id: 2,
                text: "Child 1",
                kind: 2,
                children: []
            },
            {
                id: 3,
                text: "Child 2",
                kind: 2,
                children: [
                    {
                        id: 4,
                        text: "Nested",
                        kind: 2,
                        children: []
                    }
                ]
            }
        ]
    }
];

test("normalizeNodeKind handles string and numeric values", () => {
    assert.equal(normalizeNodeKind("Section"), "section");
    assert.equal(normalizeNodeKind(1), "section");
    assert.equal(normalizeNodeKind(2), "subsection");
    assert.equal(normalizeNodeKind(99), "root");
    assert.equal(normalizeNodeKind(null), "");
});

test("findNodeById returns nested matches", () => {
    const found = findNodeById(sampleTree, 4);
    assert.equal(found?.text, "Nested");
    assert.equal(findNodeById(sampleTree, 999), null);
});

test("flattenNodes returns depth-aware traversal", () => {
    const flattened = flattenNodes(sampleTree);
    assert.deepEqual(
        flattened.map(entry => [entry.node.id, entry.depth]),
        [
            [1, 0],
            [2, 1],
            [3, 1],
            [4, 2]
        ]
    );
});

test("isDescendantNode detects descendants", () => {
    assert.equal(isDescendantNode(sampleTree, 4), true);
    assert.equal(isDescendantNode(sampleTree, 8), false);
});

test("buildNodeRelationshipMaps builds lookup maps", () => {
    const maps = buildNodeRelationshipMaps(sampleTree);
    assert.equal(maps.nodeLookup.get(4)?.text, "Nested");
    assert.equal(maps.parentLookup.get(4), 3);
    assert.equal(maps.sectionLookup.get(4), 1);
});

test("hasNodeLink requires link id and text", () => {
    assert.equal(hasNodeLink({ linkId: 3, linkText: "Go" }), true);
    assert.equal(hasNodeLink({ linkId: 0, linkText: "Go" }), false);
    assert.equal(hasNodeLink({ linkId: 3, linkText: "" }), false);
});

test("date formatting helpers produce expected output", () => {
    assert.equal(formatStudyDate("2026-02-15T12:00:00"), "2/15/2026");
    assert.equal(formatStudyDate(""), "");
    assert.match(toDateInputValue("invalid"), /^\d{4}-\d{2}-\d{2}$/);
    assert.equal(toDateInputValue("2026-01-02T12:00:00.000Z"), "2026-01-02");
});
