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

            EnsureDirExists(_toolsPath);
            var individualTool = _toolsPath.WithCombineFollowing(packageId);
            EnsureDirExists(individualTool);
            var individualToolVersion = _toolsPath.WithCombineFollowing(packageId);
            EnsureDirExists(individualToolVersion);
            
            InvokeRestore(targetframework, nugetconfig, packageId, individualToolVersion);
            return new ToolConfigurationAndExecutableDirectory(
                toolConfiguration: new ToolConfiguration("a", "b"),
                executableDirectory: _toolsPath.WithCombineFollowing($"{packageId}.{packageVersion}", "lib", "netcoreapp2.0"));
        }

        private void InvokeRestore(string targetframework, FilePath nugetconfig, string packageId, DirectoryPath restoreDirectory)
        {

            // Create temp project to restore the tool
            var restoreTargetFramework = targetframework;
            var tempProjectDirectory = new DirectoryPath(Path.GetTempPath());
            var tempProjectPath = tempProjectDirectory.CreateFilePath(Path.GetTempFileName() + ".csproj") ;
                        
            Debug.WriteLine("Temp path: " + tempProjectPath.ToEscapedString());
            File.WriteAllText(tempProjectPath.Value , 
                string.Format(ProjectTemplate, 
                    restoreTargetFramework, restoreDirectory.ToEscapedString()));

            var addPackageComamnd = _commandFactory.Create(
                "add", 
                new []{
                    tempProjectPath.ToEscapedString(), 
                    "package", packageId}, 
                configuration: "release");
            addPackageComamnd.WorkingDirectory(tempProjectPath.Value);
            addPackageComamnd.Execute();
            
            var comamnd = _commandFactory.Create(
                "restore", 
                new []{"--runtime", RuntimeEnvironment.GetRuntimeIdentifier(), 
                    "--configfile", nugetconfig.ToEscapedString()}, 
                configuration: "release");
            comamnd.WorkingDirectory(tempProjectPath.Value);
            comamnd.Execute();
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
</Project>";
    }
}
