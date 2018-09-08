// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Frameworks;
using NuGet.Versioning;
using Xunit;

namespace Microsoft.DotNet.ToolPackage.Tests
{
    public class LocalToolsResolverCacheTests : TestBase
    {
        private static
            (DirectoryPath nuGetGlobalPackagesFolder,
            LocalToolsResolverCache localToolsResolverCache) Setup()
        {
            IFileSystem fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();
            DirectoryPath tempDirectory =
                new DirectoryPath(fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath);
            DirectoryPath cacheDirectory = tempDirectory.WithSubDirectories("cacheDirectory");
            DirectoryPath nuGetGlobalPackagesFolder = tempDirectory.WithSubDirectories("nugetGlobalPackageLocation");
            fileSystem.Directory.CreateDirectory(cacheDirectory.Value);
            const int version = 1;

            LocalToolsResolverCache localToolsResolverCache =
                new LocalToolsResolverCache(fileSystem, cacheDirectory, version);
            return (nuGetGlobalPackagesFolder, localToolsResolverCache);
        }

        [Fact]
        public void GivenExecutableIdentifierItCanSaveAndCannotLoadWithMismatches()
        {
            (DirectoryPath nuGetGlobalPackagesFolder, LocalToolsResolverCache localToolsResolverCache) = Setup();

            NuGetFramework targetFramework = NuGetFramework.Parse("netcoreapp2.1");
            string runtimeIdentifier = "any";
            PackageId packageId = new PackageId("my.toolBundle");
            NuGetVersion nuGetVersion = NuGetVersion.Parse("1.0.2");
            IReadOnlyList<RestoredCommand> listOfCommandSettings = new[]
            {
                new RestoredCommand("tool1", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool1.dll")),
                new RestoredCommand("tool2", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool2.dll"))
            };

            localToolsResolverCache.Save(
                listOfCommandSettings.ToDictionary(
                    c => new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                        c.Name)),
                nuGetGlobalPackagesFolder);

            localToolsResolverCache
                .TryLoad(
                    new RestoreCommandIdentifier(packageId, NuGetVersion.Parse("1.0.0-wrong-version"), targetFramework,
                        runtimeIdentifier, listOfCommandSettings[0].Name), nuGetGlobalPackagesFolder, out _)
                .Should().BeFalse();

            localToolsResolverCache
                .TryLoad(
                    new RestoreCommandIdentifier(packageId, nuGetVersion, NuGetFramework.Parse("wrongFramework"),
                        runtimeIdentifier, listOfCommandSettings[0].Name), nuGetGlobalPackagesFolder, out _)
                .Should().BeFalse();

            localToolsResolverCache
                .TryLoad(
                    new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework,
                        "wrongRuntimeIdentifier", listOfCommandSettings[0].Name),
                    nuGetGlobalPackagesFolder, out _)
                .Should().BeFalse();
        }

        [Fact]
        public void GivenExecutableIdentifierItCanSaveAndLoad()
        {
            (DirectoryPath nuGetGlobalPackagesFolder, LocalToolsResolverCache localToolsResolverCache) = Setup();

            NuGetFramework targetFramework = NuGetFramework.Parse("netcoreapp2.1");
            string runtimeIdentifier = "any";
            PackageId packageId = new PackageId("my.toolBundle");
            NuGetVersion nuGetVersion = NuGetVersion.Parse("1.0.2");
            IReadOnlyList<RestoredCommand> listOfCommandSettings = new[]
            {
                new RestoredCommand("tool1", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool1.dll")),
                new RestoredCommand("tool2", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool2.dll"))
            };

            localToolsResolverCache.Save(
                listOfCommandSettings.ToDictionary(
                    c => new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                        c.Name)),
                nuGetGlobalPackagesFolder);

            localToolsResolverCache.TryLoad(
                new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                    listOfCommandSettings[0].Name),
                nuGetGlobalPackagesFolder, out var commandSettingsTool1).Should().BeTrue();

            localToolsResolverCache.TryLoad(
                new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                    listOfCommandSettings[1].Name),
                nuGetGlobalPackagesFolder, out var commandSettingsTool2).Should().BeTrue();


            commandSettingsTool1.ShouldBeEquivalentTo(listOfCommandSettings[0]);
            commandSettingsTool2.ShouldBeEquivalentTo(listOfCommandSettings[1]);
        }

        [Fact]
        public void GivenExecutableIdentifierItCanSaveMultipleSameAndLoadContainsOnlyOne()
        {
            (DirectoryPath nuGetGlobalPackagesFolder, LocalToolsResolverCache localToolsResolverCache) = Setup();

            NuGetFramework targetFramework = NuGetFramework.Parse("netcoreapp2.1");
            string runtimeIdentifier = "any";
            PackageId packageId = new PackageId("my.toolBundle");
            NuGetVersion nuGetVersion = NuGetVersion.Parse("1.0.2");
            IReadOnlyList<RestoredCommand> listOfCommandSettings = new[]
            {
                new RestoredCommand("tool1", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool1.dll")),
                new RestoredCommand("tool2", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool2.dll"))
            };

            localToolsResolverCache.Save(
                listOfCommandSettings.ToDictionary(
                    c => new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                        c.Name)),
                nuGetGlobalPackagesFolder);

            localToolsResolverCache.Save(
                listOfCommandSettings.ToDictionary(
                    c => new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                        c.Name)),
                nuGetGlobalPackagesFolder);

            localToolsResolverCache.TryLoad(
                new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                    listOfCommandSettings[0].Name),
                nuGetGlobalPackagesFolder, out var commandSettingsTool1);

            localToolsResolverCache.TryLoad(
                new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                    listOfCommandSettings[1].Name),
                nuGetGlobalPackagesFolder, out var commandSettingsTool2);


            commandSettingsTool1.ShouldBeEquivalentTo(listOfCommandSettings[0]);
            commandSettingsTool2.ShouldBeEquivalentTo(listOfCommandSettings[1]);
        }

        [Fact]
        public void ItCanSaveMultipleSameAndLoadTheHighestFromVersionRange()
        {
            (DirectoryPath nuGetGlobalPackagesFolder, LocalToolsResolverCache localToolsResolverCache) = Setup();

            NuGetFramework targetFramework = NuGetFramework.Parse("netcoreapp2.1");
            string runtimeIdentifier = "any";
            PackageId packageId = new PackageId("my.toolBundle");

            NuGetVersion previewNuGetVersion = NuGetVersion.Parse("0.0.2");
            IReadOnlyList<RestoredCommand> listOfCommandSettings0 = new[]
            {
                new RestoredCommand("tool1", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool1preview.dll")),
            };

            NuGetVersion nuGetVersion = NuGetVersion.Parse("1.0.2");
            IReadOnlyList<RestoredCommand> listOfCommandSettings1 = new[]
            {
                new RestoredCommand("tool1", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool1.dll")),
            };

            NuGetVersion newerNuGetVersion = NuGetVersion.Parse("2.0.2");
            IReadOnlyList<RestoredCommand> listOfCommandSettings2 = new[]
            {
                new RestoredCommand("tool1", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool1new.dll")),
            };

            localToolsResolverCache.Save(
                listOfCommandSettings0.ToDictionary(
                    c => new RestoreCommandIdentifier(packageId, previewNuGetVersion, targetFramework, runtimeIdentifier,
                        c.Name)),
                nuGetGlobalPackagesFolder);

            localToolsResolverCache.Save(
                listOfCommandSettings1.ToDictionary(
                    c => new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                        c.Name)),
                nuGetGlobalPackagesFolder);

            localToolsResolverCache.Save(
                listOfCommandSettings2.ToDictionary(
                    c => new RestoreCommandIdentifier(packageId, newerNuGetVersion, targetFramework, runtimeIdentifier,
                        c.Name)),
                nuGetGlobalPackagesFolder);


            bool loadSuccess =
                localToolsResolverCache.TryLoadHighestVersion(
                    new RestoreCommandIdentifierVersionRange(
                        packageId,
                        VersionRange.Parse("(0.0.0, 2.0.0)"),
                        targetFramework, runtimeIdentifier, "tool1"),
                    nuGetGlobalPackagesFolder, out RestoredCommand loadedResolverCache);

            loadSuccess.Should().BeTrue();

            loadedResolverCache.ShouldBeEquivalentTo(listOfCommandSettings1[0]);
        }

        [Fact]
        public void ItReturnsFalseWhenFailedToLoadVersionRange()
        {
            (DirectoryPath nuGetGlobalPackagesFolder, LocalToolsResolverCache localToolsResolverCache) = Setup();

            bool loadSuccess =
                localToolsResolverCache.TryLoadHighestVersion(
                    new RestoreCommandIdentifierVersionRange(
                        new PackageId("my.toolBundle"),
                        VersionRange.Parse("(0.0.0, 2.0.0)"),
                        NuGetFramework.Parse("netcoreapp2.1"), "any", "tool1"),
                    nuGetGlobalPackagesFolder, out _);

            loadSuccess.Should().BeFalse();
        }

        [Fact]
        public void GivenExecutableIdentifierItCanSaveMultipleVersionAndLoad()
        {
            (DirectoryPath nuGetGlobalPackagesFolder, LocalToolsResolverCache localToolsResolverCache) = Setup();

            NuGetFramework targetFramework = NuGetFramework.Parse("netcoreapp2.1");
            string runtimeIdentifier = "any";
            PackageId packageId = new PackageId("my.toolBundle");
            NuGetVersion nuGetVersion = NuGetVersion.Parse("1.0.2");
            IReadOnlyList<RestoredCommand> listOfCommandSettings = new[]
            {
                new RestoredCommand("tool1", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool1.dll")),
            };

            NuGetVersion newerNuGetVersion = NuGetVersion.Parse("2.0.2");
            IReadOnlyList<RestoredCommand> listOfCommandSettings2 = new[]
            {
                new RestoredCommand("tool1", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool1new.dll")),
                new RestoredCommand("tool2", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool2new.dll")),
            };

            localToolsResolverCache.Save(
                listOfCommandSettings.ToDictionary(
                    c => new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                        c.Name)),
                nuGetGlobalPackagesFolder);

            localToolsResolverCache.Save(
                listOfCommandSettings2.ToDictionary(
                    c => new RestoreCommandIdentifier(packageId, newerNuGetVersion, targetFramework, runtimeIdentifier,
                        c.Name)),
                nuGetGlobalPackagesFolder);

            localToolsResolverCache.TryLoad(
                new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                    listOfCommandSettings[0].Name),
                nuGetGlobalPackagesFolder, out RestoredCommand tool1);
            localToolsResolverCache.TryLoad(
                new RestoreCommandIdentifier(packageId, newerNuGetVersion, targetFramework, runtimeIdentifier,
                    listOfCommandSettings2[0].Name),
                nuGetGlobalPackagesFolder, out RestoredCommand tool1Newer);

            localToolsResolverCache.TryLoad(
                new RestoreCommandIdentifier(packageId, newerNuGetVersion, targetFramework, runtimeIdentifier,
                    listOfCommandSettings2[1].Name),
                nuGetGlobalPackagesFolder, out RestoredCommand tool2Newer);

            tool1.ShouldBeEquivalentTo(listOfCommandSettings[0]);
            tool1Newer.ShouldBeEquivalentTo(listOfCommandSettings2[0]);
            tool2Newer.ShouldBeEquivalentTo(listOfCommandSettings2[1]);
        }

        [Fact]
        public void WhenTheCacheIsCorruptedByAppendingLineItShouldLoadAsEmpty()
        {
            WhenTheCacheIsCorruptedItShouldLoadAsEmpty(
                useRealFileSystem: false,
                corruptCache: (fileSystem, cachePath, existingCache) =>
                    fileSystem.File.WriteAllText(cachePath, existingCache + " !!!Corrupted")
            );
        }

        [Fact]
        public void WhenTheCacheIsCorruptedByNotAJsonItShouldLoadAsEmpty()
        {
            WhenTheCacheIsCorruptedItShouldLoadAsEmpty(
                useRealFileSystem: true,
                corruptCache: (fileSystem, cachePath, existingCache) =>
                {
                    File.WriteAllBytes(cachePath, new byte[] {0x12, 0x23, 0x34, 0x45});
                }
            );
        }

        [Fact]
        public void WhenTheCacheIsCorruptedItShouldNotAffectNextSaveAndLoad()
        {
            IFileSystem fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();

            DirectoryPath tempDirectory =
                new DirectoryPath(fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath);
            DirectoryPath cacheDirectory = tempDirectory.WithSubDirectories("cacheDirectory");
            DirectoryPath nuGetGlobalPackagesFolder = tempDirectory.WithSubDirectories("nugetGlobalPackageLocation");
            fileSystem.Directory.CreateDirectory(cacheDirectory.Value);
            const int version = 1;

            LocalToolsResolverCache localToolsResolverCache =
                new LocalToolsResolverCache(fileSystem, cacheDirectory, version);

            NuGetFramework targetFramework = NuGetFramework.Parse("netcoreapp2.1");
            string runtimeIdentifier = "any";
            PackageId packageId = new PackageId("my.toolBundle");
            NuGetVersion nuGetVersion = NuGetVersion.Parse("1.0.2");
            IReadOnlyList<RestoredCommand> listOfCommandSettings = new[]
            {
                new RestoredCommand("tool1", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool1.dll")),
            };

            localToolsResolverCache.Save(
                listOfCommandSettings.ToDictionary(
                    c => new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                        c.Name)),
                nuGetGlobalPackagesFolder);

            var cachePath = cacheDirectory
                .WithSubDirectories(version.ToString())
                .WithSubDirectories(packageId.ToString()).Value;
            var existingCache =
                fileSystem.File.ReadAllText(
                    cachePath);
            existingCache.Should().NotBeEmpty();

            fileSystem.File.WriteAllText(cachePath, existingCache + " !!!Corrupted");

            // Save after corruption
            localToolsResolverCache.Save(
                listOfCommandSettings.ToDictionary(
                    c => new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                        c.Name)),
                nuGetGlobalPackagesFolder);

            localToolsResolverCache.TryLoad(
                new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                    listOfCommandSettings[0].Name),
                nuGetGlobalPackagesFolder, out RestoredCommand commandSettings);

            commandSettings.ShouldBeEquivalentTo(listOfCommandSettings[0]);
        }

        private static void WhenTheCacheIsCorruptedItShouldLoadAsEmpty(
            bool useRealFileSystem,
            Action<IFileSystem, string, string> corruptCache)
        {
            IFileSystem fileSystem =
                useRealFileSystem == false
                    ? new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build()
                    : new FileSystemWrapper();

            DirectoryPath tempDirectory =
                new DirectoryPath(fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath);
            DirectoryPath cacheDirectory = tempDirectory.WithSubDirectories("cacheDirectory");
            DirectoryPath nuGetGlobalPackagesFolder = tempDirectory.WithSubDirectories("nugetGlobalPackageLocation");
            fileSystem.Directory.CreateDirectory(cacheDirectory.Value);
            const int version = 1;

            LocalToolsResolverCache localToolsResolverCache =
                new LocalToolsResolverCache(fileSystem, cacheDirectory, version);

            NuGetFramework targetFramework = NuGetFramework.Parse("netcoreapp2.1");
            string runtimeIdentifier = "any";
            PackageId packageId = new PackageId("my.toolBundle");
            NuGetVersion nuGetVersion = NuGetVersion.Parse("1.0.2");
            IReadOnlyList<RestoredCommand> listOfCommandSettings = new[]
            {
                new RestoredCommand("tool1", "dotnet", nuGetGlobalPackagesFolder.WithFile("tool1.dll")),
            };

            localToolsResolverCache.Save(
                listOfCommandSettings.ToDictionary(
                    c => new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                        c.Name)),
                nuGetGlobalPackagesFolder);

            var cachePath = cacheDirectory
                .WithSubDirectories(version.ToString())
                .WithSubDirectories(packageId.ToString()).Value;
            var existingCache =
                fileSystem.File.ReadAllText(
                    cachePath);
            existingCache.Should().NotBeEmpty();

            corruptCache(fileSystem, cachePath, existingCache);

            localToolsResolverCache.TryLoad(
                new RestoreCommandIdentifier(packageId, nuGetVersion, targetFramework, runtimeIdentifier,
                    listOfCommandSettings[0].Name),
                nuGetGlobalPackagesFolder, out _).Should().BeFalse("Consider corrupted file cache miss");
        }
    }
}
