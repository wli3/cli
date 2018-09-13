using System.Collections.Generic;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace Microsoft.DotNet.Cli.ToolPackage
{
    internal interface IManifestFileFinder
    {
        IEnumerable<(PackageId, NuGetVersion, NuGetFramework)> GetPackages(FilePath? manifestFilePath = null);
    }
}