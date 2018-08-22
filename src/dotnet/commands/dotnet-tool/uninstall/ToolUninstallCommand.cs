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

namespace Microsoft.DotNet.Tools.Tool.Uninstall
{
    internal delegate IShellShimRepository CreateShellShimRepository(DirectoryPath? nonGlobalLocation = null);
    internal delegate (IToolPackageStore, IToolPackageUninstaller) CreateToolPackageStoreAndUninstaller(DirectoryPath? nonGlobalLocation = null);
    internal class ToolUninstallCommand : CommandBase
    {
        private readonly AppliedOption _options;
        private readonly IReporter _reporter;
        private readonly IReporter _errorReporter;
        private CreateShellShimRepository _createShellShimRepository;
        private CreateToolPackageStoreAndUninstaller _createToolPackageStoreAndUninstaller;

        public ToolUninstallCommand(
            AppliedOption options,
            ParseResult result,
            CreateToolPackageStoreAndUninstaller createToolPackageStoreAndUninstaller = null,
            CreateShellShimRepository createShellShimRepository = null,
            IReporter reporter = null)
            : base(result)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _reporter = reporter ?? Reporter.Output;
            _errorReporter = reporter ?? Reporter.Error;

            _createShellShimRepository = createShellShimRepository ?? ShellShimRepositoryFactory.CreateShellShimRepository;
            _createToolPackageStoreAndUninstaller = createToolPackageStoreAndUninstaller ??
                                                    ToolPackageFactory.CreateToolPackageStoreAndUninstaller;
        }

        public override int Execute()
        {
            var global = _options.ValueOrDefault<bool>("global");
            var toolPath = _options.SingleArgumentOrDefault("tool-path");

            DirectoryPath? toolDirectoryPath = null;
            if (!string.IsNullOrWhiteSpace(toolPath))
            {
                if (!Directory.Exists(toolPath))
                {
                    throw new GracefulException(
                        string.Format(
                            LocalizableStrings.InvalidToolPathOption,
                            toolPath));
                }

                toolDirectoryPath = new DirectoryPath(toolPath);
            }

            if (toolDirectoryPath == null && !global)
            {
                throw new GracefulException(LocalizableStrings.UninstallToolCommandNeedGlobalOrToolPath);
            }

            if (toolDirectoryPath != null && global)
            {
                throw new GracefulException(LocalizableStrings.UninstallToolCommandInvalidGlobalAndToolPath);
            }

            (IToolPackageStore toolPackageStore, IToolPackageUninstaller toolPackageUninstaller)
                = _createToolPackageStoreAndUninstaller(toolDirectoryPath);
            IShellShimRepository shellShimRepository = _createShellShimRepository(toolDirectoryPath);

            var packageId = new PackageId(_options.Arguments.Single());
            IToolPackage package = null;
            try
            {
                package = toolPackageStore.EnumeratePackageVersions(packageId).SingleOrDefault();
                if (package == null)
                {
                    throw new GracefulException(
                        messages: new[]
                        {
                            string.Format(
                                LocalizableStrings.ToolNotInstalled,
                                packageId),
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
                        LocalizableStrings.ToolHasMultipleVersionsInstalled,
                        packageId),
                        },
                    isUserError: false);
            }

            try
            {
                using (var scope = new TransactionScope(
                    TransactionScopeOption.Required,
                    TimeSpan.Zero))
                {
                    foreach (var command in package.Commands)
                    {
                        shellShimRepository.RemoveShim(command.Name);
                    }

                    toolPackageUninstaller.Uninstall(package.PackageDirectory);

                    scope.Complete();
                }

                _reporter.WriteLine(
                    string.Format(
                        LocalizableStrings.UninstallSucceeded,
                        package.Id,
                        package.Version.ToNormalizedString()).Green());
                return 0;
            }
            catch (Exception ex) when (ToolUninstallCommandLowLevelErrorConverter.ShouldConvertToUserFacingError(ex))
            {
                throw new GracefulException(
                    messages: ToolUninstallCommandLowLevelErrorConverter.GetUserFacingMessages(ex, packageId),
                    verboseMessages: new[] {ex.ToString()},
                    isUserError: false);
            }
        }
    }
}
