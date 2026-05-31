$ErrorActionPreference = "SilentlyContinue"

$bundle = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\DulangLandIndexTool.bundle"
$pathsToRemove = @(
    $bundle,
    (Join-Path $bundle "Contents"),
    (Join-Path $bundle "Contents\Win64")
)

$acad = @(Get-Process acad -ErrorAction SilentlyContinue)
if ($acad.Count -gt 0) {
    Write-Host "AutoCAD is running. Close all AutoCAD/T20 windows before uninstalling."
    exit 2
}

if (Test-Path $bundle) {
    Remove-Item -LiteralPath $bundle -Recurse -Force
    Write-Host "Removed bundle folder: $bundle"
}

$root = "HKCU:\Software\Autodesk\AutoCAD"
if (Test-Path $root) {
    $updated = 0
    Get-ChildItem $root -Recurse |
        Where-Object { $_.PSChildName -eq "Variables" } |
        ForEach-Object {
            $current = [string]$_.GetValue("TRUSTEDPATHS", "")
            if ([string]::IsNullOrWhiteSpace($current)) {
                return
            }

            $items = New-Object System.Collections.Generic.List[string]
            $changed = $false
            foreach ($item in ($current -split ";")) {
                $value = $item.Trim()
                if ([string]::IsNullOrWhiteSpace($value)) {
                    continue
                }

                $remove = $false
                foreach ($path in $pathsToRemove) {
                    if ($value.TrimEnd([char]92) -ieq $path.TrimEnd([char]92)) {
                        $remove = $true
                        $changed = $true
                        break
                    }
                }

                if (-not $remove) {
                    $items.Add($value)
                }
            }

            if ($changed) {
                Set-ItemProperty -LiteralPath $_.PSPath -Name "TRUSTEDPATHS" -Value ($items -join ";")
                $updated++
            }
        }

    Write-Host "Cleaned AutoCAD TRUSTEDPATHS profiles: $updated"
}

exit 0
