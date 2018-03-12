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
    internal delegate IToolPackageStore CreateToolPackageStore(DirectoryPath? nonGlobalLocation = null);
    internal class UpdateToolCommand : CommandBase
    {
        private readonly AppliedOption _options;
        private readonly IReporter _reporter;
        private readonly IReporter _errorReporter;
        private CreateShellShimRepository _createShellShimRepository;
        private CreateToolPackageStore _createToolPackageStoreAndInstaller;

        public UpdateToolCommand(
            AppliedOption options,
            ParseResult result,
            CreateToolPackageStore createToolPackageStoreAndInstaller = null,
            CreateShellShimRepository createShellShimRepository = null,
            IReporter reporter = null)
            : base(result)
        {
            var pathCalculator = new CliFolderPathCalculator();

            _options = options ?? throw new ArgumentNullException(nameof(options));
            _reporter = reporter ?? Reporter.Output;
            _errorReporter = reporter ?? Reporter.Error;

            _createShellShimRepository
                = createShellShimRepository
                  ?? ShellShimRepositoryFactory.CreateShellShimRepository;
            _createToolPackageStoreAndInstaller
                = createToolPackageStoreAndInstaller
                  ?? ToolPackageFactory.CreateToolPackageStore;
        }

        public override int Execute()
        {
            var global = _options.ValueOrDefault<bool>("global");
            return 0;
        }
    }
}
