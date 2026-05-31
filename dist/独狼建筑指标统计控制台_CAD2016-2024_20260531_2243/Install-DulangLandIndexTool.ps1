param(
    [switch]$TrustOnly
)

$ErrorActionPreference = "Stop"

function Add-TrustedPaths {
    param([string]$BundlePath)

    $applicationPlugins = Split-Path -Parent $BundlePath
    $pathsToAdd = @(
        $applicationPlugins,
        $BundlePath,
        (Join-Path $BundlePath "Contents"),
        (Join-Path $BundlePath "Contents\Win64")
    )

    $root = "HKCU:\Software\Autodesk\AutoCAD"
    if (-not (Test-Path $root)) {
        Write-Host "AutoCAD user profiles were not found. If loading fails, add the bundle folder to TRUSTEDPATHS manually."
        return
    }

    $updated = 0
    Get-ChildItem $root -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.PSChildName -eq "Variables" } |
        ForEach-Object {
            $key = $_
            $current = [string]$key.GetValue("TRUSTEDPATHS", "")
            $items = New-Object System.Collections.Generic.List[string]

            foreach ($item in ($current -split ";")) {
                if (-not [string]::IsNullOrWhiteSpace($item)) {
                    $items.Add($item.Trim())
                }
            }

            $changed = $false
            foreach ($path in $pathsToAdd) {
                $exists = $false
                foreach ($item in $items) {
                    if ($item.TrimEnd([char]92) -ieq $path.TrimEnd([char]92)) {
                        $exists = $true
                        break
                    }
                }

                if (-not $exists) {
                    $items.Add($path)
                    $changed = $true
                }
            }

            if ($changed) {
                Set-ItemProperty -LiteralPath $key.PSPath -Name "TRUSTEDPATHS" -Value ($items -join ";")
                $updated++
            }
        }

    Write-Host "Updated AutoCAD TRUSTEDPATHS profiles: $updated"
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $scriptRoot "DulangLandIndexTool.bundle"
$dst = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\DulangLandIndexTool.bundle"

if ($TrustOnly) {
    Add-TrustedPaths -BundlePath $dst
    exit 0
}

$acad = @(Get-Process acad -ErrorAction SilentlyContinue)
if ($acad.Count -gt 0) {
    Write-Host "AutoCAD is running. Close all AutoCAD/T20 windows before installing."
    exit 2
}

if (-not (Test-Path $src)) {
    Write-Host "Bundle folder was not found: $src"
    exit 1
}

$package = Join-Path $src "PackageContents.xml"
$dll = Join-Path $src "Contents\Win64\LandIndexTool.dll"
if (-not (Test-Path $package) -or -not (Test-Path $dll)) {
    Write-Host "The bundle is incomplete. Extract the package again."
    exit 1
}

try {
    [xml]$null = Get-Content -LiteralPath $package -Raw -Encoding UTF8
} catch {
    Write-Host "PackageContents.xml is invalid: $($_.Exception.Message)"
    exit 1
}

$parent = Split-Path -Parent $dst
New-Item -ItemType Directory -Force -Path $parent | Out-Null

if (Test-Path $dst) {
    Remove-Item -LiteralPath $dst -Recurse -Force
}

Copy-Item -LiteralPath $src -Destination $dst -Recurse -Force

Get-ChildItem -LiteralPath $dst -Recurse -File -ErrorAction SilentlyContinue |
    ForEach-Object {
        try { Unblock-File -LiteralPath $_.FullName -ErrorAction SilentlyContinue } catch { }
    }

Add-TrustedPaths -BundlePath $dst

Write-Host "Installed to: $dst"
Write-Host "Command: dulang1"
exit 0
