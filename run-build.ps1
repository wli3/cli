#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

param(
    [string]$Configuration="Debug",
    [string]$Architecture="x64",
    # This is here just to eat away this parameter because CI still passes this in.
    [string]$Targets="Default",
    [switch]$NoPackage,
    [switch]$NoBuild,
    [switch]$Help,
    [Parameter(Position=0, ValueFromRemainingArguments=$true)]
    $ExtraParameters)

if($Help)
{
    Write-Host "Usage: .\build.ps1 [-Configuration <CONFIGURATION>] [-Architecture <ARCHITECTURE>] [-NoPackage] [-Help]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Configuration <CONFIGURATION>     Build the specified Configuration (Debug or Release, default: Debug)"
    Write-Host "  -Architecture <ARCHITECTURE>       Build the specified architecture (x64 or x86 (supported only on Windows), default: x64)"
    Write-Host "  -NoPackage                         Skip packaging targets"
    Write-Host "  -NoBuild                           Skip building the product"
    Write-Host "  -Help                              Display this help message"
    exit 0
}

$env:CONFIGURATION = $Configuration;
$RepoRoot = "$PSScriptRoot"
$env:NUGET_PACKAGES = "$RepoRoot\.nuget\packages"

if($NoPackage)
{
    $env:DOTNET_BUILD_SKIP_PACKAGING=1
}
else
{
    $env:DOTNET_BUILD_SKIP_PACKAGING=0
}

# Load Branch Info
cat "$RepoRoot\branchinfo.txt" | ForEach-Object {
    if(!$_.StartsWith("#") -and ![String]::IsNullOrWhiteSpace($_)) {
        $splat = $_.Split([char[]]@("="), 2)
        Set-Content "env:\$($splat[0])" -Value $splat[1]
    }
}

# Create a stage0 PJ install directory
if (!$env:DOTNET_INSTALL_DIR_PJ)
{
    $env:DOTNET_INSTALL_DIR_PJ="$RepoRoot\.dotnet_stage0PJ\$Architecture"
}

if (!(Test-Path $env:DOTNET_INSTALL_DIR_PJ))
{
    mkdir $env:DOTNET_INSTALL_DIR_PJ | Out-Null
}

# Also create an install directory for a post-PJnistic CLI
if (!$env:DOTNET_INSTALL_DIR)
{
    $env:DOTNET_INSTALL_DIR="$RepoRoot\.dotnet_stage0\$Architecture"
}

if (!(Test-Path $env:DOTNET_INSTALL_DIR))
{
    mkdir $env:DOTNET_INSTALL_DIR | Out-Null
}

# Disable first run since we want to control all package sources
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Enable vs test console logging
$env:VSTEST_BUILD_TRACE=1
$env:VSTEST_TRACE_BUILD=1

$dotnetInstallPath = Join-Path $RepoRoot "scripts\obtain\dotnet-install.ps1"

# install the stage0PJ
Write-Host "$dotnetInstallPath -Version ""1.0.0-preview3-003223""  -InstallDir $env:DOTNET_INSTALL_DIR_PJ -Architecture ""$Architecture"""
Invoke-Expression "$dotnetInstallPath -Version ""1.0.0-preview3-003223"" -InstallDir $env:DOTNET_INSTALL_DIR_PJ -Architecture ""$Architecture"""
if ($LastExitCode -ne 0)
{
    Write-Output "The .NET CLI stage0PJ installation failed with exit code $LastExitCode"
    exit $LastExitCode
}

# install the post-PJnistic stage0
Write-Host "$dotnetInstallPath -Version ""1.1.10-servicing-005283""  -InstallDir $env:DOTNET_INSTALL_DIR -Architecture ""$Architecture"""
Invoke-Expression "$dotnetInstallPath -Version ""1.1.10-servicing-005283"" -InstallDir $env:DOTNET_INSTALL_DIR -Architecture ""$Architecture"""
if ($LastExitCode -ne 0)
{
    Write-Output "The .NET CLI stage0 installation failed with exit code $LastExitCode"
    exit $LastExitCode
}

# Put the stage0 on the path
$env:PATH = "$env:DOTNET_INSTALL_DIR;$env:PATH"

if ($NoBuild)
{
    Write-Host "Not building due to --nobuild"
    Write-Host "Command that would be run: 'dotnet msbuild build.proj /m /p:Architecture=$Architecture $ExtraParameters'"
}
else
{
    dotnet msbuild build.proj /m /v:diag /p:Architecture=$Architecture $ExtraParameters
    if($LASTEXITCODE -ne 0) { throw "Failed to build" } 
}
