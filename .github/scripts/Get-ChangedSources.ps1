<#
.SYNOPSIS
    Lists the C# files a pull request adds or modifies, as a glob list for `dotnet format --include`.

.DESCRIPTION
    The sources predate .editorconfig and do not pass a repository wide format check, so the formatting gate
    is scoped to what a pull request actually touches. Deleted files are dropped, since dotnet format cannot
    load a path that is no longer there.

    Two outputs are written to $env:GITHUB_OUTPUT: `files`, a space separated list of repository relative
    paths, and `any`, 'true' only when that list is non empty.

.PARAMETER BaseSha
    Commit the pull request is merging into, used as the diff base.

.PARAMETER HeadSha
    Commit at the tip of the pull request.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $BaseSha,
    [Parameter(Mandatory)] [string] $HeadSha
)

$ErrorActionPreference = 'Stop'

# --diff-filter excludes deletions; ACMR keeps added, copied, modified and renamed paths.
$changed = git diff --name-only --diff-filter=ACMR $BaseSha $HeadSha -- '*.cs'
if ($LASTEXITCODE -ne 0) {
    throw "git diff between '$BaseSha' and '$HeadSha' failed. Was the repository checked out with fetch-depth: 0?"
}

# Generated output under bin/ and obj/ is never worth formatting, and is normally not committed anyway.
$sources = @($changed | Where-Object { $_ -and $_ -notmatch '(^|/)(bin|obj)/' })

if ($sources.Count -eq 0) {
    Write-Host 'No C# files changed. Skipping the format check.'
    "files=" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    "any=false" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    return
}

foreach ($source in $sources) {
    Write-Host "  $source"
}

Write-Host ''
Write-Host "$($sources.Count) changed C# file(s) will be checked."

"files=$($sources -join ' ')" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
"any=true" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
