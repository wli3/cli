// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ExecutablePackageObtainer
{
    public class ExecutablePackageObtainer
    {
        private readonly ICommandFactory _commandFactory;
        private readonly DirectoryPath _toolsPath;

        public ExecutablePackageObtainer(ICommandFactory commandFactory, DirectoryPath toolsPath)
        {
            _commandFactory = commandFactory ?? throw new ArgumentNullException(nameof(commandFactory));
            _toolsPath = toolsPath ?? throw new ArgumentNullException(nameof(toolsPath));
        }

        public ToolConfigurationAndExecutableDirectory ObtainAndReturnExecutablePath(
            string packageId,
            string packageVersion,
            FilePath nugetconfig,
            string targetframework)
        {
            if (packageId == null) throw new ArgumentNullException(nameof(packageId));
            if (packageVersion == null) throw new ArgumentNullException(nameof(packageVersion));

            var individualToolVersion = CreateIndividualToolVersionDirectory(packageId, packageVersion);

            var tempProjectPath = CreateTempProject(packageId, packageVersion, targetframework, individualToolVersion);

            var fa = new CommandFactory();
            var comamnd = fa.Create(
                    "dotnet",
                    new[]
                    {
                        "restore",
                        "--runtime", RuntimeEnvironment.GetRuntimeIdentifier(),
                        "--configfile", nugetconfig.ToEscapedString()
                    })
                .WorkingDirectory(tempProjectPath.GetDirectoryPath().Value)
                .CaptureStdOut()
                .CaptureStdErr();

            var result = comamnd.Execute();
            if (result.ExitCode != 0)
            {
                throw new Exception(result.StdErr + result.StdOut);
            }

            var toolConfigurationPath = individualToolVersion
                .WithCombineFollowing(packageId, packageVersion, "tools")
                .CreateFilePathWithCombineFollowing("DotnetToolsConfig.xml");

            var toolConfiguration = ToolConfigurationDeserializer.Deserialize(toolConfigurationPath.Value);

            return new ToolConfigurationAndExecutableDirectory(
                toolConfiguration: toolConfiguration,
                executableDirectory: individualToolVersion.WithCombineFollowing(
                    packageId,
                    packageVersion,
                    "tools",
                    targetframework));
        }

        private static FilePath CreateTempProject(string packageId, string packageVersion, string targetframework,
            DirectoryPath individualToolVersion)
        {
            var tempProjectDirectory =
                new DirectoryPath(Path.GetTempPath()).WithCombineFollowing(Path.GetRandomFileName());
            EnsureDirectoryExists(tempProjectDirectory);
            var tempProjectPath =
                tempProjectDirectory.CreateFilePathWithCombineFollowing(Path.GetRandomFileName() + ".csproj");
            File.WriteAllText(tempProjectPath.Value,
                string.Format(TemporaryProjectTemplate,
                    targetframework, individualToolVersion.Value, packageId, packageVersion));
            return tempProjectPath;
        }

        private DirectoryPath CreateIndividualToolVersionDirectory(string packageId, string packageVersion)
        {
            EnsureDirectoryExists(_toolsPath);
            var individualTool = _toolsPath.WithCombineFollowing(packageId);
            EnsureDirectoryExists(individualTool);
            var individualToolVersion = individualTool.WithCombineFollowing(packageVersion);
            EnsureDirectoryExists(individualToolVersion);
            return individualToolVersion;
        }

        private static void EnsureDirectoryExists(DirectoryPath path)
        {
            if (!Directory.Exists(path.Value))
            {
                Directory.CreateDirectory(path.Value);
            }
        }

        private const string TemporaryProjectTemplate = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{0}</TargetFramework>
    <RestorePackagesPath>{1}</RestorePackagesPath>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""{2}"" Version=""{3}""/>    
  </ItemGroup>
</Project>";
    }
}
