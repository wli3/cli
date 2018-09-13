// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Frameworks;
using NuGet.Versioning;

// TODO wul no checkin move it to tools
// TODO remove dummy impl
namespace Microsoft.DotNet.Cli.ToolPackage
{
    internal class ManifestFileFinder : IManifestFileFinder
    {
        private IFileSystem _fileSystem;

        public ManifestFileFinder(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public IEnumerable<(PackageId, NuGetVersion, NuGetFramework)> GetPackages(FilePath? manifestFilePath = null)
        {
            return new (PackageId, NuGetVersion, NuGetFramework)[0];
        }
    }
}
