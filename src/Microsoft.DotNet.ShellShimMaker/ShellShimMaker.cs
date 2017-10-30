// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ShellShimMaker
{
    public class ShellShimMaker
    {
        private readonly string _systemPathToPlaceShim;

        public ShellShimMaker(string systemPathToPlaceShim)
        {
            _systemPathToPlaceShim = systemPathToPlaceShim ?? throw new ArgumentNullException(nameof(systemPathToPlaceShim));
        }

        public void CreateShim(FileInfo packageExecutablePath, string shellCommandName)
        {
            var script = new StringBuilder();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                script.AppendLine("@echo off");
                script.AppendLine($"dotnet exec {packageExecutablePath} %*");
            }
            else
            {
                throw new NotImplementedException("unix work in progress");
            }

            File.WriteAllText($"{GetScriptPath(shellCommandName)}", script.ToString());
        }

        public void Remove(string shellCommandName)
        {
            File.Delete(GetScriptPath(shellCommandName));
        }

        private string GetScriptPath(string shellCommandName)
        {
            var scriptPath = Path.Combine(_systemPathToPlaceShim, shellCommandName);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                scriptPath += ".cmd";
            }
            else
            {
                throw new NotImplementedException("unix work in progress");
            }

            return scriptPath;
        }
    }
}
