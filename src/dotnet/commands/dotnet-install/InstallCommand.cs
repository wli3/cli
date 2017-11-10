// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Configurer;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Cli
{
    public class InstallCommand
    {
        public static int Run(string[] args)
        {
            var parser = Parser.Instance;
            var result = parser.ParseFrom("dotnet install", args);

            var parseResult = result["dotnet"]["install"]["globaltool"];

            var packageId = parseResult.Arguments.Single();
            var packageVersion = parseResult.ValueOrDefault<string>("version");

            FilePath configFile = null;
            var configFilePath = parseResult.ValueOrDefault<string>("configfile");
            if (string.IsNullOrWhiteSpace(configFilePath))
            {
                configFile = new FilePath(parseResult.ValueOrDefault<string>("configfile"));
            }

            if (packageVersion == null)
            {
                throw new NotImplementedException("Auto look up work in progress");
            }

            var executablePackagePath = new DirectoryPath(new CliFolderPathCalculator().ExecutablePackagesPath);
            var executablePackageObtainer =
                new ExecutablePackageObtainer.ExecutablePackageObtainer(
                    executablePackagePath);

            var toolConfigurationAndExecutableDirectory = executablePackageObtainer.ObtainAndReturnExecutablePath(
                packageId: packageId,
                packageVersion: packageVersion,
                nugetconfig: configFile,
                targetframework: "netcoreapp2.0");


            var executable = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .CreateFilePathWithCombineFollowing(
                    toolConfigurationAndExecutableDirectory
                    .Configuration
                    .ToolAssemblyEntryPoint);


            var shellShimMaker = new ShellShimMaker.ShellShimMaker(executablePackagePath.Value);
            shellShimMaker.CreateShim(executable.Value, toolConfigurationAndExecutableDirectory.Configuration.CommandName);

            return 0;
        }
    }
}
