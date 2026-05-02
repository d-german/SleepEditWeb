<#
.SYNOPSIS
    Downloads and prepares the Vosk speech model for local development.
.DESCRIPTION
    Downloads vosk-model-small-en-us-0.15 from alphacephei.com, extracts it,
    repacks as tar.gz (required by vosk-browser), and places it in wwwroot/vosk-model/.
#>

$ErrorActionPreference = 'Stop'

$modelUrl = 'https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip'
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputDir = Join-Path $projectRoot 'SleepEditWeb\wwwroot\vosk-model'
$outputFile = Join-Path $outputDir 'model.tar.gz'

if (Test-Path $outputFile) {
    Write-Host "Model already exists at $outputFile" -ForegroundColor Yellow
    exit 0
}

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) 'vosk-model-download'
$zipPath = Join-Path $tempDir 'vosk-model.zip'

try {
    New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

    Write-Host 'Downloading vosk-model-small-en-us-0.15...' -ForegroundColor Cyan
    Invoke-WebRequest -Uri $modelUrl -OutFile $zipPath -UseBasicParsing

    Write-Host 'Extracting...' -ForegroundColor Cyan
    Expand-Archive -Path $zipPath -DestinationPath $tempDir -Force

    $modelDir = Join-Path $tempDir 'vosk-model-small-en-us-0.15'

    Write-Host 'Repacking as tar.gz...' -ForegroundColor Cyan
    tar -czf $outputFile -C $modelDir .

    Write-Host "Model saved to $outputFile" -ForegroundColor Green
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -Recurse -Force $tempDir
    }
}
