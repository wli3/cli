// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Cli.CommandInfrastructure
{
    public class CommandAvailableAsDotNetToolException : GracefulException
    {
        public CommandAvailableAsDotNetToolException(string commandName) : base(GetMessage(commandName))
        {
        }

        public CommandAvailableAsDotNetToolException(string commandName, Exception innerException) : base(
            GetMessage(commandName), innerException)
        {
        }

        private static string GetMessage(string commandName)
        {
            var commandRemoveLeadingDotnet = commandName.Replace("dotnet-", string.Empty);
            var packageName = "dotnet-" + commandRemoveLeadingDotnet.ToLower();

            return string.Format(LocalizableStrings.CannotFindCommandAvailableAsTool,
                commandRemoveLeadingDotnet,
                packageName);
        }
    }
}
