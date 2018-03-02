﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Configurer;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ShellShim
{
    internal class ShellShimRepositoryFactory : IShellShimRepositoryFactory
    {
        public IShellShimRepository CreateShellShimRepository(DirectoryPath? nonGlobalLocation = null)
        {
            return new ShellShimRepository(GetShimLocation(nonGlobalLocation));
        }

        private static DirectoryPath GetShimLocation(DirectoryPath? nonGlobalLocation)
        {
            DirectoryPath packageLocation;

            if (nonGlobalLocation.HasValue)
            {
                packageLocation = nonGlobalLocation.Value;
            }
            else
            {
                var cliFolderPathCalculator = new CliFolderPathCalculator();
                packageLocation = new DirectoryPath(cliFolderPathCalculator.ToolsShimPath);
            }

            return packageLocation;
        }
    }
}
