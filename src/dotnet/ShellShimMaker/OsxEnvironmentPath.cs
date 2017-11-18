// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ShellShimMaker
{
    internal class OsxEnvironmentPath : IEnvironmentPath
    {
        private const string PathName = "PATH";
        private readonly string _packageExecutablePath;
        private readonly string _fullPackageExecutablePath;
        private const string PathDDotnetCliToolsPath = @"/etc/paths.d/dotnet-cli-tools";
        private readonly IFile _fileSystem;
        private readonly IEnvironmentProvider _environmentProvider;
        private readonly IReporter _reporter;

        public OsxEnvironmentPath(
            string packageExecutablePathWIthTilde, 
            string fullPackageExecutablePath, 
            IReporter reporter,
            IEnvironmentProvider environmentProvider, 
            IFile fileSystem
            )
        {
            _fullPackageExecutablePath = fullPackageExecutablePath ?? throw new ArgumentNullException(nameof(fullPackageExecutablePath));
            _packageExecutablePath = packageExecutablePathWIthTilde ?? throw new ArgumentNullException(nameof(packageExecutablePathWIthTilde));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _environmentProvider
                = environmentProvider ?? throw new ArgumentNullException(nameof(environmentProvider));
            _reporter
                = reporter ?? throw new ArgumentNullException(nameof(reporter));
        }

        public void AddPackageExecutablePathToUserPath()
        {
            if (PackageExecutablePathExists()) return;

            var script = $"{_packageExecutablePath}";
            File.WriteAllText(PathDDotnetCliToolsPath, script);
        }

        private bool PackageExecutablePathExists()
        {
            return Environment.GetEnvironmentVariable(PathName).Split(':').Contains(_packageExecutablePath) || 
                   Environment.GetEnvironmentVariable(PathName).Split(':').Contains(_fullPackageExecutablePath);
        }

        public void PrintAddPathInstructionIfPathDoesNotExist()
        {
            throw new NotImplementedException();
        }
    }
}
