// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
using NuGet.Protocol.Core.Types;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.ExecutablePackageObtainer.Tests
{
    public class ExecutablePackageObtainerTests
    {

        private static readonly Func<FilePath> GetTempProjectPath = () =>
        {
            var tempProjectDirectory =
                new DirectoryPath(Path.GetTempPath()).WithCombineFollowing(Path.GetRandomFileName());
            var tempProjectPath =
                tempProjectDirectory.CreateFilePathWithCombineFollowing(Path.GetRandomFileName() + ".csproj");
            return tempProjectPath;
        };
        
        [Fact]
        public void GivenNugetConfigAndPackageNameAndVersionAndTargetFrameworkWhenCallItCanDownloadThePacakge()
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer =
                new ExecutablePackageObtainer(new DirectoryPath(toolsPath), GetTempProjectPath);
            var toolConfigurationAndExecutableDirectory = packageObtainer.ObtainAndReturnExecutablePath(
                packageId: "console.wul.test.app.one",
                packageVersion: "1.0.5",
                nugetconfig: nugetConfigPath,
                targetframework: "netcoreapp2.0");

            var executable = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .CreateFilePathWithCombineFollowing(
                    toolConfigurationAndExecutableDirectory
                        .Configuration
                        .ToolAssemblyEntryPoint);

            File.Exists(executable.Value)
                .Should()
                .BeTrue(executable + " should have the executable");
        }

        [Fact]
        public void GivenNugetConfigAndPackageNameAndVersionAndTargetFrameworkWhenCallItCreateAssetFile()
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer =
                new ExecutablePackageObtainer(new DirectoryPath(toolsPath), GetTempProjectPath);
            var toolConfigurationAndExecutableDirectory = packageObtainer.ObtainAndReturnExecutablePath(
                packageId: "console.wul.test.app.one",
                packageVersion: "1.0.5",
                nugetconfig: nugetConfigPath,
                targetframework: "netcoreapp2.0");

            var assetJsonPath = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .GetParentPath()
                .GetParentPath()
                .GetParentPath()
                .GetParentPath()
                .CreateFilePathWithCombineFollowing("project.assets.json").Value;

            File.Exists(assetJsonPath)
                .Should()
                .BeTrue(assetJsonPath + " should be created");
        }

        [Fact]
        public void GivenPackageNameAndVersionAndTargetFrameworkWhenCallItCreateAssetFile()
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer =
                new ExecutablePackageObtainer(new DirectoryPath(toolsPath), WriteNugetConfigFileToPointToTheFeed);
            var toolConfigurationAndExecutableDirectory = packageObtainer.ObtainAndReturnExecutablePath(
                packageId: "console.wul.test.app.one",
                packageVersion: "1.0.5",
                targetframework: "netcoreapp2.0");

            var assetJsonPath = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .GetParentPath()
                .GetParentPath()
                .GetParentPath()
                .GetParentPath()
                .CreateFilePathWithCombineFollowing("project.assets.json").Value;

            File.Exists(assetJsonPath)
                .Should()
                .BeTrue(assetJsonPath + " should be created");
        }

        private static FilePath WriteNugetConfigFileToPointToTheFeed()
        {
            var nugetConfigName = Path.GetRandomFileName() + ".config";
            var execuateDir =
                Path.GetDirectoryName(
                    System.Reflection
                        .Assembly
                        .GetExecutingAssembly()
                        .Location);
            NuGetConfig.Write(
                directory: execuateDir,
                configname: nugetConfigName,
                localFeedPath: Path.Combine(execuateDir, "TestAssetLocalNugetFeed"));
            return new FilePath(Path.GetFullPath(nugetConfigName));
        }

        [Fact(Skip = "Pending")]
        public void GivenNugetConfigAndPackageNameAndVersionWithoutTargetFrameworkWhenCallItCanDownloadThePacakge()
        {
        }
    }
}
