// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.ToolManifest;
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
        private readonly IToolManifestFinder _toolManifestFinder;
        private readonly DirectoryPath _nugetGlobalPackagesFolder;
        private readonly AppliedOption _options;
        private readonly IFileSystem _fileSystem;
        private readonly IReporter _reporter;
        private readonly string[] _sources;
        private readonly IToolPackageInstaller _toolPackageInstaller;
        private readonly string _verbosity;

        public ToolRestoreCommand(
            AppliedOption appliedCommand,
            ParseResult result,
            IToolPackageInstaller toolPackageInstaller = null,
            IToolManifestFinder toolManifestFinder = null,
            ILocalToolsResolverCache localToolsResolverCache = null,
            IFileSystem fileSystem = null,
            DirectoryPath? nugetGlobalPackagesFolder = null,
            IReporter reporter = null)
            : base(result)
        {
            _options = appliedCommand ?? throw new ArgumentNullException(nameof(appliedCommand));

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

            _toolManifestFinder
                = toolManifestFinder
                  ?? new ToolManifestFinder(new DirectoryPath(Directory.GetCurrentDirectory()));

            _localToolsResolverCache = localToolsResolverCache ?? new LocalToolsResolverCache();
            _fileSystem = fileSystem ?? new FileSystemWrapper();

            _nugetGlobalPackagesFolder =
                nugetGlobalPackagesFolder ?? new DirectoryPath(NuGetGlobalPackagesFolder.GetLocation());
            _reporter = reporter ?? Reporter.Output;
            _errorReporter = reporter ?? Reporter.Error;

            _configFilePath = appliedCommand.ValueOrDefault<string>("configfile");
            _sources = appliedCommand.ValueOrDefault<string[]>("add-source");
            _verbosity = appliedCommand.SingleArgumentOrDefault("verbosity");
        }

        public override int Execute()
        {
            FilePath? customManifestFileLocation = GetCustomManifestFileLocation();

            FilePath? configFile = null;
            if (_configFilePath != null)
            {
                configFile = new FilePath(_configFilePath);
            }

            IReadOnlyCollection<ToolManifestPackage> packagesFromManifest;
            try
            {
                packagesFromManifest = _toolManifestFinder.Find(customManifestFileLocation);
            }
            catch (ToolManifestCannotBeFoundException e)
            {
                _reporter.WriteLine(e.Message.Yellow());
                return 0;
            }

            var succeeded = new ConcurrentDictionary<RestoredCommandIdentifier, RestoredCommand>();
            var exceptions = new ConcurrentDictionary<PackageId, ToolPackageException>();
            var errorMessages = new ConcurrentQueue<string>();
            var successMessages = new ConcurrentQueue<string>();

            Parallel.ForEach(packagesFromManifest,
                package =>
                {
                    InstallPackages(
                        package,
                        configFile,
                        succeeded,
                        exceptions,
                        errorMessages,
                        successMessages);
                });

            EnsureNoCommandNameCollision(succeeded);

            _localToolsResolverCache.Save(succeeded, _nugetGlobalPackagesFolder);

            return PrintConclusionAndReturn(succeeded.Any(), exceptions, errorMessages, successMessages);
        }

        private void InstallPackages(
            ToolManifestPackage package,
            FilePath? configFile,
            ConcurrentDictionary<RestoredCommandIdentifier, RestoredCommand> dictionary,
            ConcurrentDictionary<PackageId, ToolPackageException> toolPackageExceptions,
            ConcurrentQueue<string> errorMessages,
            ConcurrentQueue<string> successMessages)
        {
            string targetFramework = BundledTargetFramework.GetTargetFrameworkMoniker();

            if (PackageHasBeenRestored(package, targetFramework))
            {
                successMessages.Enqueue(string.Format(
                    LocalizableStrings.RestoreSuccessful, package.PackageId,
                    package.Version.ToNormalizedString(), string.Join(", ", package.CommandNames)));
                return;
            }

            try
            {
                IToolPackage toolPackage =
                    _toolPackageInstaller.InstallPackageToExternalManagedLocation(
                        new PackageLocation(
                            nugetConfig: configFile,
                            additionalFeeds: _sources,
                            rootConfigDirectory: package.FirstAffectDirectory),
                        package.PackageId, ToVersionRangeWithOnlyOneVersion(package.Version), targetFramework,
                        verbosity: _verbosity);

                if (!ManifestCommandMatchesActualInPackage(package.CommandNames, toolPackage.Commands))
                {
                    errorMessages.Enqueue(
                        string.Format(LocalizableStrings.CommandsMismatch,
                            JoinBySpaceWithQuote(package.CommandNames.Select(c => c.Value.ToString())),
                            package.PackageId,
                            JoinBySpaceWithQuote(toolPackage.Commands.Select(c => c.Name.ToString()))));
                }

                foreach (RestoredCommand command in toolPackage.Commands)
                {
                    var successReturn = dictionary.TryAdd(
                        new RestoredCommandIdentifier(
                            toolPackage.Id,
                            toolPackage.Version,
                            NuGetFramework.Parse(targetFramework),
                            Constants.AnyRid,
                            command.Name),
                        command);

                    AssertNoFalseAddingToDictionary(dictionary, command, successReturn);
                }

                successMessages.Enqueue(string.Format(
                    LocalizableStrings.RestoreSuccessful, package.PackageId,
                    package.Version.ToNormalizedString(), string.Join(" ", package.CommandNames)));
            }
            catch (ToolPackageException e)
            {
                toolPackageExceptions.TryAdd(package.PackageId, e);
            }
        }

        private static void AssertNoFalseAddingToDictionary(
            ConcurrentDictionary<RestoredCommandIdentifier, RestoredCommand> dictionary,
            RestoredCommand command,
            bool successReturn)
        {
            if (successReturn == false)
            {
                throw new InvalidOperationException(
                    $"Failed to add {command.DebugToString()} to " +
                    $"{string.Join(";", dictionary.Values.Select(k => k.DebugToString()))}");
            }
        }

        private int PrintConclusionAndReturn(
            bool anySuccess,
            ConcurrentDictionary<PackageId, ToolPackageException> toolPackageExceptions,
            ConcurrentQueue<string> errorMessages,
            ConcurrentQueue<string> successMessages)
        {
            if (toolPackageExceptions.Any() || errorMessages.Any())
            {
                _reporter.WriteLine(Environment.NewLine);
                _errorReporter.WriteLine(string.Join(
                                        Environment.NewLine,
                                        CreateErrorMessage(toolPackageExceptions).Concat(errorMessages)).Red());

                _reporter.WriteLine(Environment.NewLine);

                _reporter.WriteLine(string.Join(Environment.NewLine, successMessages));
                _errorReporter.WriteLine(Environment.NewLine +
                    (anySuccess
                    ? LocalizableStrings.RestorePartiallyFailed
                    : LocalizableStrings.RestoreFailed).Red());

                return 1;
            }
            else
            {
                _reporter.WriteLine(string.Join(Environment.NewLine, successMessages));
                _reporter.WriteLine(Environment.NewLine);
                _reporter.WriteLine(LocalizableStrings.LocalToolsRestoreWasSuccessful.Green());

                return 0;
            }
        }

        private static IEnumerable<string> CreateErrorMessage(
            ConcurrentDictionary<PackageId, ToolPackageException> toolPackageExceptions)
        {
            return toolPackageExceptions.Select(p =>
                string.Format(LocalizableStrings.PackageFailedToRestore,
                    p.Key.ToString(), p.Value.ToString()));
        }

        private static bool ManifestCommandMatchesActualInPackage(
            ToolCommandName[] commandsFromManifest,
            IReadOnlyList<RestoredCommand> toolPackageCommands)
        {
            ToolCommandName[] commandsFromPackage = toolPackageCommands.Select(t => t.Name).ToArray();
            foreach (var command in commandsFromManifest)
            {
                if (!commandsFromPackage.Contains(command))
                {
                    return false;
                }
            }

            foreach (var command in commandsFromPackage)
            {
                if (!commandsFromManifest.Contains(command))
                {
                    return false;
                }
            }

            return true;
        }

        private bool PackageHasBeenRestored(
            ToolManifestPackage package,
            string targetFramework)
        {
            var sampleRestoredCommandIdentifierOfThePackage = new RestoredCommandIdentifier(
                package.PackageId,
                package.Version,
                NuGetFramework.Parse(targetFramework),
                Constants.AnyRid,
                package.CommandNames.First());

            return _localToolsResolverCache.TryLoad(
                       sampleRestoredCommandIdentifierOfThePackage,
                       _nugetGlobalPackagesFolder,
                       out var restoredCommand)
                   && _fileSystem.File.Exists(restoredCommand.Executable.Value);
        }

        private FilePath? GetCustomManifestFileLocation()
        {
            string customFile = _options.ValueOrDefault<string>("tool-manifest");
            FilePath? customManifestFileLocation;
            if (customFile != null)
            {
                customManifestFileLocation = new FilePath(customFile);
            }
            else
            {
                customManifestFileLocation = null;
            }

            return customManifestFileLocation;
        }

        private void EnsureNoCommandNameCollision(ConcurrentDictionary<RestoredCommandIdentifier, RestoredCommand> dictionary)
        {
            string[] errors = dictionary
                .Select(pair => (PackageId: pair.Key.PackageId, CommandName: pair.Key.CommandName))
                .GroupBy(packageIdAndCommandName => packageIdAndCommandName.CommandName)
                .Where(grouped => grouped.Count() > 1)
                .Select(nonUniquePackageIdAndCommandNames =>
                    string.Format(LocalizableStrings.PackagesCommandNameCollisionConclusion,
                        string.Join(Environment.NewLine,
                            nonUniquePackageIdAndCommandNames.Select(
                                p => "\t" + string.Format(
                                    LocalizableStrings.PackagesCommandNameCollisionForOnePackage,
                                    p.CommandName.Value,
                                    p.PackageId.ToString())))))
                .ToArray();

            if (errors.Any())
            {
                throw new ToolPackageException(string.Join(Environment.NewLine, errors));
            }
        }

        private static string JoinBySpaceWithQuote(IEnumerable<object> objects)
        {
            return string.Join(" ", objects.Select(o => $"\"{o.ToString()}\""));
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
