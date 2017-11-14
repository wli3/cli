// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.ExecutablePackageObtainer
{
    public class ToolConfiguration
    {
        public ToolConfiguration(
            string commandName,
            string toolAssemblyEntryPoint)
        {

            if (string.IsNullOrWhiteSpace(commandName))
            {
                throw new ArgumentNullException(paramName: nameof(commandName), message: "Cannot be null or whitespace");
            }

            // https://stackoverflow.com/questions/1976007/what-characters-are-forbidden-in-windows-and-linux-directory-names
            char[] invalidCharactors = new char[] { '/', '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
            if (commandName.IndexOfAny(invalidCharactors) != -1)
            {
                throw new ArgumentException(paramName: nameof(toolAssemblyEntryPoint), message: "Cannot contain following character " + new string(invalidCharactors));
            }

            if (string.IsNullOrWhiteSpace(toolAssemblyEntryPoint))
            {
                throw new ArgumentNullException(paramName: nameof(toolAssemblyEntryPoint), message: "Cannot be null or whitespace");
            }

            CommandName = commandName;
            ToolAssemblyEntryPoint = toolAssemblyEntryPoint;
        }

        public string CommandName { get; }
        public string ToolAssemblyEntryPoint { get; }
    }

    public class ToolConfigurationException : ArgumentException
    {
        public ToolConfigurationException(string message) : base(message)
        {
        }
    }
}
