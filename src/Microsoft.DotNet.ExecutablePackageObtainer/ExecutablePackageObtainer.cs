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
        private readonly Func<FilePath> _getTempProjectPath;
        private readonly DirectoryPath _toolsPath;

        public ExecutablePackageObtainer(DirectoryPath toolsPath, Func<FilePath> getTempProjectPath)
        {
            _getTempProjectPath = getTempProjectPath;
            _toolsPath = toolsPath ?? throw new ArgumentNullException(nameof(toolsPath));
        }

        public ToolConfigurationAndExecutableDirectory ObtainAndReturnExecutablePath(
            string packageId,
            string packageVersion,
            FilePath nugetconfig = null,
            string targetframework = null)
        {
            if (packageId == null) throw new ArgumentNullException(nameof(packageId));
            if (packageVersion == null) throw new ArgumentNullException(nameof(packageVersion));
            if (nugetconfig == null) throw new ArgumentNullException(nameof(nugetconfig));
            if (targetframework == null) throw new ArgumentNullException(nameof(targetframework));

            var individualToolVersion = CreateIndividualToolVersionDirectory(packageId, packageVersion);
            
            var tempProjectPath = CreateTempProject(packageId, packageVersion, targetframework, individualToolVersion);

            InvokeRestore(nugetconfig, tempProjectPath, individualToolVersion);

            var toolConfiguration = GetConfiguration(packageId, packageVersion, individualToolVersion);

            return new ToolConfigurationAndExecutableDirectory(
                toolConfiguration: toolConfiguration,
                executableDirectory: individualToolVersion.WithCombineFollowing(
                    packageId,
                    packageVersion,
                    "tools",
                    targetframework));
        }

        private static ToolConfiguration GetConfiguration(string packageId, string packageVersion,
            DirectoryPath individualToolVersion)
        {
            var toolConfigurationPath = individualToolVersion
                .WithCombineFollowing(packageId, packageVersion, "tools")
                .CreateFilePathWithCombineFollowing("DotnetToolsConfig.xml");

            var toolConfiguration = ToolConfigurationDeserializer.Deserialize(toolConfigurationPath.Value);
            return toolConfiguration;
        }

        private void InvokeRestore(FilePath nugetconfig, FilePath tempProjectPath, DirectoryPath individualToolVersion)
        {
            var comamnd = new CommandFactory()
                .Create(
                    "dotnet",
                    new[]
                    {
                        "restore",
                        "--runtime", RuntimeEnvironment.GetRuntimeIdentifier(),
                        "--configfile", nugetconfig.ToEscapedString(),
                        $"/p:BaseIntermediateOutputPath={individualToolVersion.ToEscapedString()}", 
                    })
                .WorkingDirectory(tempProjectPath.GetDirectoryPath().Value)
                .CaptureStdOut()
                .CaptureStdErr();

            var result = comamnd.Execute();
            if (result.ExitCode != 0)
            {
                throw new PackageObtainException("Failed to restore package. " +
                                                 "WorkingDirectory: " +
                                                 result.StartInfo.WorkingDirectory + "Arguments: " +
                                                 result.StartInfo.Arguments + "Output: " +
                                                 result.StdErr + result.StdOut);
            }
        }

        private FilePath CreateTempProject(string packageId, string packageVersion, string targetframework,
            DirectoryPath individualToolVersion)
        {
            var tempProjectPath = _getTempProjectPath();
            if (Path.GetExtension(tempProjectPath.Value) != "csproj")
            {
                tempProjectPath = new FilePath(Path.ChangeExtension(tempProjectPath.Value, "csproj"));
            }

            EnsureDirectoryExists(tempProjectPath.GetDirectoryPath());
            File.WriteAllText(tempProjectPath.Value,
                string.Format(
                    TemporaryProjectTemplate,
                    targetframework,
                    individualToolVersion.Value,
                    packageId,
                    packageVersion));
            return tempProjectPath;
        }

        private DirectoryPath CreateIndividualToolVersionDirectory(string packageId, string packageVersion)
        {
            var individualTool = _toolsPath.WithCombineFollowing(packageId);
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
    <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""{2}"" Version=""{3}""/>    
  </ItemGroup>
</Project>";
    }
}
