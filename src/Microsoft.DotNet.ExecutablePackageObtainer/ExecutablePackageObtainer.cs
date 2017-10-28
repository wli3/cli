// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ExecutablePackageObtainer
{
    public class ExecutablePackageObtainer
    {
        private readonly string _toolsPath;

        public ExecutablePackageObtainer(string toolsPath)
        {
            _toolsPath = toolsPath ?? throw new ArgumentNullException(nameof(toolsPath));
        }

        public string ObtainAndReturnExecutablePath(string packageId, 
            string packageVersion)
        {
            var result = new ProcessStartInfo
            {
                FileName = "nuget.exe",
                Arguments = $"install {packageId} -version {packageVersion} -OutputDirectory {_toolsPath}",
                UseShellExecute = false
            };
            
            var process = new Process
            {
                StartInfo = result
            };

            process.Start();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new Exception("Nuget install failed");
            }
            return Path.Combine(_toolsPath, $"{packageId}.{packageVersion}", "lib", "netcoreapp2.0", "consoleappababab.dll"); // TODO how to get the dll name?? Metadata
        }
    }
}
