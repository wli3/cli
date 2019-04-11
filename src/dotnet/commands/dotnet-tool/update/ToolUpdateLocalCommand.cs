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
using Microsoft.DotNet.Tools.Tool.Install;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Tools.Tool.Update
{
    internal class ToolUpdateLocalCommand : CommandBase
    {
        private readonly IToolManifestFinder _toolManifestFinder;
        private readonly IToolManifestEditor _toolManifestEditor;
        private readonly ILocalToolsResolverCache _localToolsResolverCache;
        private readonly IToolPackageInstaller _toolPackageInstaller;
        private readonly ToolInstallLocalInstaller _toolLocalPackageInstaller;
        private readonly IReporter _reporter;

        private readonly PackageId _packageId;
        private readonly string _packageVersion;
        private readonly string _configFilePath;
        private readonly string[] _sources;
        private readonly string _verbosity;
        private readonly string _explicitManifestFile;

        public ToolUpdateLocalCommand(
            AppliedOption appliedCommand,
            ParseResult parseResult,
            IToolPackageInstaller toolPackageInstaller = null,
            IToolManifestFinder toolManifestFinder = null,
            IToolManifestEditor toolManifestEditor = null,
            ILocalToolsResolverCache localToolsResolverCache = null,
            IReporter reporter = null)
            : base(parseResult)
        {
            if (appliedCommand == null)
            {
                throw new ArgumentNullException(nameof(appliedCommand));
            }

            _packageId = new PackageId(appliedCommand.Arguments.Single());
            _packageVersion = appliedCommand.ValueOrDefault<string>("version");
            _configFilePath = appliedCommand.ValueOrDefault<string>("configfile");
            _sources = appliedCommand.ValueOrDefault<string[]>("add-source");
            _verbosity = appliedCommand.SingleArgumentOrDefault("verbosity");
            _explicitManifestFile = appliedCommand.SingleArgumentOrDefault(ToolAppliedOption.ToolManifest);

            _reporter = (reporter ?? Reporter.Output);

            if (toolPackageInstaller == null)
            {
                (IToolPackageStore,
                    IToolPackageStoreQuery,
                    IToolPackageInstaller installer) toolPackageStoresAndInstaller
                        = ToolPackageFactory.CreateToolPackageStoresAndInstaller(
                            additionalRestoreArguments: appliedCommand.OptionValuesToBeForwarded());
                _toolPackageInstaller = toolPackageStoresAndInstaller.installer;
            }
            else
            {
                _toolPackageInstaller = toolPackageInstaller;
            }

            _toolManifestFinder = toolManifestFinder ??
                                  new ToolManifestFinder(new DirectoryPath(Directory.GetCurrentDirectory()));
            _toolManifestEditor = toolManifestEditor ?? new ToolManifestEditor();
            _localToolsResolverCache = localToolsResolverCache ?? new LocalToolsResolverCache();
            _toolLocalPackageInstaller = new ToolInstallLocalInstaller(appliedCommand, toolPackageInstaller);
        }

        public override int Execute()
        {
            (FilePath manifestFile, string warningMessag) = FindManifestFile();

            var toolDownloadedPackage = _toolLocalPackageInstaller.Install(manifestFile);
            var existingPackage =
                _toolManifestFinder
                .Find(manifestFile)
                .Single(p => p.PackageId.Equals(_packageId));

            if (existingPackage.Version > toolDownloadedPackage.Version)
            {
                throw new GracefulException(string.Format(
                    LocalizableStrings.UpdateLocaToolToLowerVersion,
                    toolDownloadedPackage.Version.ToNormalizedString(),
                    existingPackage.Version.ToNormalizedString(),
                    manifestFile.Value));
            }

            if (existingPackage.Version != toolDownloadedPackage.Version)
            {
                _toolManifestEditor.Edit(
                manifestFile,
                _packageId,
                toolDownloadedPackage.Version,
                toolDownloadedPackage.Commands.Select(c => c.Name).ToArray());
            }

            _localToolsResolverCache.SaveToolPackage(
                toolDownloadedPackage,
                _toolLocalPackageInstaller.TargetFrameworkToInstall);

            if (warningMessag != null)
            {
                _reporter.WriteLine(warningMessag.Yellow());
            }

            if (existingPackage.Version == toolDownloadedPackage.Version)
            {
                _reporter.WriteLine(
                   string.Format(
                       LocalizableStrings.UpdateLocaToolSucceededVersionNoChange,
                       toolDownloadedPackage.Id,
                       existingPackage.Version.ToNormalizedString(),
                       manifestFile.Value));
            }
            else
            {
                _reporter.WriteLine(
                   string.Format(
                       LocalizableStrings.UpdateLocalToolSucceeded,
                       toolDownloadedPackage.Id,
                       existingPackage.Version.ToNormalizedString(),
                       toolDownloadedPackage.Version.ToNormalizedString(),
                       manifestFile.Value).Green());
            }

            return 0;
        }

        private (FilePath filePath, string warningMessage) FindManifestFile()
        {
            if (!string.IsNullOrWhiteSpace(_explicitManifestFile))
            {
                return (new FilePath(_explicitManifestFile), null);
            }

            var manifestFilesContainPackageId
                = _toolManifestFinder.FindContainPackageId(_packageId);

            if (manifestFilesContainPackageId.Any())
            {
                string warning = null;
                if (manifestFilesContainPackageId.Count > 1)
                {
                    warning =
                        string.Format(
                            LocalizableStrings.SamePackageIdInOtherManifestFile,
                            string.Join(
                                Environment.NewLine,
                                manifestFilesContainPackageId.Skip(1).Select(m => $"\t{m}")));
                }

                return (manifestFilesContainPackageId.First(), warning);
            }

            return (_toolManifestFinder.FindFirst(), null);
        }
    }
}
