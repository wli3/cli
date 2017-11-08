// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.ExecutablePackageObtainer
{
    public class ToolConfiguration
    {
        public ToolConfiguration(
            string commandName,
            string toolAssemblyEntryPoint,
            EntryPointType entryPointType)
        {
            CommandName = 
                commandName ?? throw new ArgumentNullException(nameof(commandName));
            ToolAssemblyEntryPoint =
                toolAssemblyEntryPoint ?? throw new ArgumentNullException(nameof(toolAssemblyEntryPoint));
            EntryPointType = entryPointType;
        }

        public string CommandName { get; }
        public string ToolAssemblyEntryPoint { get; }
        public EntryPointType EntryPointType { get; }
    }

    public enum EntryPointType
    {
        DotnetNetCoreAssembly,
        NativeBinary,
        Script,
    }
}
