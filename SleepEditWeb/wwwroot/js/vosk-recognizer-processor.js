/**
 * AudioWorklet processor that bridges the audio thread to the Vosk Web Worker
 * via a MessageChannel. Converts Float32 audio samples to Int16 PCM and
 * forwards them through the channel port.
 *
 * Usage: Register this processor in an AudioWorkletNode and pass the Vosk
 * channel port via the node's port.
 */
class VoskRecognizerProcessor extends AudioWorkletProcessor {
    /**
     * Called by the audio rendering thread for each block of 128 frames.
     * Posts raw Float32 audio back to the main thread for forwarding to Vosk.
     * @param {Float32Array[][]} inputs
     * @returns {boolean}
     */
    process(inputs) {
        const input = inputs[0];
        if (input.length > 0 && input[0].length > 0) {
            const copy = new Float32Array(input[0]);
            this.port.postMessage(
                { type: 'audioData', data: copy.buffer },
                [copy.buffer]
            );
        }
        return true;
    }
}

registerProcessor('vosk-recognizer-processor', VoskRecognizerProcessor);
