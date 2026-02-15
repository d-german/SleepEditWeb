import { readdirSync, readFileSync, statSync } from "node:fs";
import { basename, join, relative, resolve } from "node:path";

const root = resolve(process.cwd());
const jsRoot = join(root, "wwwroot", "js");
const viewsRoot = join(root, "Views");

const protocolFiles = walk(jsRoot)
    .filter(file => file.endsWith(".js"))
    .filter(file => file.includes(`${join("wwwroot", "js", "protocol-")}`));

const errors = [];

const allowedImports = new Map([
    ["protocol-editor-api.js", new Set()],
    ["protocol-editor-store.js", new Set()],
    ["protocol-shared-utils.js", new Set()],
    ["protocol-tree.js", new Set(["protocol-shared-utils.js"])],
    ["protocol-drag-drop.js", new Set(["protocol-shared-utils.js"])],
    ["protocol-link-picker.js", new Set(["protocol-shared-utils.js", "protocol-tree.js"])],
    ["protocol-editor-ui.js", new Set([
        "protocol-editor-api.js",
        "protocol-editor-store.js",
        "protocol-shared-utils.js",
        "protocol-tree.js",
        "protocol-drag-drop.js",
        "protocol-link-picker.js"
    ])],
    ["protocol-viewer.js", new Set(["protocol-shared-utils.js"])],
    ["protocol-viewer-bootstrap.js", new Set(["protocol-viewer.js"])]
]);

for (const file of protocolFiles) {
    const name = basename(file);
    if (!/^protocol-[a-z0-9-]+\.js$/.test(name)) {
        errors.push(`Invalid protocol module naming: ${relative(root, file)}`);
    }

    const source = readFileSync(file, "utf8");
    const importTargets = [...source.matchAll(/from\s+["'](\.\/[^"']+)["']/g)]
        .map(match => match[1].replace("./", ""));

    const allowed = allowedImports.get(name);
    if (!allowed) {
        errors.push(`No dependency rule defined for ${name}. Add it to scripts/frontend-lint.mjs.`);
        continue;
    }

    for (const target of importTargets) {
        if (!allowed.has(target)) {
            errors.push(`Dependency boundary violation: ${name} -> ${target}`);
        }
    }
}

const testFiles = walk(join(jsRoot, "__tests__"))
    .filter(file => file.endsWith(".mjs"));
for (const file of testFiles) {
    const name = basename(file);
    if (!name.endsWith(".test.mjs")) {
        errors.push(`Test file naming violation: ${relative(root, file)} must end with .test.mjs`);
    }
}

assertContains(
    join(viewsRoot, "ProtocolEditor", "Index.cshtml"),
    "<script type=\"module\">",
    "ProtocolEditor view must use module script bootstrap."
);
assertContains(
    join(viewsRoot, "ProtocolEditor", "Index.cshtml"),
    "/js/protocol-editor-ui.js",
    "ProtocolEditor view must import protocol-editor-ui.js."
);
assertContains(
    join(viewsRoot, "ProtocolViewer", "Index.cshtml"),
    "<script type=\"module\">",
    "ProtocolViewer view must use module script bootstrap."
);
assertContains(
    join(viewsRoot, "ProtocolViewer", "Index.cshtml"),
    "/js/protocol-viewer-bootstrap.js",
    "ProtocolViewer view must import protocol-viewer-bootstrap.js."
);

if (errors.length > 0) {
    console.error("Frontend guardrails failed:");
    for (const error of errors) {
        console.error(`- ${error}`);
    }
    process.exit(1);
}

console.log("Frontend guardrails passed.");

function assertContains(file, expected, message) {
    const content = readFileSync(file, "utf8");
    if (!content.includes(expected)) {
        errors.push(message);
    }
}

function walk(directory) {
    if (!statExists(directory)) {
        return [];
    }

    const results = [];
    for (const entry of readdirSync(directory)) {
        const fullPath = join(directory, entry);
        const stats = statSync(fullPath);
        if (stats.isDirectory()) {
            results.push(...walk(fullPath));
            continue;
        }

        results.push(fullPath);
    }

    return results;
}

function statExists(path) {
    try {
        statSync(path);
        return true;
    } catch {
        return false;
    }
}
