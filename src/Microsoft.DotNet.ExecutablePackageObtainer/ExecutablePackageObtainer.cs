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
            EnsureDirExists(_toolsPath);
            var individualTool = _toolsPath.WithCombineFollowing(packageId);
            EnsureDirExists(individualTool);
            var individualToolVersion = _toolsPath.WithCombineFollowing(packageVersion);
            EnsureDirExists(individualToolVersion);

            InvokeRestore(targetframework, nugetconfig, packageId,packageVersion, individualToolVersion);
            return new ToolConfigurationAndExecutableDirectory(
                toolConfiguration: new ToolConfiguration("a", "consoleappababab.dll"), //TODO hard code no checkin
                executableDirectory: individualToolVersion.WithCombineFollowing($"{packageId}.{packageVersion}", "tools",
                    targetframework));
        }

        private void InvokeRestore(string targetframework, FilePath nugetconfig, string packageId, string packageVersion,
            DirectoryPath restoreDirectory)
        {
            // Create temp project to restore the tool
            var restoreTargetFramework = targetframework;
            var tempProjectDirectory = new DirectoryPath(Path.GetTempPath()).WithCombineFollowing( Path.GetRandomFileName());
            EnsureDirExists(tempProjectDirectory);
            var tempProjectPath = tempProjectDirectory.CreateFilePath(Path.GetRandomFileName() + ".csproj");

            Debug.WriteLine("Temp path: " + tempProjectPath.ToEscapedString());
            File.WriteAllText(tempProjectPath.Value,
                string.Format(ProjectTemplate,
                    restoreTargetFramework, restoreDirectory.Value, packageId, packageVersion));

            var comamnd = _commandFactory.Create(
                "restore",
                new[]
                {
                     "--runtime", RuntimeEnvironment.GetRuntimeIdentifier(),
                     "--configfile", nugetconfig.ToEscapedString()
                })
                    .WorkingDirectory(tempProjectDirectory.Value)
                    .CaptureStdOut()
                    .CaptureStdErr();
            
            var result  = comamnd.Execute();
            if (result.ExitCode != 0)
            {
                throw new Exception(result.StdErr + result.StdOut);
            }
        }

        private static void EnsureDirExists(DirectoryPath path)
        {
            if (!Directory.Exists(path.Value))
            {
                Directory.CreateDirectory(path.Value);
            }
        }

        private const string ProjectTemplate = @"<Project Sdk=""Microsoft.NET.Sdk"">
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
