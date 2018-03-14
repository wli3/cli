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

namespace Microsoft.DotNet.Tools.Update.Tool
{
    internal delegate IShellShimRepository CreateShellShimRepository(DirectoryPath? nonGlobalLocation = null);

    internal delegate (IToolPackageStore, IToolPackageInstaller) CreateToolPackageStoreAndInstaller(
        DirectoryPath? nonGlobalLocation = null);

    internal class UpdateToolCommand : CommandBase
    {
        private readonly IReporter _reporter;
        private readonly IReporter _errorReporter;
        private readonly CreateShellShimRepository _createShellShimRepository;
        private readonly CreateToolPackageStoreAndInstaller _createToolPackageStoreAndInstaller;

        private readonly PackageId _packageId;
        private readonly string _configFilePath;
        private readonly string _framework;
        private readonly string _source;
        private readonly bool _global;
        private readonly string _verbosity;
        private readonly string _toolPath;

        public UpdateToolCommand(AppliedOption appliedCommand,
            ParseResult parseResult,
            CreateToolPackageStoreAndInstaller createToolPackageStoreAndInstaller = null,
            CreateShellShimRepository createShellShimRepository = null,
            IReporter reporter = null)
            : base(parseResult)
        {
            if (appliedCommand == null)
            {
                throw new ArgumentNullException(nameof(appliedCommand));
            }

            _packageId = new PackageId(appliedCommand.Arguments.Single());
            _configFilePath = appliedCommand.ValueOrDefault<string>("configfile");
            _framework = appliedCommand.ValueOrDefault<string>("framework");
            _source = appliedCommand.ValueOrDefault<string>("source");
            _global = appliedCommand.ValueOrDefault<bool>("global");
            _verbosity = appliedCommand.SingleArgumentOrDefault("verbosity");
            _toolPath = appliedCommand.SingleArgumentOrDefault("tool-path");

            _createToolPackageStoreAndInstaller = createToolPackageStoreAndInstaller ??
                                                  ToolPackageFactory.CreateToolPackageStoreAndInstaller;

            _createShellShimRepository =
                createShellShimRepository ?? ShellShimRepositoryFactory.CreateShellShimRepository;

            _reporter = (reporter ?? Reporter.Output);
            _errorReporter = (reporter ?? Reporter.Error);
        }

        public override int Execute()
        {
            if (string.IsNullOrWhiteSpace(_toolPath) && !_global)
            {
                throw new GracefulException(
                    "Please specify either the global option (--global) or the tool path option (--tool-path)."); // TODO wul loc
            }

            if (!string.IsNullOrWhiteSpace(_toolPath) && _global)
            {
                throw new GracefulException(
                    "(--global) conflicts with the tool path option (--tool-path). Please specify only one of the options.");
            }

            if (_configFilePath != null && !File.Exists(_configFilePath))
            {
                throw new GracefulException(
                    string.Format(
                        "NuGet configuration file '{0}' does not exist.",
                        Path.GetFullPath(_configFilePath)));
            }

            DirectoryPath? toolPath = null;
            if (_toolPath != null)
            {
                toolPath = new DirectoryPath(_toolPath);
            }

            (IToolPackageStore toolPackageStore, IToolPackageInstaller toolPackageInstaller) =
                _createToolPackageStoreAndInstaller(toolPath);
            IShellShimRepository shellShimRepository = _createShellShimRepository(toolPath);


            IToolPackage oldPackage;
            try
            {
                oldPackage = toolPackageStore.EnumeratePackageVersions(_packageId).SingleOrDefault();
                if (oldPackage == null)
                {
                    throw new GracefulException(
                        messages: new[]
                        {
                            string.Format(
                                "Tool '{0}' is not currently installed.", // TODO wul loc
                                _packageId),
                        },
                        isUserError: false);
                }
            }
            catch (InvalidOperationException)
            {
                throw new GracefulException(
                    messages: new[]
                    {
                        string.Format(
                            "Tool '{0}' has multiple versions installed and cannot be uninstalled.", // TODO wul loc
                            _packageId),
                    },
                    isUserError: false);
            }

            FilePath? configFile = null;
            if (_configFilePath != null)
            {
                configFile = new FilePath(_configFilePath);
            }

            try
            {
                using (var scope = new TransactionScope(
                    TransactionScopeOption.Required,
                    TimeSpan.Zero))
                {
                    foreach (var command in oldPackage.Commands)
                    {
                        shellShimRepository.RemoveShim(command.Name);
                    }

                    oldPackage.Uninstall();

                    var newInstalledPackage = toolPackageInstaller.InstallPackage(
                        packageId: _packageId,
                        targetFramework: _framework,
                        nugetConfig: configFile,
                        source: _source,
                        verbosity: _verbosity);

                    foreach (var command in newInstalledPackage.Commands)
                    {
                        shellShimRepository.CreateShim(command.Executable, command.Name);
                    }

                    PrintSuccessMessage(oldPackage, newInstalledPackage);

                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return 0;
        }

        private void PrintSuccessMessage(IToolPackage oldPackage, IToolPackage newInstalledPackage)
        {
            if (oldPackage.Version != newInstalledPackage.Version)
            {
                _reporter.WriteLine(
                    string.Format(
                        "Tool '{0}' (from version '{1}' to version'{2}') was successfully updated.",
                        newInstalledPackage.Id,
                        oldPackage.Version.ToNormalizedString(),
                        newInstalledPackage.Version.ToNormalizedString()).Green());
            }
            else
            {
                _reporter.WriteLine(
                    string.Format(
                        "Tool '{0}' was successfully updated with no version change.",
                        newInstalledPackage.Id).Green());
            }
        }
    }
}