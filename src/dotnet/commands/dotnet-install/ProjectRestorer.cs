// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.ExecutablePackageObtainer;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Cli
{


    internal class ProjectRestorer : ICanRestoreProject
    {
        public void Restore(
            FilePath tempProjectPath,
            DirectoryPath assetJsonOutput, 
            FilePath nugetconfig)
        {
            var argsToPassToRestore = new List<string>();
            argsToPassToRestore.Add("restore");

            argsToPassToRestore.Add(tempProjectPath.ToEscapedString());
            if (nugetconfig != null)
            {
                argsToPassToRestore.Add("--configfile");
                argsToPassToRestore.Add(nugetconfig.ToEscapedString());
            }

            argsToPassToRestore.AddRange(new List<string>
            {
                "--runtime",
                RuntimeEnvironment.GetRuntimeIdentifier(),
                $"/p:BaseIntermediateOutputPath={assetJsonOutput.ToEscapedString()}"
            });

            var command = new CommandFactory()
                .Create(
                    "dotnet",
                    argsToPassToRestore)
                .CaptureStdOut()
                .CaptureStdErr();

            var result = command.Execute();
            if (result.ExitCode != 0)
            {
                throw new PackageObtainException("Failed to restore package. " +
                                                 "WorkingDirectory: " +
                                                 result.StartInfo.WorkingDirectory + "Arguments: " +
                                                 result.StartInfo.Arguments + "Output: " +
                                                 result.StdErr + result.StdOut);
            }
        }

    }
}
