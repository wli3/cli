// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.ShellShimMaker;
using Microsoft.DotNet.ToolPackageObtainer;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Cli
{
    public class InstallCommand
    {
        public static int Run(string[] args)
        {
            CommandLine.Parser parser = Parser.Instance;
            ParseResult result = parser.ParseFrom("dotnet install", args);

            AppliedOption parseResult = result["dotnet"]["install"]["globaltool"];

            var packageId = parseResult.Arguments.Single();
            var packageVersion = parseResult.ValueOrDefault<string>("version");

            FilePath? configFile = null;

            var configFilePath = parseResult.ValueOrDefault<string>("configfile");
            if (configFilePath != null)
            {
                configFile = new FilePath(configFilePath);
            }

            var framework = parseResult.ValueOrDefault<string>("framework");

            var executablePackagePath = new DirectoryPath(new CliFolderPathCalculator().ExecutablePackagesPath);
            var toolPackageObtainer =
                new ToolPackageObtainer.ToolPackageObtainer(
                    executablePackagePath,
                    () => new DirectoryPath(Path.GetTempPath())
                        .WithSubDirectories(Path.GetRandomFileName())
                        .WithFile(Path.GetRandomFileName() + ".csproj"),
                    new Lazy<string>(BundledTargetFramework.GetTargetFrameworkMoniker),
                    new PackageToProjectFileAdder(),
                    new ProjectRestorer());

            ToolConfigurationAndExecutableDirectory toolConfigurationAndExecutableDirectory =
                toolPackageObtainer.ObtainAndReturnExecutablePath(
                    packageId: packageId,
                    packageVersion: packageVersion,
                    nugetconfig: configFile,
                    targetframework: framework);


            DirectoryPath executable = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .WithSubDirectories(
                    toolConfigurationAndExecutableDirectory
                        .Configuration
                        .ToolAssemblyEntryPoint);


            var shellShimMaker = new ShellShimMaker.ShellShimMaker(executablePackagePath.Value);

            shellShimMaker.EnsureCommandNameUniqueness(
                toolConfigurationAndExecutableDirectory.Configuration.CommandName);

            shellShimMaker.CreateShim(
                executable.Value,
                toolConfigurationAndExecutableDirectory.Configuration.CommandName);

            EnvironmentPathFactory
                .CreateEnvironmentPathInstruction()
                .PrintAddPathInstructionIfPathDoesNotExist();

            return 0;
        }
    }
}
