using System.Collections.Generic;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace Microsoft.DotNet.Cli.ToolPackage
{
    internal interface IManifestFileFinder
    {
        IEnumerable<(PackageId packageId, 
            NuGetVersion version, 
            NuGetFramework targetframework)> GetPackages(FilePath? manifestFilePath = null);
    }
}
