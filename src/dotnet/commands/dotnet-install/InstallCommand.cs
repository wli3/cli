// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Configurer;

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

            if (packageVersion == null)
            {
                throw new NotImplementedException("Auto look up work in progress");
            }

            var executablePackagesPath = new CliFolderPathCalculator().ExecutablePackagesPath;
            var executablePackageObtainer =
                new ExecutablePackageObtainer.ExecutablePackageObtainer(
                    executablePackagesPath);
            var executablePath = executablePackageObtainer.ObtainAndReturnExecutablePath(packageId, packageVersion);

            var shellShimMaker = new ShellShimMaker.ShellShimMaker(executablePackagesPath);
            shellShimMaker.CreateShim(executablePath, packageId);

            return 0;
        }
    }
}
