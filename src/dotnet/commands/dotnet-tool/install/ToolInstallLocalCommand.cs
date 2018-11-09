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
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Versioning;

namespace Microsoft.DotNet.Tools.Tool.Install
{
    internal class ToolInstallLocalCommand : CommandBase
    {
        private readonly IToolManifestFinder _toolManifestFinder;
        private readonly IToolManifestEditor _toolManifestEditor;
        private readonly IFileSystem _fileSystem;
        private readonly DirectoryPath _nugetGlobalPackagesFolder;
        private readonly ILocalToolsResolverCache _localToolsResolverCache;
        private readonly IToolPackageInstaller _toolPackageInstaller;
        private readonly IReporter _reporter;
        private readonly IReporter _errorReporter;

        private readonly PackageId _packageId;
        private readonly string _packageVersion;
        private readonly string _configFilePath;
        private readonly string _framework;
        private readonly string[] _sources;
        private readonly string _verbosity;
        private readonly string _explicitManifestFile;

        public ToolInstallLocalCommand(
            AppliedOption appliedCommand,
            ParseResult parseResult,
            IToolPackageInstaller toolPackageInstaller = null,
            IToolManifestFinder toolManifestFinder = null,
            IToolManifestEditor toolManifestEditor = null,
            ILocalToolsResolverCache localToolsResolverCache = null,
            IFileSystem fileSystem = null,
            DirectoryPath? nugetGlobalPackagesFolder = null,
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
            _framework = appliedCommand.ValueOrDefault<string>("framework");
            _sources = appliedCommand.ValueOrDefault<string[]>("add-source");
            _verbosity = appliedCommand.SingleArgumentOrDefault("verbosity");
            _explicitManifestFile = appliedCommand.SingleArgumentOrDefault("--tool-manifest");

            _reporter = (reporter ?? Reporter.Output);
            _errorReporter = (reporter ?? Reporter.Error);

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

            _toolManifestFinder = toolManifestFinder ?? new ToolManifestFinder(new DirectoryPath(Directory.GetCurrentDirectory()));
            _toolManifestEditor = toolManifestEditor ?? new ToolManifestEditor();
            _fileSystem = fileSystem ?? new FileSystemWrapper();
            _localToolsResolverCache = localToolsResolverCache ?? new LocalToolsResolverCache();
            _nugetGlobalPackagesFolder =
                nugetGlobalPackagesFolder ?? new DirectoryPath(NuGetGlobalPackagesFolder.GetLocation());
        }

        public override int Execute()
        {
            if (_configFilePath != null && !File.Exists(_configFilePath))
            {
                throw new GracefulException(
                    string.Format(
                        LocalizableStrings.NuGetConfigurationFileDoesNotExist,
                        Path.GetFullPath(_configFilePath)));
            }

            VersionRange versionRange = null;
            if (!string.IsNullOrEmpty(_packageVersion) && !VersionRange.TryParse(_packageVersion, out versionRange))
            {
                throw new GracefulException(
                    string.Format(
                        LocalizableStrings.InvalidNuGetVersionRange,
                        _packageVersion));
            }

            FilePath? configFile = null;
            if (_configFilePath != null)
            {
                configFile = new FilePath(_configFilePath);
            }

            string targetFramework = BundledTargetFramework.GetTargetFrameworkMoniker();

            var manfiestFile = _toolManifestFinder.FindFirst();

            IToolPackage toolPackage =
                   _toolPackageInstaller.InstallPackageToExternalManagedLocation(
                       new PackageLocation(
                           nugetConfig: configFile,
                           additionalFeeds: _sources,
                           rootConfigDirectory: manfiestFile.GetDirectoryPath()),
                       _packageId,
                       versionRange,
                       targetFramework,
                       verbosity: _verbosity);

            _toolManifestEditor.Add(
                manfiestFile,
                toolPackage.Id,
                toolPackage.Version,
                toolPackage.Commands.Select(c => c.Name).ToArray());

            _localToolsResolverCache.Save(new Dictionary(), _nugetGlobalPackagesFolder);

            return 1;
        }
    }
}
