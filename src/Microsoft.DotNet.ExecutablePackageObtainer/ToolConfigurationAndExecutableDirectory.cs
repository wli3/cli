// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ExecutablePackageObtainer
{
    public class ToolConfigurationAndExecutableDirectory
    {
        public ToolConfiguration Configuration { get; }
        public DirectoryPath ExecutableDirectory { get; }

        public ToolConfigurationAndExecutableDirectory(ToolConfiguration toolConfiguration, DirectoryPath executableDirectory)
        {
            Configuration = toolConfiguration;
            ExecutableDirectory = executableDirectory;
        }
    }
}
