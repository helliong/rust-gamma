$ErrorActionPreference = "Stop"

$version = (Get-Content "version.txt").Trim()
Write-Output "Building release packages for version: v$version"

# 1. Ensure dist directory exists
if (-not (Test-Path "dist")) {
    New-Item -ItemType Directory -Path "dist" | Out-Null
}

# 2. Recompile main executable
Write-Output "Compiling RustGammaPro.exe..."
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /win32icon:app.ico /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll /out:RustGammaPro.exe RustGamma.cs
if ($LASTEXITCODE -ne 0) { throw "Compilation of RustGammaPro.exe failed!" }

# 3. Create payload.zip
Write-Output "Creating payload.zip..."
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$payloadZip = "payload.zip"
if (Test-Path $payloadZip) { Remove-Item $payloadZip -Force }

$filesToPack = @(
    "RustGammaPro.exe",
    "app.ico",
    "icon_clean.png",
    "config.ini",
    "LICENSE",
    "README.md",
    "README_RU.md",
    "version.txt"
)

$zip = [System.IO.Compression.ZipFile]::Open($payloadZip, [System.IO.Compression.ZipArchiveMode]::Create)
foreach ($f in $filesToPack) {
    if (Test-Path $f) {
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Resolve-Path $f).Path, $f) | Out-Null
        Write-Output "  + Added $f"
    } else {
        Write-Warning "File not found: $f"
    }
}
$zip.Dispose()

# 4. Copy payload.zip to Portable zip in dist
$portableZip = "dist\RustGammaPro_v${version}_Portable.zip"
Copy-Item $payloadZip $portableZip -Force
Write-Output "Created $portableZip"

# 5. Compile Installer / Setup
$setupExe = "dist\RustGammaPro_Setup_v${version}.exe"
Write-Output "Compiling $setupExe..."
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /win32icon:app.ico /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.IO.Compression.dll,System.IO.Compression.FileSystem.dll /resource:payload.zip,payload.zip /out:$setupExe Installer.cs
if ($LASTEXITCODE -ne 0) { throw "Compilation of Setup failed!" }
Write-Output "Compiled $setupExe successfully."

# 6. Clean up temporary payload.zip
if (Test-Path $payloadZip) { Remove-Item $payloadZip -Force }

# 7. Generate SHA256 checksums
Write-Output "Generating SHA256 checksums..."
$shaSetup = (Get-FileHash -Algorithm SHA256 $setupExe).Hash.ToLower()
$shaPortable = (Get-FileHash -Algorithm SHA256 $portableZip).Hash.ToLower()

$shaContent = @"
$shaSetup  RustGammaPro_Setup_v${version}.exe
$shaPortable  RustGammaPro_v${version}_Portable.zip
"@

Set-Content -Path "dist\SHA256SUMS.txt" -Value $shaContent -Encoding UTF8
Write-Output "SHA256 checksums written to dist\SHA256SUMS.txt"
Write-Output "Setup:    $shaSetup"
Write-Output "Portable: $shaPortable"
Write-Output "=== BUILD COMPLETE ==="
