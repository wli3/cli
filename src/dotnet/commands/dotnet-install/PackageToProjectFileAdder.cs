// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.ExecutablePackageObtainer;
using Microsoft.DotNet.Tools.Add;
using Microsoft.DotNet.Tools.NuGet;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Cli
{
    internal class PackageToProjectFileAdder : ICanAddPackageToProjectFile
    {
        public void Add(FilePath projectPath, string packageId)
        {
            var result = NuGetCommand.Run(new[]
            {
                "package",
                "add",
                "--package",
                packageId,
                "--project",
                projectPath.Value,
                "--no-restore"
            });
            try
            {
                
            }
            catch (GracefulException e)
            {
                throw new PackageObtainException("Failed to add package. ", innerException:e);
            }

            if (result != 0)
            {
                throw new PackageObtainException($"Failed to add package. Exit code {result}");
            }
        }
    }
}
