// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.CommandLine;
using LocalizableStrings = Microsoft.DotNet.Tools.Uninstall.LocalizableStrings;

namespace Microsoft.DotNet.Cli
{
    internal static class UpdateCommandParser
    {
        public static Command Update()
        {
            return Create.Command(
                "update",
                "LocalizableStrings.CommandDescription", // TODO wul
                Accept.NoArguments(),
                CommonOptions.HelpOption(),
                UpdateToolCommandParser.Update());
        }
    }
}
