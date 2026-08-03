param(
    [string] $CandidateZip = (Join-Path $PSScriptRoot '..\artifacts\phase11-a\LOM_MessageSpeed-v0.2.0-candidate.zip')
)

$ErrorActionPreference = 'Stop'
$stageScript = Join-Path $PSScriptRoot 'Stage-ApprovedPluginPayload.ps1'
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('lom-payload-tests-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$passed = 0

function Invoke-Stage {
    param([string] $Zip, [string] $Output, [string] $ZipHash, [string] $DllHash = '0D23512EB5E59C8AAEC4545D344F8D4948C99E22E163D6DB2E5AB0D6D98B40A3')
    & $stageScript -ZipPath $Zip -OutputPath $Output `
        -ExpectedZipSha256 $ZipHash `
        -ExpectedDllSha256 $DllHash `
        -ExpectedDllLength 19968 `
        -ExpectedAssemblyVersion '1.0.0.0' `
        -ExpectedFileVersion '0.2.0.0' `
        -ExpectedProductVersionPrefix '0.2.0'
}

function Pass([string] $Name) {
    $script:passed++
    Write-Output "ok $script:passed - $Name"
}

function Expect-Failure([string] $Name, [scriptblock] $Action) {
    try {
        & $Action
    }
    catch {
        Pass $Name
        return
    }
    throw "Expected failure: $Name"
}

function New-TestZip {
    param([string] $Name, [object[]] $Entries)
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $path = Join-Path $tempRoot $Name
    $archive = [System.IO.Compression.ZipFile]::Open($path, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entryData in $Entries) {
            $entry = $archive.CreateEntry($entryData.Name)
            $stream = $entry.Open()
            try { $stream.Write($entryData.Bytes, 0, $entryData.Bytes.Length) } finally { $stream.Dispose() }
        }
    }
    finally { $archive.Dispose() }
    return $path
}

try {
    $resolvedCandidate = (Resolve-Path -LiteralPath $CandidateZip).Path
    $candidateHash = (Get-FileHash -LiteralPath $resolvedCandidate -Algorithm SHA256).Hash
    $goodOutput = Join-Path $tempRoot 'good\LOM_MessageSpeed.dll'
    Invoke-Stage $resolvedCandidate $goodOutput $candidateHash
    if ((Get-FileHash -LiteralPath $goodOutput -Algorithm SHA256).Hash -ne '0D23512EB5E59C8AAEC4545D344F8D4948C99E22E163D6DB2E5AB0D6D98B40A3') {
        throw 'Correct staging produced the wrong DLL.'
    }
    Pass 'approved ZIP stages the exact DLL'

    Expect-Failure 'ZIP hash mismatch is rejected' {
        Invoke-Stage $resolvedCandidate (Join-Path $tempRoot 'bad-zip.dll') ('A' * 64)
    }
    Expect-Failure 'DLL hash mismatch is rejected' {
        Invoke-Stage $resolvedCandidate (Join-Path $tempRoot 'bad-dll.dll') $candidateHash ('B' * 64)
    }

    $dllBytes = [System.IO.File]::ReadAllBytes($goodOutput)
    $unsafe = New-TestZip 'unsafe.zip' @(
        @{ Name = '../LOM_MessageSpeed.dll'; Bytes = $dllBytes }
    )
    $unsafeHash = (Get-FileHash -LiteralPath $unsafe -Algorithm SHA256).Hash
    Expect-Failure 'unsafe ZIP entry is rejected' {
        Invoke-Stage $unsafe (Join-Path $tempRoot 'unsafe.dll') $unsafeHash
    }

    $duplicate = New-TestZip 'duplicate.zip' @(
        @{ Name = 'LOM_MessageSpeed.dll'; Bytes = $dllBytes },
        @{ Name = 'LOM_MessageSpeed.dll'; Bytes = $dllBytes }
    )
    $duplicateHash = (Get-FileHash -LiteralPath $duplicate -Algorithm SHA256).Hash
    Expect-Failure 'duplicate DLL entry is rejected' {
        Invoke-Stage $duplicate (Join-Path $tempRoot 'duplicate.dll') $duplicateHash
    }

    $missing = New-TestZip 'missing.zip' @(
        @{ Name = 'README.md'; Bytes = [System.Text.Encoding]::UTF8.GetBytes('missing') }
    )
    $missingHash = (Get-FileHash -LiteralPath $missing -Algorithm SHA256).Hash
    Expect-Failure 'missing DLL entry is rejected' {
        Invoke-Stage $missing (Join-Path $tempRoot 'missing.dll') $missingHash
    }

    $extra = New-TestZip 'extra.zip' @(
        @{ Name = 'LOM_MessageSpeed.dll'; Bytes = $dllBytes },
        @{ Name = 'other.exe'; Bytes = @(0, 1, 2) }
    )
    $extraHash = (Get-FileHash -LiteralPath $extra -Algorithm SHA256).Hash
    Expect-Failure 'unexpected executable payload is rejected' {
        Invoke-Stage $extra (Join-Path $tempRoot 'extra.dll') $extraHash
    }

    Write-Output "PASS: $passed payload staging tests"
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
