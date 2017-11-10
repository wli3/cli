// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Cli;
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
    public class ExecutablePackageObtainerTests // TODO the PackageObtainer should be called Executalbe Package Obtainer
    {
        [Fact]
        public void GivenNugetConfigAndPackageNameAndVersionAndTargetFrameworkWhenCallItCanDownloadThePacakge()
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var randomFileName = Path.GetRandomFileName();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), randomFileName); // TODO Nocheck in make it mock file system or windows only 
                
            var packageObtainer = new ExecutablePackageObtainer(new DotNetCommandFactory(), new DirectoryPath(toolsPath));
            var toolConfigurationAndExecutableDirectory = packageObtainer.ObtainAndReturnExecutablePath("console.wul.test.app.one", "1.0.5", nugetConfigPath, "netcoreapp2.0");

            var executable = toolConfigurationAndExecutableDirectory.ExecutableDirectory.CreateFilePath(toolConfigurationAndExecutableDirectory.Configuration.ToolAssemblyEntryPoint).Value;
            File.Exists(executable)
                .Should()
                .BeTrue(executable + " should have the executable");
        }

        private static FilePath WriteNugetConfigFileToPointToTheFeed()
        {
            var nugetConfigName = Path.GetRandomFileName() + ".config";
            var execuateDir =
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            NuGetConfig.Write(
                directory: execuateDir , 
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
