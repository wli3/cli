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
    internal delegate (IToolPackageStore, IToolPackageInstaller) CreateToolPackageStoreAndInstaller(DirectoryPath? nonGlobalLocation = null);

    internal class UpdateToolCommand : CommandBase
    {
        private readonly IEnvironmentPathInstruction _environmentPathInstruction;
        private readonly IReporter _reporter;
        private readonly IReporter _errorReporter;
        private CreateShellShimRepository _createShellShimRepository;
        private CreateToolPackageStoreAndInstaller _createToolPackageStoreAndInstaller;

        private readonly PackageId _packageId;
        private readonly string _packageVersion;
        private readonly string _configFilePath;
        private readonly string _framework;
        private readonly string _source;
        private readonly bool _global;
        private readonly string _verbosity;
        private readonly string _toolPath;

        public UpdateToolCommand(
            AppliedOption appliedCommand,
            ParseResult parseResult,
            CreateToolPackageStoreAndInstaller createToolPackageStoreAndInstaller = null,
            CreateShellShimRepository createShellShimRepository = null,
            IEnvironmentPathInstruction environmentPathInstruction = null,
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
            _source = appliedCommand.ValueOrDefault<string>("source");
            _global = appliedCommand.ValueOrDefault<bool>("global");
            _verbosity = appliedCommand.SingleArgumentOrDefault("verbosity");
            _toolPath = appliedCommand.SingleArgumentOrDefault("tool-path");

            var cliFolderPathCalculator = new CliFolderPathCalculator();

            _createToolPackageStoreAndInstaller = createToolPackageStoreAndInstaller ?? ToolPackageFactory.CreateToolPackageStoreAndInstaller;

            _environmentPathInstruction = environmentPathInstruction
                ?? EnvironmentPathFactory.CreateEnvironmentPathInstruction();
            _createShellShimRepository = createShellShimRepository ?? ShellShimRepositoryFactory.CreateShellShimRepository;

            _reporter = (reporter ?? Reporter.Output);
            _errorReporter = (reporter ?? Reporter.Error);
        }

        public override int Execute()
        {
           
            return 0;
        }
    }
}
