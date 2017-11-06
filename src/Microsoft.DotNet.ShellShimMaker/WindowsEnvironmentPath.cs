// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.ShellShimMaker
{
    public class WindowsEnvironmentPath : IEnvironmentPath
    {
        private const string PathName = "PATH";
        private readonly string _packageExecutablePath;

        public WindowsEnvironmentPath(string packageExecutablePath)
        {
            _packageExecutablePath = packageExecutablePath;
        }

        public void AddPackageExecutablePathToUserPath()
        {
            if (PackageExecutablePathExists()) return;

            var existingUserEnvPath = Environment.GetEnvironmentVariable(PathName, EnvironmentVariableTarget.User);

            Environment.SetEnvironmentVariable(
                PathName,
                $"{existingUserEnvPath};{_packageExecutablePath}",
                EnvironmentVariableTarget.User);
        }

        private bool PackageExecutablePathExists()
        {
            return Environment.GetEnvironmentVariable(PathName).Split(';').Contains(_packageExecutablePath);
        }
    }
}
