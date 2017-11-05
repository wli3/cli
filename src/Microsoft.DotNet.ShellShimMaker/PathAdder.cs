// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.ShellShimMaker
{
    public class PathAdder : IPathAdder
    {
        private const string PathName = "PATH";
        private readonly string _packageExecutablePath;

        public PathAdder(string packageExecutablePath)
        {
            _packageExecutablePath = packageExecutablePath;
        }

        public void AddPackageExecutablePathToUserPath()
        {
            var existingPath = Environment.GetEnvironmentVariable(PathName);

            if (existingPath.Split(';').Contains(_packageExecutablePath))
            {
                Environment.SetEnvironmentVariable(
                    PathName,
                    $"{existingPath};{_packageExecutablePath}",
                    EnvironmentVariableTarget.User);
            }
        }
    }
}
