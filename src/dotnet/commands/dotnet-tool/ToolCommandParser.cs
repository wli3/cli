// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.CommandLine;
using LocalizableStrings = Microsoft.DotNet.Tools.Install.LocalizableStrings;

namespace Microsoft.DotNet.Cli
{
    internal static class ToolCommandParser
    {
        public static Command Tool()
        {
            return Create.Command(
                "tool",
                LocalizableStrings.CommandDescription,
                Accept.NoArguments(),
                CommonOptions.HelpOption(),
                InstallToolCommandParser.InstallTool()); // TODO fix to more than install tool
        }
    }
}
