param(
    [Parameter(Mandatory)]
    [string] $Compiler,
    [string] $Dotnet = 'dotnet',
    [string] $Output,
    [string] $Version,
    [ValidateSet('x64', 'arm64')]
    [string[]] $Architecture = @('x64', 'arm64')
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$project = Join-Path $root 'src/RecycleBinStudio/RecycleBinStudio.csproj'
$compilerPath = (Get-Command $Compiler).Source

if (-not $Version) {
    $Version = ([xml](Get-Content -Raw -LiteralPath $project)).Project.PropertyGroup.Version
}
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Expected a numeric release version such as 1.1.0.'
}
if (-not $Output) {
    $Output = Join-Path $root "artifacts/installer-$Version"
}

$target = (New-Item -ItemType Directory -Force -Path $Output).FullName
$script = Join-Path $PSScriptRoot 'RecycleBinStudio.iss'
$payloadRoot = Join-Path $target ('payload-' + [guid]::NewGuid().ToString('N'))
$buildPath = Join-Path $target 'build'

foreach ($arch in $Architecture) {
    $payload = Join-Path $payloadRoot "win-$arch"
    & $Dotnet publish $project -c Release -r "win-$arch" --self-contained true -warnaserror --artifacts-path $buildPath -o $payload "-p:Version=$Version"
    if ($LASTEXITCODE -ne 0) { throw "Publish failed for win-$arch." }

    & $compilerPath "/DArchitecture=$arch" "/DBuildVersion=$Version" "/DPayload=$payload" "/DBuildOutput=$target" $script
    if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed for win-$arch." }
}
