#!/bin/sh
set -e

MODEL_DIR="/app/wwwroot/vosk-model"
MODEL_FILE="$MODEL_DIR/model.tar.gz"
MODEL_URL="https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip"

if [ ! -f "$MODEL_FILE" ]; then
    echo "Vosk model not found. Downloading..."
    mkdir -p "$MODEL_DIR"
    curl --fail -L "$MODEL_URL" -o /tmp/vosk-model.zip
    unzip -q /tmp/vosk-model.zip -d /tmp/vosk-model
    tar -czf "$MODEL_FILE" -C /tmp/vosk-model/vosk-model-small-en-us-0.15 .
    rm -rf /tmp/vosk-model /tmp/vosk-model.zip
    echo "Vosk model downloaded successfully ($(du -h "$MODEL_FILE" | cut -f1))."
fi

exec dotnet SleepEditWeb.dll
