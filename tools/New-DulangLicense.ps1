param(
    [Parameter(Mandatory = $true)]
    [string]$Customer,

    [Parameter(Mandatory = $true)]
    [string]$Machine,

    [string]$Expires = "NEVER",

    [string]$Edition = "Standard",

    [string]$Output = ".\DulangLicense.lic"
)

$ErrorActionPreference = "Stop"

function Normalize-Machine([string]$value) {
    if ($null -eq $value) {
        $value = ""
    }

    $value = $value.Trim().ToUpperInvariant()
    if ($value -eq "ANY") {
        return $value
    }

    return $value.Replace("-", "").Replace(" ", "")
}

function Get-Signature([string]$customer, [string]$machine, [string]$expires, [string]$edition) {
    $secret = [Convert]::FromBase64String("RHVsYW5nTGFuZEluZGV4VG9vbC0yMDI2LUxpY2Vuc2UtS2V5LUIyN0Q5RjQz")
    $canonical = "Customer=$($customer.Trim())`nMachine=$(Normalize-Machine $machine)`nExpires=$($expires.Trim().ToUpperInvariant())`nEdition=$($edition.Trim())"
    $hmac = [System.Security.Cryptography.HMACSHA256]::new($secret)
    try {
        return [Convert]::ToBase64String($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($canonical)))
    } finally {
        $hmac.Dispose()
    }
}

if ([string]::IsNullOrWhiteSpace($Customer)) {
    throw "Customer cannot be empty."
}

if ([string]::IsNullOrWhiteSpace($Machine)) {
    throw "Machine cannot be empty. Use DULANGID in CAD to get the machine code, or use ANY."
}

$Expires = $Expires.Trim().ToUpperInvariant()
if ($Expires -ne "NEVER" -and $Expires -ne "PERPETUAL") {
    [datetime]::ParseExact($Expires, "yyyy-MM-dd", [Globalization.CultureInfo]::InvariantCulture) | Out-Null
}

$signature = Get-Signature -customer $Customer -machine $Machine -expires $Expires -edition $Edition
$content = @(
    "# Dulang Land Index Tool License"
    "Customer=$($Customer.Trim())"
    "Machine=$(Normalize-Machine $Machine)"
    "Expires=$Expires"
    "Edition=$($Edition.Trim())"
    "Signature=$signature"
) -join "`r`n"

$parent = Split-Path -Parent $Output
if (-not [string]::IsNullOrWhiteSpace($parent)) {
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
}

$fullPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllText($fullPath, $content + "`r`n", [Text.Encoding]::UTF8)
Write-Host "License created: $fullPath"
