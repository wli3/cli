// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.ToolPackage;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace Microsoft.DotNet.Tools.Tool.Restore
{
    internal class ToolRestoreCommand : CommandBase
    {
        private readonly string _configFilePath;
        private readonly IReporter _errorReporter;
        private readonly ILocalToolsResolverCache _localToolsResolverCache;
        private readonly IManifestFileFinder _manifestFileFinder;
        private readonly DirectoryPath _nugetGlobalPackagesFolder;
        private readonly AppliedOption _options;
        private readonly IReporter _reporter;
        private readonly string[] _source;
        private readonly IToolPackageInstaller _toolPackageInstaller;
        private readonly string _verbosity;

        public ToolRestoreCommand(
            AppliedOption appliedCommand,
            ParseResult result,
            IToolPackageInstaller toolPackageInstaller,
            IManifestFileFinder manifestFileFinder,
            ILocalToolsResolverCache localToolsResolverCache,
            DirectoryPath nugetGlobalPackagesFolder,
            IReporter reporter)
            : base(result)
        {
            _options = appliedCommand ?? throw new ArgumentNullException(nameof(appliedCommand));
            _toolPackageInstaller =
                toolPackageInstaller ?? throw new ArgumentNullException(nameof(toolPackageInstaller));
            _manifestFileFinder = manifestFileFinder ?? throw new ArgumentNullException(nameof(manifestFileFinder));
            _localToolsResolverCache = localToolsResolverCache ??
                                       throw new ArgumentNullException(nameof(localToolsResolverCache));
            _nugetGlobalPackagesFolder = nugetGlobalPackagesFolder;
            _reporter = reporter ?? Reporter.Output;
            _errorReporter = reporter ?? Reporter.Error;

            _configFilePath = appliedCommand.ValueOrDefault<string>("configfile");
            _source = appliedCommand.ValueOrDefault<string[]>("add-source");
            _verbosity = appliedCommand.SingleArgumentOrDefault("verbosity");
        }

        public override int Execute()
        {
            string customFile = _options.Arguments.SingleOrDefault();
            FilePath? customManifestFileLocation;
            if (customFile != null)
                customManifestFileLocation = new FilePath(customFile);
            else
                customManifestFileLocation = null;

            FilePath? configFile = null;
            if (_configFilePath != null) configFile = new FilePath(_configFilePath);

            IEnumerable<(PackageId packageId, NuGetVersion version, NuGetFramework targetframework)> packagesToRestore =
                _manifestFileFinder.GetPackages(customManifestFileLocation);

            Dictionary<RestoredCommandIdentifier, RestoredCommand> dictionary =
                new Dictionary<RestoredCommandIdentifier, RestoredCommand>();
            foreach ((PackageId packageId, NuGetVersion version, NuGetFramework targetframework) p in packagesToRestore)
            {
                string targetFramework = p.targetframework?.GetShortFolderName() ?? BundledTargetFramework
                                             .GetTargetFrameworkMoniker();

                IToolPackage toolPackage =
                    _toolPackageInstaller.InstallPackageToExternalManagedLocation(
                        new PackageLocation(
                            nugetConfig: configFile,
                            additionalFeeds: _source),
                        p.packageId, ToVersionRangeWithOnlyOneVersion(p.version), targetFramework,
                        verbosity: _verbosity);

                foreach (RestoredCommand command in toolPackage.Commands)
                    dictionary.Add(
                        new RestoredCommandIdentifier(
                            toolPackage.Id,
                            toolPackage.Version,
                            NuGetFramework.Parse(targetFramework),
                            "any",
                            command.Name),
                        command);
            }

            string[] errors = dictionary
                .Select(pair => (PackageId: pair.Key.PackageId, CommandName: pair.Key.CommandName))
                .GroupBy(t => t.CommandName)
                .Where(g => g.Count() > 1)
                .Select(aa =>
                    $"Packages {JoinBySpaceWithQuote(aa.Select(a => a.PackageId.ToString()))} have a command with the same name {JoinBySpaceWithQuote(aa.Select(a => a.CommandName.ToString()))} regardless of the casing.")
                .ToArray();

            if (errors.Any()) throw new ToolPackageException(string.Join(Environment.NewLine, errors));

            _localToolsResolverCache.Save(dictionary, _nugetGlobalPackagesFolder);

            return 0;
        }

        private string JoinBySpaceWithQuote(IEnumerable<object> objects)
        {
            return string.Join(", ", objects.Select(o => $"\"{o.ToString()}\""));
        }

        private static VersionRange ToVersionRangeWithOnlyOneVersion(NuGetVersion version)
        {
            return new VersionRange(
                version,
                includeMinVersion: true,
                maxVersion: version,
                includeMaxVersion: true);
        }
    }
}
