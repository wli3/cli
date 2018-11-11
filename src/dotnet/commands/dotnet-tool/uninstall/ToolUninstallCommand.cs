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

    internal class ToolUninstallCommand : CommandBase
    {
        private readonly AppliedOption _options;
        private readonly ToolUninstallGlobalOrToolPathCommand _toolUninstallGlobalOrToolPathCommand;
        private readonly IReporter _reporter;
        private readonly IReporter _errorReporter;

        public ToolUninstallCommand(
            AppliedOption options,
            ParseResult result,
            IReporter reporter = null,
            ToolUninstallGlobalOrToolPathCommand toolUninstallGlobalOrToolPathCommand = null)
            : base(result)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _reporter = reporter ?? Reporter.Output;
            _errorReporter = reporter ?? Reporter.Error;
            _toolUninstallGlobalOrToolPathCommand =
                toolUninstallGlobalOrToolPathCommand
                ?? new ToolUninstallGlobalOrToolPathCommand(options, result);
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

            return _toolUninstallGlobalOrToolPathCommand.Execute();
        }
    }
}
