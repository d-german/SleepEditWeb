/**
 * Dictation speech module — orchestrates Vosk-Browser for in-browser
 * speech-to-text without cloud dependencies.
 *
 * Usage:
 *   const ctrl = createDictationController({
 *       modelUrl: '/vosk-model/model.tar.gz',
 *       workletUrl: '/js/vosk-recognizer-processor.js',
 *       onResult: text => insertTextAtCursor(text),
 *       onPartial: text => showPartialText(text),
 *       onStatusChange: status => updateUI(status),
 *       onError: msg => showError(msg)
 *   });
 *   await ctrl.loadModel();
 *   ctrl.start();
 */

/* global Vosk */

/**
 * @typedef {'idle'|'loading'|'ready'|'listening'|'error'} DictationState
 */

/**
 * @typedef {Object} DictationOptions
 * @property {string} modelUrl    - Path to vosk model tar.gz
 * @property {string} workletUrl  - Path to AudioWorklet processor JS
 * @property {(text: string) => void} onResult       - Final transcription callback
 * @property {(text: string) => void} [onPartial]    - Partial/interim text callback
 * @property {(status: DictationState) => void} [onStatusChange]
 * @property {(message: string) => void} [onError]
 */

const VALID_TRANSITIONS = {
    idle:      ['loading'],
    loading:   ['ready', 'error'],
    ready:     ['listening', 'loading'],
    listening: ['ready', 'error'],
    error:     ['loading', 'idle']
};

/**
 * @param {DictationOptions} options
 */
function createDictationController(options) {
    const { modelUrl, workletUrl, onResult, onPartial, onStatusChange, onError } = options;

    /** @type {DictationState} */
    let state = 'idle';

    /** @type {any} */
    let model = null;
    /** @type {any} */
    let recognizer = null;
    /** @type {AudioContext|null} */
    let audioContext = null;
    /** @type {MediaStream|null} */
    let mediaStream = null;
    /** @type {AudioWorkletNode|null} */
    let workletNode = null;

    function transition(newState) {
        if (!VALID_TRANSITIONS[state]?.includes(newState)) {
            console.warn(`Dictation: invalid transition ${state} → ${newState}`);
            return false;
        }
        state = newState;
        onStatusChange?.(state);
        return true;
    }

    function handleError(message) {
        console.error(`Dictation error: ${message}`);
        transition('error');
        onError?.(message);
    }

    async function loadModel() {
        if (state !== 'idle' && state !== 'error' && state !== 'ready') {
            return;
        }

        if (model) {
            return;
        }

        if (!transition('loading')) return;

        try {
            if (typeof Vosk === 'undefined' || !Vosk.createModel) {
                throw new Error('Vosk library not loaded. Ensure vosk.js is included before dictation.js.');
            }

            if (typeof Vosk.setLogLevel === 'function') {
                Vosk.setLogLevel(-1);
            }
            model = await Vosk.createModel(modelUrl);

            transition('ready');
        } catch (err) {
            handleError(`Failed to load speech model: ${err.message}`);
        }
    }

    async function start() {
        if (state !== 'ready') {
            if (state === 'idle' || state === 'error') {
                await loadModel();
                if (state !== 'ready') return;
            } else {
                return;
            }
        }

        try {
            mediaStream = await navigator.mediaDevices.getUserMedia({
                audio: {
                    echoCancellation: true,
                    noiseSuppression: true,
                    channelCount: 1
                },
                video: false
            });

            audioContext = new AudioContext();
            const sampleRate = audioContext.sampleRate;

            const channel = new MessageChannel();

            recognizer = new model.KaldiRecognizer(sampleRate);
            recognizer.setWords(false);

            recognizer.on('result', (message) => {
                const text = message?.result?.text;
                if (text) {
                    onResult(text + ' ');
                }
            });

            recognizer.on('partialresult', (message) => {
                const partial = message?.result?.partial;
                if (partial) {
                    onPartial?.(partial);
                }
            });

            await audioContext.audioWorklet.addModule(workletUrl);

            workletNode = new AudioWorkletNode(audioContext, 'vosk-recognizer-processor');
            workletNode.port.postMessage({ type: 'setPort', port: channel.port1 }, [channel.port1]);

            recognizer.connectAudioPort(channel.port2);

            const source = audioContext.createMediaStreamSource(mediaStream);
            source.connect(workletNode);

            transition('listening');
        } catch (err) {
            cleanupAudio();
            if (err.name === 'NotAllowedError') {
                handleError('Microphone access denied. Please allow microphone access in your browser settings.');
            } else if (err.name === 'NotFoundError') {
                handleError('No microphone found. Please connect a microphone.');
            } else {
                handleError(`Failed to start dictation: ${err.message}`);
            }
        }
    }

    function cleanupAudio() {
        if (workletNode) {
            workletNode.disconnect();
            workletNode = null;
        }
        if (audioContext) {
            audioContext.close().catch(() => {});
            audioContext = null;
        }
        if (mediaStream) {
            mediaStream.getTracks().forEach(t => t.stop());
            mediaStream = null;
        }
        if (recognizer) {
            recognizer.remove();
            recognizer = null;
        }
    }

    function stop() {
        if (state !== 'listening') return;
        cleanupAudio();
        transition('ready');
    }

    function isListening() {
        return state === 'listening';
    }

    function getState() {
        return state;
    }

    function dispose() {
        cleanupAudio();
        if (model) {
            model.terminate();
            model = null;
        }
        state = 'idle';
        onStatusChange?.('idle');
    }

    return Object.freeze({
        loadModel,
        start,
        stop,
        isListening,
        getState,
        dispose
    });
}

// Export for both module and script contexts
if (typeof window !== 'undefined') {
    window.createDictationController = createDictationController;
}
