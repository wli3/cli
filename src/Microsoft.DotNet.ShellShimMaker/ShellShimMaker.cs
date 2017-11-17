// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ShellShimMaker
{
    public class ShellShimMaker
    {
        private readonly string _systemPathToPlaceShim;

        public ShellShimMaker(string systemPathToPlaceShim)
        {
            _systemPathToPlaceShim =
                systemPathToPlaceShim ?? throw new ArgumentNullException(nameof(systemPathToPlaceShim));
        }

        public void CreateShim(string packageExecutablePath, string shellCommandName)
        {
            var packageExecutable = new FilePath(packageExecutablePath);

            var script = new StringBuilder();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                script.AppendLine("@echo off");
                script.AppendLine($"dotnet {packageExecutable.ToEscapedString()} %*");
            }
            else
            {
                script.AppendLine("#!/bin/sh");
                script.AppendLine($"dotnet {packageExecutable.ToEscapedString()} \"$@\"");
            }

            var scriptPath = GetScriptPath(shellCommandName);
            try
            {
                File.WriteAllText(scriptPath.Value, script.ToString());
            }
            catch (UnauthorizedAccessException e)
            {
                throw new GracefulException(
                    string.Format(LocalizableStrings.InstallCommandUnauthorizedAccessMessage,
                        e.Message));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            
            var result = new CommandFactory()
                .Create("chmod", new[] {"u+x", scriptPath.Value})
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute();


            if (result.ExitCode != 0)
            {
                throw new GracefulException(
                    "Failed to change permission" +
                    $"{Environment.NewLine}error: " + result.StdErr +
                    $"{Environment.NewLine}output: " +
                    result.StdOut);
            }
        }

        public void Remove(string shellCommandName)
        {
            File.Delete(GetScriptPath(shellCommandName).Value);
        }

        private FilePath GetScriptPath(string shellCommandName)
        {
            var scriptPath = Path.Combine(_systemPathToPlaceShim, shellCommandName);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                scriptPath += ".cmd";
            }

            return new FilePath(scriptPath);
        }
    }
}
