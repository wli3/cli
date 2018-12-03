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

namespace Microsoft.DotNet.Tools.Tool.List
{
    internal class ToolListLocalCommand : CommandBase
    {
        private readonly IToolManifestInspector _toolManifestInspector;
        private readonly IReporter _reporter;
        public const string CommandDelimiter = ", ";

        private readonly PackageId _packageId;

        public ToolListLocalCommand(
            AppliedOption appliedCommand,
            ParseResult parseResult,
            IToolManifestInspector toolManifestInspector = null,
            IReporter reporter = null)
            : base(parseResult)
        {
            if (appliedCommand == null)
            {
                throw new ArgumentNullException(nameof(appliedCommand));
            }

            _packageId = new PackageId(appliedCommand.Arguments.Single());

            _reporter = (reporter ?? Reporter.Output);

            _toolManifestInspector = toolManifestInspector ??
                                     new ToolManifestFinder(new DirectoryPath(Directory.GetCurrentDirectory()));
        }

        public override int Execute()
        {
            var toolManifestPackageAndSourceManifest = _toolManifestInspector.Inspect();
            var table = new PrintableTable<IReadOnlyCollection<(ToolManifestPackage, FilePath)>>();

            table.AddColumn(
                LocalizableStrings.PackageIdColumn,
                p => p.Id.ToString());
            table.AddColumn(
                LocalizableStrings.VersionColumn,
                p => p.Version.ToNormalizedString());
            table.AddColumn(
                LocalizableStrings.CommandsColumn,
                p => string.Join(CommandDelimiter, p.Commands.Select(c => c.Name)));

            table.PrintRows(GetPackages(toolPath), l => _reporter.WriteLine(l));
            return 0;
        }
    }
}
