// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

namespace Microsoft.DotNet.Tools.Uninstall.Tool
{
    internal class UninstallToolCommand : CommandBase
    {
        private readonly AppliedOption _options;
        private readonly IToolPackageRepository _toolPackageRepository;
        private readonly IShellShimManager _shellShimManager;
        private readonly IReporter _reporter;
        private readonly IReporter _errorReporter;

        public UninstallToolCommand(
            AppliedOption options,
            ParseResult result,
            IToolPackageRepository toolPackageRepository = null,
            IShellShimManager shellShimManager = null,
            IReporter reporter = null)
            : base(result)
        {
            var pathCalculator = new CliFolderPathCalculator();

            _options = options ?? throw new ArgumentNullException(nameof(options));
            _toolPackageRepository = toolPackageRepository ?? new ToolPackageRepository(
                new DirectoryPath(pathCalculator.ToolsPackagePath));
            _shellShimManager = shellShimManager ?? new ShellShimManager(
                new DirectoryPath(pathCalculator.ToolsShimPath));
            _reporter = reporter ?? Reporter.Output;
            _errorReporter = reporter ?? Reporter.Error;
        }

        public override int Execute()
        {
            if (!_options.ValueOrDefault<bool>("global"))
            {
                throw new GracefulException(LocalizableStrings.UninstallToolCommandOnlySupportsGlobal);
            }

            var packageId = _options.Arguments.Single();
            IToolPackage package = null;

            try
            {
                package = _toolPackageRepository.GetInstalledPackages(packageId).SingleOrDefault();
                if (package == null)
                {
                    _errorReporter.WriteLine(
                        string.Format(
                            LocalizableStrings.ToolNotInstalled,
                            packageId).Red());
                    return 1;
                }
            }
            catch (InvalidOperationException)
            {
                _errorReporter.WriteLine(
                string.Format(
                    LocalizableStrings.ToolHasMultipleVersionsInstalled,
                    packageId).Red());
                return 1;
            }

            try
            {
                using (var scope = new TransactionScope())
                {
                    foreach (var command in package.Commands)
                    {
                        _shellShimManager.RemoveShim(command.Name);
                    }

                    package.Uninstall();

                    scope.Complete();
                }

                _reporter.WriteLine(
                    string.Format(
                        LocalizableStrings.UninstallSucceeded,
                        package.PackageId,
                        package.PackageVersion).Green());
                return 0;
            }
            catch (Exception ex) when (ex is ToolConfigurationException || ex is ToolPackageException || ex is ShellShimException)
            {
                if (Reporter.IsVerbose)
                {
                    Reporter.Verbose.WriteLine(ex.ToString().Red());
                }

                _errorReporter.WriteLine(
                    string.Format(
                        LocalizableStrings.FailedToUninstallPackage,
                        packageId,
                        ex.Message).Red());
                return 1;
            }
        }
    }
}
