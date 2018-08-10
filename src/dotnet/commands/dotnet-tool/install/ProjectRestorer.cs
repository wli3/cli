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

namespace Microsoft.DotNet.Tools.Tool.Install
{
    internal class ProjectRestorer : IProjectRestorer
    {
        private const string AnyRid = "any";
        private readonly IReporter _reporter;
        private readonly IReporter _errorReporter;
        private readonly bool _forceOutputRedirection;

        public ProjectRestorer(IReporter reporter = null)
        {
            _reporter = reporter ?? Reporter.Output;
            _errorReporter = reporter ?? Reporter.Error;
            _forceOutputRedirection = reporter != null;
        }

        public void Restore(FilePath project,
            PackageLocation packageLocation,
            string verbosity = null)
        {
            var argsToPassToRestore = new List<string>();

            argsToPassToRestore.Add(project.Value);

            argsToPassToRestore.AddRange(new List<string>
            {
                "--runtime",
                AnyRid
            });

            argsToPassToRestore.AddRange(ToRestoreArguments(packageLocation));

            argsToPassToRestore.Add($"-verbosity:{verbosity ?? "quiet"}");

            var command = new DotNetCommandFactory(alwaysRunOutOfProc: true)
                .Create("restore", argsToPassToRestore);

            if (verbosity == null || _forceOutputRedirection)
            {
                command = command
                    .OnOutputLine(line => WriteLine(_reporter, line, project))
                    .OnErrorLine(line => WriteLine(_errorReporter, line, project));
            }

            var result = command.Execute();
            if (result.ExitCode != 0)
            {
                throw new ToolPackageException(LocalizableStrings.ToolInstallationRestoreFailed);
            }
        }

        public static List<string> ToRestoreArguments(PackageLocation packageLocation)
        {
            var args = new List<string>();

            if (packageLocation.NugetConfig != null)
            {
                args.Add("--configfile");
                args.Add(packageLocation.NugetConfig.Value.Value);
            }

            if (packageLocation.DisableParallel)
            {
                args.Add("--disable-parallel");
            }

            if (packageLocation.NoCache)
            {
                args.Add("--no-cache");
            }

            if (packageLocation.IgnoreFailedSources)
            {
                args.Add("--ignore-failed-sources");
            }

            return args;
        }

        private static void WriteLine(IReporter reporter, string line, FilePath project)
        {
            line = line ?? "";

            // Remove the temp project prefix if present
            if (line.StartsWith($"{project.Value} : ", StringComparison.OrdinalIgnoreCase))
            {
                line = line.Substring(project.Value.Length + 3);
            }

            // Note: MSBuild intentionally does not localize "warning" and "error" for diagnostic messages
            if (line.StartsWith("warning ", StringComparison.Ordinal))
            {
                line = line.Yellow();
            }
            else if (line.StartsWith("error ", StringComparison.Ordinal))
            {
                line = line.Red();
            }

            reporter.WriteLine(line);
        }
    }
}
