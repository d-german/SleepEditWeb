/**
 * AudioWorklet processor that bridges the audio thread to the Vosk Web Worker
 * via a MessageChannel. Converts Float32 audio samples to Int16 PCM and
 * forwards them through the channel port.
 *
 * Usage: Register this processor in an AudioWorkletNode and pass the Vosk
 * channel port via the node's port.
 */
class VoskRecognizerProcessor extends AudioWorkletProcessor {
    constructor() {
        super();
        /** @type {MessagePort|null} */
        this._voskPort = null;
        this.port.onmessage = (event) => {
            if (event.data.type === 'setPort') {
                this._voskPort = event.data.port;
            }
        };
    }

    /**
     * Convert Float32 audio data to Int16 PCM.
     * @param {Float32Array} float32Array
     * @returns {Int16Array}
     */
    static toInt16(float32Array) {
        const int16 = new Int16Array(float32Array.length);
        for (let i = 0; i < float32Array.length; i++) {
            const s = Math.max(-1, Math.min(1, float32Array[i]));
            int16[i] = s < 0 ? s * 0x8000 : s * 0x7FFF;
        }
        return int16;
    }

    /**
     * Called by the audio rendering thread for each block of 128 frames.
     * @param {Float32Array[][]} inputs
     * @returns {boolean}
     */
    process(inputs) {
        const input = inputs[0];
        if (input.length > 0 && this._voskPort) {
            const pcm = VoskRecognizerProcessor.toInt16(input[0]);
            this._voskPort.postMessage(
                { type: 'audioData', data: pcm.buffer },
                [pcm.buffer]
            );
        }
        return true;
    }
}

registerProcessor('vosk-recognizer-processor', VoskRecognizerProcessor);
