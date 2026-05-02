# Vosk Speech Model

This directory holds the Vosk speech recognition model used for browser-based dictation.

## Model: vosk-model-small-en-us-0.15

- **Size:** ~40 MB (tar.gz)
- **Language:** English (US)
- **License:** Apache 2.0
- **Source:** https://alphacephei.com/vosk/models

## Docker Build

The Dockerfile automatically downloads and bakes the model into the image at build time.
No manual steps needed for deployment.

## Local Development

To download the model for local development:

```powershell
.\scripts\download-vosk-model.ps1
```

Or manually:

1. Download: https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip
2. Extract the zip
3. Re-pack the contents as tar.gz: `tar -czf model.tar.gz -C vosk-model-small-en-us-0.15 .`
4. Place `model.tar.gz` in this directory

## Note

The `*.tar.gz` and `*.zip` files in this directory are excluded from git via `.gitignore`.
