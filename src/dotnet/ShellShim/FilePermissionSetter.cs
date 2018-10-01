﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Microsoft.DotNet.CommandInfrastructure;

namespace Microsoft.DotNet.ShellShim
{
    internal class FilePermissionSetter : IFilePermissionSetter
    {
        public void SetUserExecutionPermission(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            CommandResult result = new CommandFactory()
                .Create("chmod", new[] { "u+x", path })
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute();

            if (result.ExitCode != 0)
            {
                throw new FilePermissionSettingException(result.StdErr);
            }
        }
    }
}
