[CmdletBinding()]
param(
    [switch]$NoRestore
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = Split-Path -Parent $PSScriptRoot
$utf8 = [Text.UTF8Encoding]::new($false)

function Invoke-DotNet {
    param([string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

Push-Location $repoRoot
try {
    [xml]$project = Get-Content -LiteralPath 'Soulstone/Soulstone.csproj' -Raw
    $assemblyVersion = [string]$project.Project.PropertyGroup.Version
    $releaseVersion = ([Version]$assemblyVersion).ToString(3)
    $releaseTag = "V$releaseVersion"
    $manifest = Get-Content -LiteralPath 'Soulstone/Soulstone.json' -Raw | ConvertFrom-Json
    if ($manifest.AssemblyVersion -ne $assemblyVersion) {
        throw 'Plugin project and manifest versions differ.'
    }
    [xml]$serverProject = Get-Content -LiteralPath 'Soulstone.SyncServer/Soulstone.SyncServer.csproj' -Raw
    if ([string]$serverProject.Project.PropertyGroup.Version -ne $releaseVersion) {
        throw 'Server and plugin release versions differ.'
    }
    $repository = Get-Content -LiteralPath 'SoulstoneRep.json' -Raw
    foreach ($field in @('AssemblyVersion', 'TestingAssemblyVersion')) {
        if ($repository -notmatch "$field`: '$([regex]::Escape($assemblyVersion))'") {
            throw "Repository $field differs from the plugin version."
        }
    }
    foreach ($field in @('DownloadLinkInstall', 'DownloadLinkTesting', 'DownloadLinkUpdate')) {
        $url = "https://github.com/Taelina/Soulstone/releases/download/$releaseTag/latest.zip"
        if ($repository -notmatch "$field`: '$([regex]::Escape($url))'") {
            throw "Repository $field does not target $releaseTag."
        }
    }
    $changelog = [IO.File]::ReadAllText((Join-Path $repoRoot 'CHANGELOG.md'))
    $notes = [regex]::Match($changelog, "(?ms)^## \[$([regex]::Escape($releaseVersion))\].*?(?=^---\s*$|^## \[|\z)")
    if (-not $notes.Success) {
        throw "Missing changelog entry for $releaseVersion."
    }

    $restoreArguments = @()
    if ($NoRestore) { $restoreArguments += '--no-restore' }
    Invoke-DotNet (@('test', 'Soulstone.Tests/Soulstone.Tests.csproj', '-c', 'Release') + $restoreArguments)
    Invoke-DotNet (@('test', 'Soulstone.SyncServer.Tests/Soulstone.SyncServer.Tests.csproj', '-c', 'Release') + $restoreArguments)
    Invoke-DotNet (@('build', 'Soulstone.sln', '-c', 'Release') + $restoreArguments)

    $pluginOutput = Join-Path $repoRoot 'Soulstone/bin/Release'
    $builtVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $pluginOutput 'Soulstone.dll')).Version.ToString()
    if ($builtVersion -ne $assemblyVersion) {
        throw "Built plugin version $builtVersion differs from $assemblyVersion."
    }
    $packagePath = Join-Path $pluginOutput 'Soulstone/latest.zip'
    $packageManifest = Join-Path $pluginOutput 'Soulstone/Soulstone.json'
    if (-not (Test-Path -LiteralPath $packagePath) -or -not (Test-Path -LiteralPath $packageManifest)) {
        throw 'DalamudPackager did not produce the plugin ZIP and manifest.'
    }
    $builtManifest = Get-Content -LiteralPath $packageManifest -Raw | ConvertFrom-Json
    if ($builtManifest.AssemblyVersion -ne $assemblyVersion) {
        throw 'Packaged manifest version differs from the plugin version.'
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $pluginZip = [IO.Compression.ZipFile]::OpenRead($packagePath)
    try {
        $entryNames = @($pluginZip.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
        foreach ($required in @('Soulstone.dll', 'Soulstone.json', 'ECommons.dll', 'Localizations/en.json', 'Localizations/fr.json')) {
            if ($required -notin $entryNames) {
                throw "Plugin ZIP is missing $required."
            }
        }
    }
    finally { $pluginZip.Dispose() }

    $output = Join-Path $repoRoot "ReleaseData/$releaseTag"
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    Copy-Item -LiteralPath $packagePath -Destination (Join-Path $output 'latest.zip') -Force
    Copy-Item -LiteralPath $packageManifest -Destination (Join-Path $output 'Soulstone.json') -Force
    Copy-Item -LiteralPath 'SoulstoneRep.json' -Destination (Join-Path $output 'SoulstoneRep.json') -Force
    [IO.File]::WriteAllText((Join-Path $output 'ReleaseNotes.md'), $notes.Value.TrimEnd() + "`n", $utf8)

    # Explicit source allowlist excludes build outputs, local configuration, keys, and databases.
    $deploymentFiles = @('compose.yaml', '.dockerignore', 'LICENSE.md',
        'docs/DEPLOYMENT.md', 'docs/ENCRYPTED_STORAGE.md', 'docs/PUBLICATION_API.md')
    foreach ($directory in @('Soulstone.SyncServer', 'Soulstone.SyncServer.Tests')) {
        foreach ($file in Get-ChildItem -LiteralPath $directory -File) {
            if ($file.Extension -in @('.cs', '.csproj', '.json', '.http', '.ps1', '.md') -or $file.Name -eq 'Dockerfile') {
                $deploymentFiles += "$directory/$($file.Name)"
            }
        }
    }
    $deploymentPath = Join-Path $output 'Soulstone-sync-docker.zip'
    $zipStream = [IO.File]::Open($deploymentPath, [IO.FileMode]::Create)
    $deploymentZip = [IO.Compression.ZipArchive]::new($zipStream, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($relativePath in $deploymentFiles | Sort-Object) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($deploymentZip,
                (Join-Path $repoRoot $relativePath), $relativePath, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally {
        $deploymentZip.Dispose()
        $zipStream.Dispose()
    }
    $assetNames = @('latest.zip', 'Soulstone-sync-docker.zip', 'Soulstone.json', 'SoulstoneRep.json', 'ReleaseNotes.md')
    $hashes = foreach ($assetName in $assetNames) {
        $hash = (Get-FileHash -LiteralPath (Join-Path $output $assetName) -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $assetName"
    }
    [IO.File]::WriteAllText((Join-Path $output 'SHA256SUMS.txt'), ($hashes -join "`n") + "`n", $utf8)
    Write-Output "Prepared $releaseTag assets in $output"
    Write-Output 'Docker image and in-game validation must be performed separately before publishing.'
}
finally { Pop-Location }
