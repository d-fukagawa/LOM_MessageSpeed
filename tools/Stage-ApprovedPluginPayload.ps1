param(
    [Parameter(Mandatory = $true)][string] $ZipPath,
    [Parameter(Mandatory = $true)][string] $OutputPath,
    [Parameter(Mandatory = $true)][string] $ExpectedZipSha256,
    [Parameter(Mandatory = $true)][string] $ExpectedDllSha256,
    [Parameter(Mandatory = $true)][long] $ExpectedDllLength,
    [Parameter(Mandatory = $true)][string] $ExpectedAssemblyVersion,
    [Parameter(Mandatory = $true)][string] $ExpectedFileVersion,
    [Parameter(Mandatory = $true)][string] $ExpectedProductVersionPrefix
)

$ErrorActionPreference = 'Stop'
$sha256 = [System.Security.Cryptography.SHA256]::Create()
function Get-Sha256([string] $Path) {
    $inputStream = [System.IO.File]::OpenRead($Path)
    try { return [System.BitConverter]::ToString($sha256.ComputeHash($inputStream)).Replace('-', '') }
    finally { $inputStream.Dispose() }
}
$resolvedZip = (Resolve-Path -LiteralPath $ZipPath).Path
$actualZipHash = Get-Sha256 $resolvedZip
if ($actualZipHash -ne $ExpectedZipSha256) {
    throw "Approved ZIP SHA-256 mismatch. Actual: $actualZipHash"
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($resolvedZip)
try {
    $dllEntries = @()
    foreach ($entry in $archive.Entries) {
        $name = $entry.FullName
        if ([string]::IsNullOrWhiteSpace($name) -or
            [System.IO.Path]::IsPathRooted($name) -or
            $name.Contains('\') -or
            $name.Split('/') -contains '..') {
            throw "Unsafe ZIP entry: $name"
        }

        if ($name -eq 'LOM_MessageSpeed.dll') {
            $dllEntries += $entry
        }
        elseif ([System.IO.Path]::GetExtension($name) -in @('.dll', '.exe', '.com', '.bat', '.cmd', '.ps1')) {
            throw "Unexpected executable payload: $name"
        }
    }

    if ($dllEntries.Count -ne 1) {
        throw "Expected exactly one LOM_MessageSpeed.dll entry; found $($dllEntries.Count)."
    }

    $outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
    $outputDirectory = [System.IO.Path]::GetDirectoryName($outputFullPath)
    [System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
    $stream = $dllEntries[0].Open()
    try {
        $file = [System.IO.File]::Open($outputFullPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
        try { $stream.CopyTo($file) } finally { $file.Dispose() }
    }
    finally { $stream.Dispose() }
}
finally { $archive.Dispose() }

$item = Get-Item -LiteralPath $outputFullPath
if ($item.Length -ne $ExpectedDllLength) {
    throw "Approved DLL length mismatch. Actual: $($item.Length)"
}
$actualDllHash = Get-Sha256 $outputFullPath
if ($actualDllHash -ne $ExpectedDllSha256) {
    throw "Approved DLL SHA-256 mismatch. Actual: $actualDllHash"
}
$assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($outputFullPath)
if ($assemblyName.Name -ne 'LOM_MessageSpeed' -or $assemblyName.Version.ToString() -ne $ExpectedAssemblyVersion) {
    throw "Approved DLL assembly metadata mismatch: $($assemblyName.FullName)"
}
$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($outputFullPath)
if ($version.FileVersion -ne $ExpectedFileVersion -or
    -not $version.ProductVersion.StartsWith($ExpectedProductVersionPrefix, [System.StringComparison]::Ordinal)) {
    throw "Approved DLL file metadata mismatch. File=$($version.FileVersion), Product=$($version.ProductVersion)"
}
