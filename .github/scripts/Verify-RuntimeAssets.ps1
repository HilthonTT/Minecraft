<#
.SYNOPSIS
    Checks that every asset the game loads by a literal path was copied next to the built assembly.

.DESCRIPTION
    Shaders and resources are read at runtime from a path relative to the output directory, so a file that
    is not copied there compiles perfectly well and only fails once the game is running. Rather than keep a
    hand written list in step with the code, the expected paths are collected from the source itself: every
    shader stage constant and every Assets.Path("...") call names one.

.PARAMETER SourceRoot
    Directory to scan for C# source.

.PARAMETER OutputDirectory
    Build output directory the assets are expected to have been copied into.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $SourceRoot,
    [Parameter(Mandatory)] [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $OutputDirectory -PathType Container)) {
    throw "Output directory '$OutputDirectory' does not exist. Did the build run?"
}

# Only literal paths can be checked. Assets.Path(someVariable) is skipped on purpose: the saves directory,
# for instance, is created at runtime rather than shipped.
$patterns = @(
    '(?:VertexFile|FragmentFile)\s*=\s*"([^"]+)"'
    'Assets\.Path\("([^"]+)"\)'
)

$expected = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

$sourceFiles = Get-ChildItem -LiteralPath $SourceRoot -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

foreach ($file in $sourceFiles) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($pattern in $patterns) {
        foreach ($match in [regex]::Matches($text, $pattern)) {
            [void]$expected.Add($match.Groups[1].Value)
        }
    }
}

# A regex that silently stops matching would turn this check into a no-op that always passes.
if ($expected.Count -eq 0) {
    throw "Found no asset references under '$SourceRoot'. This check is not looking where it should be."
}

$missing = @()
foreach ($relativePath in $expected) {
    $fullPath = Join-Path $OutputDirectory $relativePath
    if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
        Write-Host "  ok       $relativePath"
    } else {
        Write-Host "  MISSING  $relativePath"
        $missing += $relativePath
    }
}

Write-Host ''

if ($missing.Count -gt 0) {
    throw ("$($missing.Count) of $($expected.Count) runtime assets are missing from '$OutputDirectory'. " +
        "Check that they are included in the csproj with CopyToOutputDirectory: " + ($missing -join ', '))
}

Write-Host "All $($expected.Count) runtime assets are present."
