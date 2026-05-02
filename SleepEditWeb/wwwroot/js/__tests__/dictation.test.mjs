import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import vm from "node:vm";

const __dirname = dirname(fileURLToPath(import.meta.url));
const dictationSrc = readFileSync(join(__dirname, "..", "dictation.js"), "utf-8");

function loadDictation(globals = {}) {
    const context = {
        console,
        setTimeout,
        clearTimeout,
        window: {},
        navigator: { mediaDevices: { getUserMedia: async () => ({}) } },
        ...globals,
    };
    context.window = context;
    vm.runInNewContext(dictationSrc, context);
    return context.createDictationController;
}

test("createDictationController returns object with expected methods", () => {
    const create = loadDictation({ Vosk: {} });
    const ctrl = create({
        modelUrl: "/model.tar.gz",
        workletUrl: "/processor.js",
        onResult: () => {},
    });

    assert.equal(typeof ctrl.loadModel, "function");
    assert.equal(typeof ctrl.start, "function");
    assert.equal(typeof ctrl.stop, "function");
    assert.equal(typeof ctrl.isListening, "function");
    assert.equal(typeof ctrl.getState, "function");
    assert.equal(typeof ctrl.dispose, "function");
});

test("controller is frozen (immutable API surface)", () => {
    const create = loadDictation({ Vosk: {} });
    const ctrl = create({
        modelUrl: "/m",
        workletUrl: "/w",
        onResult: () => {},
    });
    assert.ok(Object.isFrozen(ctrl));
});

test("initial state is idle", () => {
    const create = loadDictation({ Vosk: {} });
    const ctrl = create({
        modelUrl: "/m",
        workletUrl: "/w",
        onResult: () => {},
    });
    assert.equal(ctrl.getState(), "idle");
    assert.equal(ctrl.isListening(), false);
});

test("loadModel transitions to loading then ready on success", async () => {
    const states = [];
    const fakeModel = {
        KaldiRecognizer: class {},
        terminate: () => {},
    };
    const create = loadDictation({
        Vosk: {
            setLogLevel: () => {},
            createModel: async () => fakeModel,
        },
    });
    const ctrl = create({
        modelUrl: "/m",
        workletUrl: "/w",
        onResult: () => {},
        onStatusChange: (s) => states.push(s),
    });

    await ctrl.loadModel();

    assert.deepEqual(states, ["loading", "ready"]);
    assert.equal(ctrl.getState(), "ready");
});

test("loadModel transitions to error on failure", async () => {
    const states = [];
    let errorMsg = null;
    const create = loadDictation({
        Vosk: {
            setLogLevel: () => {},
            createModel: async () => { throw new Error("network fail"); },
        },
    });
    const ctrl = create({
        modelUrl: "/m",
        workletUrl: "/w",
        onResult: () => {},
        onStatusChange: (s) => states.push(s),
        onError: (msg) => { errorMsg = msg; },
    });

    await ctrl.loadModel();

    assert.deepEqual(states, ["loading", "error"]);
    assert.equal(ctrl.getState(), "error");
    assert.ok(errorMsg.includes("network fail"));
});

test("stop() is a no-op when not listening", () => {
    const create = loadDictation({ Vosk: {} });
    const ctrl = create({
        modelUrl: "/m",
        workletUrl: "/w",
        onResult: () => {},
    });

    // Should not throw
    ctrl.stop();
    assert.equal(ctrl.getState(), "idle");
});

test("dispose() resets state to idle", async () => {
    const states = [];
    const fakeModel = {
        KaldiRecognizer: class {},
        terminate: () => {},
    };
    const create = loadDictation({
        Vosk: {
            setLogLevel: () => {},
            createModel: async () => fakeModel,
        },
    });
    const ctrl = create({
        modelUrl: "/m",
        workletUrl: "/w",
        onResult: () => {},
        onStatusChange: (s) => states.push(s),
    });

    await ctrl.loadModel();
    ctrl.dispose();

    assert.equal(ctrl.getState(), "idle");
    assert.ok(states.includes("idle"));
});

test("loadModel called twice does not reload", async () => {
    let loadCount = 0;
    const fakeModel = {
        KaldiRecognizer: class {},
        terminate: () => {},
    };
    const create = loadDictation({
        Vosk: {
            setLogLevel: () => {},
            createModel: async () => { loadCount++; return fakeModel; },
        },
    });
    const ctrl = create({
        modelUrl: "/m",
        workletUrl: "/w",
        onResult: () => {},
    });

    await ctrl.loadModel();
    await ctrl.loadModel();

    assert.equal(loadCount, 1);
});

test("loadModel fails when Vosk global is not defined", async () => {
    let errorMsg = null;
    const create = loadDictation({});
    const ctrl = create({
        modelUrl: "/m",
        workletUrl: "/w",
        onResult: () => {},
        onError: (msg) => { errorMsg = msg; },
    });

    await ctrl.loadModel();

    assert.equal(ctrl.getState(), "error");
    assert.ok(errorMsg.includes("Vosk library not loaded"));
});
