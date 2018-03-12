// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.CommandLine;
using LocalizableStrings = Microsoft.DotNet.Tools.Uninstall.Tool.LocalizableStrings;

namespace Microsoft.DotNet.Cli
{
    internal static class UpdateToolCommandParser
    {
        public static Command Upadte()
        {
            return Create.Command("tool",
                "LocalizableStrings.CommandDescription", //todo wul
                Accept.ExactlyOneArgument(errorMessage: o => "LocalizableStrings.SpecifyExactlyOnePackageId") //todo wul
                    .With(name: "LocalizableStrings.PackageIdArgumentName", //todo wul
                          description: "LocalizableStrings.PackageIdArgumentDescription"), //todo wul
                Create.Option(
                    "-g|--global",
                    "LocalizableStrings.GlobalOptionDescription", //todo wul
                    Accept.NoArguments()),
                CommonOptions.HelpOption());
        }
    }
}
