// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.ShellShimMaker
{
    public class PathAdder : IPathAdder
    {
        private readonly string _packageExecutablePath;

        public PathAdder(string packageExecutablePath)
        {
            _packageExecutablePath = packageExecutablePath;
        }

        public void AddPackageExecutablePathToUserPath()
        {
            const string pathName = "PATH";
            var existingPath = Environment.GetEnvironmentVariable(pathName);

            Environment.SetEnvironmentVariable(pathName,
                $"{existingPath};{_packageExecutablePath}",
                EnvironmentVariableTarget.User);
        }
    }
}
