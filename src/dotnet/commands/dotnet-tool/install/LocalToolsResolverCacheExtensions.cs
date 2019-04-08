// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ToolManifest;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools.Tool.Common;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace Microsoft.DotNet.Tools.Tool.Install
{
    internal static class LocalToolsResolverCacheExtensions
    {
        public static void SaveToolPackage(
            this ILocalToolsResolverCache _localToolsResolverCache,
            IToolPackage toolDownloadedPackage,
            string targetFrameworkToInstall)
        {
            if (_localToolsResolverCache == null)
            {
                throw new ArgumentNullException(nameof(_localToolsResolverCache));
            }

            if (toolDownloadedPackage == null)
            {
                throw new ArgumentNullException(nameof(toolDownloadedPackage));
            }

            if (string.IsNullOrWhiteSpace(targetFrameworkToInstall))
            {
                throw new ArgumentException("targetFrameworkToInstall cannot be null or whitespace", nameof(targetFrameworkToInstall));
            }

            foreach (var restoredCommand in toolDownloadedPackage.Commands)
            {
                _localToolsResolverCache.Save(
                    new Dictionary<RestoredCommandIdentifier, RestoredCommand>
                    {
                        [new RestoredCommandIdentifier(
                                toolDownloadedPackage.Id,
                                toolDownloadedPackage.Version,
                                NuGetFramework.Parse(targetFrameworkToInstall),
                                Constants.AnyRid,
                                restoredCommand.Name)] =
                            restoredCommand
                    });
            }
        }
    }
}
