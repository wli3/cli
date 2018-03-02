// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.Tools.Install.Tool;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ToolPackage
{
    internal static class ToolPackageFactory
    {
        private const string NameOfNestedPackageDirectory = ".store";

        public static (IToolPackageStore, IToolPackageInstaller) CreateToolPackageStoreAndInstaller(
            DirectoryPath? nonGlobalLocation = null)
        {
            IToolPackageStore toolPackageStore = CreateToolPackageStore(nonGlobalLocation);
            var toolPackageInstaller = new ToolPackageInstaller(
                toolPackageStore,
                new ProjectRestorer(Reporter.Output));

            return (toolPackageStore, toolPackageInstaller);
        }

        public static IToolPackageStore CreateToolPackageStore(
            DirectoryPath? nonGlobalLocation = null)
        {
            var toolPackageStore =
                new ToolPackageStore(GetPackageLocation(nonGlobalLocation));

            return toolPackageStore;
        }

        private static DirectoryPath GetPackageLocation(DirectoryPath? nonGlobalLocation)
        {
            DirectoryPath packageLocation;

            if (nonGlobalLocation.HasValue)
            {
                packageLocation = nonGlobalLocation.Value.WithSubDirectories(NameOfNestedPackageDirectory);
            }
            else
            {
                var cliFolderPathCalculator = new CliFolderPathCalculator();
                packageLocation = new DirectoryPath(cliFolderPathCalculator.ToolsPackagePath);
            }

            return packageLocation;
        }
    }
}
