// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.CommandLine;

namespace Microsoft.DotNet.Cli
{
    internal static class InstallCommandParser
    {
        public static Command Install()
        {
            return Create.Command(
                "install", "",
                Accept.NoArguments(), CommonOptions.HelpOption(), InstallGlobaltool());
        }

        private static Command InstallGlobaltool()
        {
            return Create.Command("globaltool",
                "Install globaltool",
                Accept.ExactlyOneArgument(o => "packageId")
                    .With(name: "packageId",
                        description: "Package Id in Nuget"),
                Create.Option(
                    "--version",
                    "Package version of the package in Nuget",
                    Accept.ExactlyOneArgument()),
                Create.Option(
                    "--configfile",
                    "Nuget config file",
                    Accept.ExactlyOneArgument()),
                Create.Option(
                    "-f|--framework",
                    "Target Framework Moniker of tools",
                    Accept.ExactlyOneArgument()),
                CommonOptions.HelpOption());
        }
    }
}
