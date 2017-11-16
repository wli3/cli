// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Mono.Unix;
using Mono.Unix.Native;

namespace Microsoft.DotNet.ShellShimMaker
{
    public class LinuxEnvironmentPath : IEnvironmentPath
    {
        private const string PathName = "PATH";
        private readonly string _packageExecutablePath;
        private const string ProfiledDotnetCliToolsPath = @"/etc/profile.d/dotnet-cli-tools-bin-path.sh";

        public LinuxEnvironmentPath(string packageExecutablePath)
        {
            _packageExecutablePath = packageExecutablePath;
        }

        public void AddPackageExecutablePathToUserPath()
        {
            if (PackageExecutablePathExists()) return;

            var script = $"export PATH=\"$PATH:{_packageExecutablePath}\"";
            File.WriteAllText(ProfiledDotnetCliToolsPath, script);
        }

        private bool PackageExecutablePathExists()
        {
            return Environment.GetEnvironmentVariable(PathName).Split(':').Contains(_packageExecutablePath);
        }
    }
}
