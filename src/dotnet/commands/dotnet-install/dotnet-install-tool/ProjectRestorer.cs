// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Tools.Install.Tool
{
    internal class ProjectRestorer : IProjectRestorer
    {
        private readonly FilePath? _nugetConfig;
        private readonly string _source;
        private readonly string _verbosity;
        private readonly IReporter _reporter;

        public ProjectRestorer(
            FilePath? nugetConfig = null,
            string source = null,
            string verbosity = null,
            IReporter reporter = null)
        {
            _nugetConfig = nugetConfig;
            _source = source;
            _verbosity = verbosity;
            _reporter = reporter;
        }

        public void Restore(FilePath project, DirectoryPath assetJsonOutput)
        {
            var argsToPassToRestore = new List<string>();

            argsToPassToRestore.Add(project.Value);
            if (_nugetConfig != null)
            {
                argsToPassToRestore.Add("--configfile");
                argsToPassToRestore.Add(_nugetConfig.Value.Value);
            }

            if (_source != null)
            {
                argsToPassToRestore.Add("--source");
                argsToPassToRestore.Add(_source);
            }

            argsToPassToRestore.AddRange(new List<string>
            {
                "--runtime",
                GetRuntimeIdentifierWithMacOsHighSierraFallback(),
                $"/p:BaseIntermediateOutputPath={assetJsonOutput.ToQuotedString()}"
            });

            argsToPassToRestore.Add($"/verbosity:{_verbosity ?? "quiet"}");

            var command = new DotNetCommandFactory(alwaysRunOutOfProc: true)
                .Create("restore", argsToPassToRestore);

            if (_reporter != null)
            {
                command = command
                    .OnOutputLine((line) => _reporter.WriteLine(line))
                    .OnErrorLine((line) => _reporter.WriteLine(line));
            }

            var result = command.Execute();
            if (result.ExitCode != 0)
            {
                throw new ToolPackageException(LocalizableStrings.ToolInstallationRestoreFailed);
            }
        }

        // walk around for https://github.com/dotnet/corefx/issues/26488
        // fallback osx.10.13 to osx
        private static string GetRuntimeIdentifierWithMacOsHighSierraFallback()
        {
            if (RuntimeEnvironment.GetRuntimeIdentifier() == "osx.10.13-x64")
            {
                return "osx-x64";
            }

            return RuntimeEnvironment.GetRuntimeIdentifier();
        }
    }
}
