// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.DotNet.Cli.CommandLine;

namespace Microsoft.DotNet.Cli
{
    internal static class InstallCommandParser
    {
        public static Command Install() =>
            Create.Command(
                "install", "",
                Accept.NoArguments(), CommonOptions.HelpOption(), InstallGlobaltool());
        
        public static Command InstallGlobaltool() =>
            Create.Command("globaltool",
                "Install globaltool",
                Accept.ExactlyOneArgument(o => "packageId")
                    .With(name: "packageId",
                        description: "Package Id in Nuget"),
                Create.Option(
                    "--version",
                    "TODO loc no check in Package version of the package in Nuget",
                    Accept.ExactlyOneArgument()),
                Create.Option(
                    "--configfile",
                    "TODO loc no check in Nuget config file",
                    Accept.ExactlyOneArgument()),
                CommonOptions.HelpOption());
    }
}
