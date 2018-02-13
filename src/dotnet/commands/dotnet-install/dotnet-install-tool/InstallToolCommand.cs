// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.ShellShim;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Tools.Install.Tool
{
    internal class InstallToolCommand : CommandBase
    {
        private readonly IToolPackageInstaller _toolPackageInstaller;
        private readonly IShellShimManager _shellShimManager;
        private readonly IEnvironmentPathInstruction _environmentPathInstruction;
        private readonly IReporter _reporter;
        private readonly IReporter _errorReporter;

        private readonly string _packageId;
        private readonly string _packageVersion;
        private readonly string _configFilePath;
        private readonly string _framework;
        private readonly string _source;
        private readonly bool _global;
        private readonly string _verbosity;

        public InstallToolCommand(
            AppliedOption appliedCommand,
            ParseResult parseResult,
            IToolPackageInstaller toolPackageInstaller = null,
            IShellShimManager shellShimManager = null,
            IEnvironmentPathInstruction environmentPathInstruction = null,
            IReporter reporter = null)
            : base(parseResult)
        {
            if (appliedCommand == null)
            {
                throw new ArgumentNullException(nameof(appliedCommand));
            }

            _packageId = appliedCommand.Arguments.Single();
            _packageVersion = appliedCommand.ValueOrDefault<string>("version");
            _configFilePath = appliedCommand.ValueOrDefault<string>("configfile");
            _framework = appliedCommand.ValueOrDefault<string>("framework");
            _source = appliedCommand.ValueOrDefault<string>("source");
            _global = appliedCommand.ValueOrDefault<bool>("global");
            _verbosity = appliedCommand.SingleArgumentOrDefault("verbosity");

            var cliFolderPathCalculator = new CliFolderPathCalculator();

            FilePath? configFile = null;
            if (_configFilePath != null)
            {
                configFile = new FilePath(_configFilePath);
            }

            _toolPackageInstaller = toolPackageInstaller
                ?? new ToolPackageInstaller(
                    new ToolPackageRepository(
                        new DirectoryPath(cliFolderPathCalculator.ToolsPackagePath)),
                    new ProjectRestorer(
                        configFile,
                        _source,
                        _verbosity,
                        _reporter));

            _environmentPathInstruction = environmentPathInstruction
                ?? EnvironmentPathFactory.CreateEnvironmentPathInstruction();

            _shellShimManager = shellShimManager ?? new ShellShimManager(
                new DirectoryPath(cliFolderPathCalculator.ToolsShimPath));

            _reporter = (reporter ?? Reporter.Output);
            _errorReporter = (reporter ?? Reporter.Error);
        }

        public override int Execute()
        {
            if (!_global)
            {
                throw new GracefulException(LocalizableStrings.InstallToolCommandOnlySupportGlobal);
            }

            if (_configFilePath != null && !File.Exists(_configFilePath))
            {
                throw new GracefulException(
                    string.Format(
                        LocalizableStrings.NuGetConfigurationFileDoesNotExist,
                        Path.GetFullPath(_configFilePath)));
            }

            // Prevent installation if any version of the package is installed
            if (_toolPackageInstaller.Repository.GetInstalledPackages(_packageId).FirstOrDefault() != null)
            {
                _errorReporter.WriteLine(string.Format(LocalizableStrings.ToolAlreadyInstalled, _packageId).Red());
                return 1;
            }

            try
            {
                IToolPackage package = null;
                using (var scope = new TransactionScope())
                {
                    package = _toolPackageInstaller.InstallPackage(
                        packageId: _packageId,
                        packageVersion: _packageVersion,
                        targetFramework: _framework);

                    foreach (var command in package.Commands)
                    {
                        _shellShimManager.CreateShim(command.Executable, command.Name);
                    }

                    scope.Complete();
                }

                _environmentPathInstruction.PrintAddPathInstructionIfPathDoesNotExist();

                _reporter.WriteLine(
                    string.Format(
                        LocalizableStrings.InstallationSucceeded,
                        string.Join(", ", package.Commands.Select(c => c.Name)),
                        package.PackageId,
                        package.PackageVersion).Green());
                return 0;
            }
            catch (ToolPackageException ex)
            {
                if (Reporter.IsVerbose)
                {
                    Reporter.Verbose.WriteLine(ex.ToString().Red());
                }

                _errorReporter.WriteLine(ex.Message.Red());
                _errorReporter.WriteLine(string.Format(LocalizableStrings.ToolInstallationFailed, _packageId).Red());
                return 1;
            }
            catch (ToolConfigurationException ex)
            {
                if (Reporter.IsVerbose)
                {
                    Reporter.Verbose.WriteLine(ex.ToString().Red());
                }

                _errorReporter.WriteLine(
                    string.Format(
                        LocalizableStrings.InvalidToolConfiguration,
                        ex.Message).Red());
                _errorReporter.WriteLine(string.Format(LocalizableStrings.ToolInstallationFailedContactAuthor, _packageId).Red());
                return 1;
            }
            catch (ShellShimException ex)
            {
                if (Reporter.IsVerbose)
                {
                    Reporter.Verbose.WriteLine(ex.ToString().Red());
                }

                _errorReporter.WriteLine(
                    string.Format(
                        LocalizableStrings.FailedToCreateToolShim,
                        _packageId,
                        ex.Message).Red());
                _errorReporter.WriteLine(string.Format(LocalizableStrings.ToolInstallationFailed, _packageId).Red());
                return 1;
            }
        }
    }
}
