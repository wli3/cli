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

        public ToolConfigurationAndExecutableDirectory ObtainAndReturnExecutablePath(string packageId, 
            string packageVersion,
            FilePath nugetconfig, 
            string targetframework)
        {

            run(targetframework);
            return new ToolConfigurationAndExecutableDirectory(
                toolConfiguration: new ToolConfiguration("a", "b"),
                executableDirectory: _toolsPath.WithCombineFollowing($"{packageId}.{packageVersion}", "lib", "netcoreapp2.0"));
        }

        private void run(string targetframework, FilePath nugetconfig)
        {

            EnsureToolboxDirExists();

            // Create temp project to restore the tool
            var restoreTargetFramework = targetframework;
            var tempProjectDirectory = Path.GetTempPath();
            var tempProjectPath = tempProjectDirectory + Path.GetTempFileName() + ".csproj";
            Debug.WriteLine("Temp path: " + tempProjectPath);
            File.WriteAllText(tempProjectPath , 
                string.Format(DefaultProject, 
                    restoreTargetFramework));

            var comamnd = _commandFactory.Create("restore", new []{"--runtime", RuntimeEnvironment.GetRuntimeIdentifier()}, configuration: "release");
            comamnd.WorkingDirectory(tempProjectPath);

        }
        
        private void EnsureToolboxDirExists()
        {
            if (!Directory.Exists(_toolsPath.Value))
            {
                Directory.CreateDirectory(_toolsPath.Value);
            }
        }
        
        public string DefaultProject = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{0}</TargetFramework>
  </PropertyGroup>
</Project>";

    }
}
