# Captura logs do Android para debug do Walls (sem spam do sistema).
#
# Uso (PowerShell na pasta Walls-main):
#   .\DebugAndroidLogcat.ps1
#   .\DebugAndroidLogcat.ps1 -VerboseUnity   # mais linhas Unity (Debug)
#   .\DebugAndroidLogcat.ps1 -AllErrors      # todos os niveis Error no log
#
# Nota: avisos ActivityManager/cgroup no MIUI sao do Android, nao do teu jogo.

param(
    [switch]$AllErrors,
    [switch]$VerboseUnity
)

$ErrorActionPreference = "Continue"

function Find-Adb {
    $p = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
    if (Test-Path -LiteralPath $p) { return $p }

    $unityRoot = "C:\Program Files\Unity\Hub\Editor"
    if (-not (Test-Path $unityRoot)) { return $null }

    $found = Get-ChildItem -Path $unityRoot -Recurse -Filter "adb.exe" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like "*AndroidPlayer*SDK*platform-tools*adb.exe" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($found) { return $found.FullName }
    return $null
}

function Test-AdbDevice {
    param([string]$AdbPath)
    $raw = & $AdbPath devices 2>&1 | Out-String
    $lines = $raw -split "`r?`n"
    foreach ($line in $lines) {
        if ($line -match "^\s*(\S+)\s+device\s*$") {
            return $true
        }
    }
    return $false
}

$adb = Find-Adb
if (-not $adb) {
    Write-Host "adb.exe nao encontrado." -ForegroundColor Red
    Write-Host "Instala Android SDK Platform-Tools ou Unity com Android Build Support." -ForegroundColor Yellow
    exit 1
}

Write-Host "adb: $adb" -ForegroundColor Cyan
& $adb devices
Write-Host ""

if (-not (Test-AdbDevice -AdbPath $adb)) {
    Write-Host "Nenhum aparelho em modo 'device'. Verifica cabo USB e autorizacao de depuracao." -ForegroundColor Red
    exit 1
}

Write-Host "Abre o app Walls no telemovel. Logs abaixo (Ctrl+C para parar)." -ForegroundColor Green
Write-Host "Procura: FATAL EXCEPTION, AndroidRuntime, Unity, signal, tombstone." -ForegroundColor Green
Write-Host ("-" * 60)

$null = & $adb logcat -c 2>&1

if ($AllErrors) {
    & $adb logcat -v time "*:E"
} elseif ($VerboseUnity) {
    # Unity em Debug + erros nativos/Java
    & $adb logcat -v time "*:S" "Unity:D" "AndroidRuntime:E" "libc:E" "DEBUG:E" "System.err:W" "CRASH:E"
} else {
    # Predefinido: sem ActivityManager (evita ruido MIUI/cgroup)
    & $adb logcat -v time "*:S" "Unity:I" "AndroidRuntime:E" "libc:E" "DEBUG:E" "System.err:W" "CRASH:E"
}
