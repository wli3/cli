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
        private readonly Lazy<string> _bundledTargetFrameworkMoniker;
        private readonly DirectoryPath _toolsPath;

        public ExecutablePackageObtainer(
            DirectoryPath toolsPath,
            Func<FilePath> getTempProjectPath,
            Lazy<string> bundledTargetFrameworkMoniker)
        {
            _getTempProjectPath = getTempProjectPath;
            _bundledTargetFrameworkMoniker = bundledTargetFrameworkMoniker;
            _toolsPath = toolsPath ?? throw new ArgumentNullException(nameof(toolsPath));
        }

        public ToolConfigurationAndExecutableDirectory ObtainAndReturnExecutablePath(
            string packageId,
            string packageVersion = null,
            FilePath nugetconfig = null,
            string targetframework = null)
        {
            if (packageId == null) throw new ArgumentNullException(nameof(packageId));
            if (targetframework == null)
            {
                targetframework = _bundledTargetFrameworkMoniker.Value;
            }

            PackageVersion packageVersionOrPlaceHolder = new PackageVersion(packageVersion);

            var individualToolVersion =
                CreateIndividualToolVersionDirectory(packageId, packageVersionOrPlaceHolder);

            var tempProjectPath = CreateTempProject(
                packageId,
                packageVersionOrPlaceHolder,
                targetframework,
                individualToolVersion);

            if (packageVersionOrPlaceHolder.IsPlaceHolder)
            {
                InvokeAddPackageRestore(
                    nugetconfig,
                    tempProjectPath,
                    individualToolVersion,
                    packageId);
            }

            InvokeRestore(nugetconfig, tempProjectPath, individualToolVersion);

            if (packageVersionOrPlaceHolder.IsPlaceHolder)
            {
                var concreteVersion =
                    new DirectoryInfo(
                        Directory.GetDirectories(
                            individualToolVersion.WithCombineFollowing(packageId).Value).Single()).Name;
                var concreteVersionIndividualToolVersion =
                    individualToolVersion.GetParentPath().WithCombineFollowing(concreteVersion);
                Directory.Move(individualToolVersion.Value, concreteVersionIndividualToolVersion.Value);

                individualToolVersion = concreteVersionIndividualToolVersion;
                packageVersion = concreteVersion;
            }

            var toolConfiguration = GetConfiguration(packageId, packageVersion, individualToolVersion);

            return new ToolConfigurationAndExecutableDirectory(
                toolConfiguration: toolConfiguration,
                executableDirectory: individualToolVersion.WithCombineFollowing(
                    packageId,
                    packageVersion,
                    "tools",
                    targetframework));
        }

        private static ToolConfiguration GetConfiguration(
            string packageId,
            string packageVersion,
            DirectoryPath individualToolVersion)
        {
            var toolConfigurationPath =
                individualToolVersion
                    .WithCombineFollowing(packageId, packageVersion, "tools")
                    .CreateFilePathWithCombineFollowing("DotnetToolsConfig.xml");

            var toolConfiguration
                = ToolConfigurationDeserializer.Deserialize(toolConfigurationPath.Value);
            return toolConfiguration;
        }

        private void InvokeRestore(
            FilePath nugetconfig,
            FilePath tempProjectPath,
            DirectoryPath individualToolVersion)
        {
            var argsToPassToRestore = new List<string>();
            argsToPassToRestore.Add("restore");
            if (nugetconfig != null)
            {
                argsToPassToRestore.Add("--configfile");
                argsToPassToRestore.Add(nugetconfig.ToEscapedString());
            }

            argsToPassToRestore.AddRange(new List<string>
            {
                "--runtime",
                RuntimeEnvironment.GetRuntimeIdentifier(),
                $"/p:BaseIntermediateOutputPath={individualToolVersion.ToEscapedString()}"
            });

            var command = new CommandFactory()
                .Create(
                    "dotnet",
                    argsToPassToRestore)
                .WorkingDirectory(tempProjectPath.GetDirectoryPath().Value)
                .CaptureStdOut()
                .CaptureStdErr();

            var result = command.Execute();
            if (result.ExitCode != 0)
            {
                throw new PackageObtainException("Failed to restore package. " +
                                                 "WorkingDirectory: " +
                                                 result.StartInfo.WorkingDirectory + "Arguments: " +
                                                 result.StartInfo.Arguments + "Output: " +
                                                 result.StdErr + result.StdOut);
            }
        }

        private FilePath CreateTempProject(
            string packageId,
            PackageVersion packageVersion,
            string targetframework,
            DirectoryPath individualToolVersion)
        {
            var tempProjectPath = _getTempProjectPath();
            if (Path.GetExtension(tempProjectPath.Value) != "csproj")
            {
                tempProjectPath = new FilePath(Path.ChangeExtension(tempProjectPath.Value, "csproj"));
            }

            EnsureDirectoryExists(tempProjectPath.GetDirectoryPath());
            if (packageVersion.IsConcreteValue)
            {
                File.WriteAllText(tempProjectPath.Value,
                    string.Format(
                        TemporaryProjectTemplate,
                        targetframework,
                        individualToolVersion.Value,
                        packageId,
                        packageVersion.Value));
            }
            else
            {
                File.WriteAllText(tempProjectPath.Value,
                    string.Format(
                        TemporaryProjectTemplateWithoutPackage,
                        targetframework,
                        individualToolVersion.Value));
            }
            return tempProjectPath;
        }

        private static void InvokeAddPackageRestore(
            FilePath nugetconfig,
            FilePath tempProjectPath,
            DirectoryPath individualToolVersion,
            string packageId)
        {
            if (nugetconfig != null)
            {
                File.Copy(
                    nugetconfig.Value,
                    tempProjectPath
                        .GetDirectoryPath()
                        .CreateFilePathWithCombineFollowing("nuget.config")
                        .Value);
            }

            var argsToPassToRestore = new List<string>
            {
                "add",
                tempProjectPath.Value,
                "package",
                packageId,
                "--no-restore"
            };

            var command = new CommandFactory()
                .Create(
                    "dotnet",
                    argsToPassToRestore)
                .WorkingDirectory(tempProjectPath.GetDirectoryPath().Value)
                .CaptureStdOut()
                .CaptureStdErr();

            var result = command.Execute();
            if (result.ExitCode != 0)
            {
                throw new PackageObtainException("Failed to add package. " +
                                                 "WorkingDirectory: " +
                                                 result.StartInfo.WorkingDirectory + "Arguments: " +
                                                 result.StartInfo.Arguments + "Output: " +
                                                 result.StdErr + result.StdOut);
            }
        }

        private DirectoryPath CreateIndividualToolVersionDirectory(
            string packageId,
            PackageVersion packageVersion)
        {
            var individualTool = _toolsPath.WithCombineFollowing(packageId);
            var individualToolVersion = individualTool.WithCombineFollowing(packageVersion.Value);
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

        private const string TemporaryProjectTemplateWithoutPackage = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{0}</TargetFramework>
    <RestorePackagesPath>{1}</RestorePackagesPath>
    <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>
  </PropertyGroup>
</Project>";

        private class PackageVersion
        {
            public bool IsPlaceHolder { get; }
            public string Value { get; }
            public bool IsConcreteValue => !IsPlaceHolder;

            public PackageVersion(string value, bool isPlaceHolder)
            {
                IsPlaceHolder = isPlaceHolder;
                Value = value;
            }

            public PackageVersion(string packageVersion)
            {
                if (packageVersion == null)
                {
                    Value = Path.GetRandomFileName();
                    IsPlaceHolder = true;
                }
                else
                {
                    Value = packageVersion;
                    IsPlaceHolder = false;
                }
            }
        }
    }
}
