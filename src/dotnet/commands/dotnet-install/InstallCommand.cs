// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.Cli.Telemetry;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Cli
{
    public class InstallCommand
    {
        internal const string TelemetrySessionIdEnvironmentVariableName = "DOTNET_CLI_TELEMETRY_SESSIONID";

        public static int Run(string[] args)
        {
            var parser = Parser.Instance;
            var result = parser.ParseFrom("dotnet install", args);

            var parseResult = result["dotnet"]["install"]["globaltool"];

            var packageId = parseResult.Arguments.Single();
            var packageVersion = parseResult.AppliedOptions["package-version"].Arguments.Single();
            if (packageVersion == null)
            {
                throw new NotImplementedException("Auto look up work in progress");
            }

            var executablePackageObtainer = new ExecutablePackageObtainer.ExecutablePackageObtainer(new CliFolderPathCalculator().ExecutablePackagesPath);
            var executablePath =  executablePackageObtainer.ObtainAndReturnExecutablePath(packageId, packageVersion);

            var shellShimMaker = new ShellShimMaker.ShellShimMaker(Path.GetDirectoryName(new Muxer().MuxerPath));
            shellShimMaker.CreateShim(executablePath, packageId);
            
            return 0;
        }

    }
}
