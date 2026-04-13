import test from "node:test";
import assert from "node:assert/strict";

import {
    buildNodeRelationshipMaps,
    findNodeById,
    flattenNodes,
    formatStudyDate,
    hasNodeLink,
    toDateInputValue
} from "../protocol-shared-utils.js";

function createSections() {
    return [
        {
            id: 10,
            text: "Diagnostic Polysomnogram",
            children: [
                {
                    id: 11,
                    text: "Monitor SpO2 and EKG",
                    linkId: 22,
                    linkText: "BiPAP Titration",
                    children: [
                        {
                            id: 12,
                            text: "SpO2 drops below 50%",
                            children: []
                        }
                    ]
                },
                {
                    id: 13,
                    text: "Monitor SpO2 for baseline changes below 86%",
                    children: []
                }
            ]
        },
        {
            id: 20,
            text: "BiPAP Titration",
            children: [
                {
                    id: 22,
                    text: "Initiate BiPAP at 8/4 cm H2O",
                    children: []
                }
            ]
        }
    ];
}

test("flattenNodes returns nodes in depth-first order with depths", () => {
    const entries = flattenNodes(createSections()[0].children);

    assert.deepEqual(
        entries.map(entry => [entry.node.id, entry.depth]),
        [
            [11, 0],
            [12, 1],
            [13, 0]
        ]
    );
});

test("buildNodeRelationshipMaps tracks parent and section relationships", () => {
    const { nodeLookup, parentLookup, sectionLookup } = buildNodeRelationshipMaps(createSections());

    assert.equal(nodeLookup.get(10).text, "Diagnostic Polysomnogram");
    assert.equal(nodeLookup.get(12).text, "SpO2 drops below 50%");
    assert.equal(parentLookup.get(12), 11);
    assert.equal(parentLookup.has(11), false);
    assert.equal(sectionLookup.get(12), 10);
    assert.equal(sectionLookup.get(22), 20);
});

test("findNodeById locates nested nodes", () => {
    const found = findNodeById(createSections(), 12);

    assert.ok(found);
    assert.equal(found.text, "SpO2 drops below 50%");
    assert.equal(findNodeById(createSections(), 999), null);
});

test("hasNodeLink only returns true for valid linked nodes", () => {
    const linkedNode = createSections()[0].children[0];

    assert.equal(hasNodeLink(linkedNode), true);
    assert.equal(hasNodeLink({ id: 99, linkId: 0, linkText: "No link" }), false);
    assert.equal(hasNodeLink({ id: 100, linkId: 22, linkText: "   " }), false);
});

test("study date helpers normalize between viewer and output formats", () => {
    assert.equal(toDateInputValue("3/7/2026"), "2026-03-07");
    assert.equal(toDateInputValue("2026-03-27"), "2026-03-27");
    assert.equal(formatStudyDate("2026-03-27"), "3/27/2026");
    assert.equal(formatStudyDate("3/7/2026"), "3/7/2026");
});

test("toDateInputValue rejects impossible calendar dates", () => {
    assert.equal(toDateInputValue("2/31/2026"), "");
    assert.equal(toDateInputValue("4/31/2026"), "");
    assert.equal(toDateInputValue("2/29/2025"), "");
});

test("toDateInputValue accepts valid leap day", () => {
    assert.equal(toDateInputValue("2/29/2028"), "2028-02-29");
});
