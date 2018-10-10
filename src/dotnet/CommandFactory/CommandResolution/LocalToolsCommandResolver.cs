// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.DotNet.ToolManifest;
using Microsoft.DotNet.ToolPackage;
using NuGet.Frameworks;
using Microsoft.DotNet.Cli;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.CommandFactory
{
    internal class LocalToolsCommandResolver : ICommandResolver
    {
        private readonly ToolManifestFinder _toolManifest;
        private readonly ILocalToolsResolverCache _localToolsResolverCache;
        private readonly DirectoryPath _nugetGlobalPackagesFolder;

        public LocalToolsCommandResolver(ToolManifestFinder toolManifest, ILocalToolsResolverCache localToolsResolverCache, DirectoryPath? nugetGlobalPackagesFolder)
        {
            _toolManifest = toolManifest ?? throw new ArgumentNullException(nameof(toolManifest));
            _localToolsResolverCache = localToolsResolverCache ?? throw new ArgumentNullException(nameof(localToolsResolverCache));
            _nugetGlobalPackagesFolder = nugetGlobalPackagesFolder ?? new DirectoryPath(NuGetGlobalPackagesFolder.GetLocation());
        }

        public CommandSpec Resolve(CommandResolverArguments arguments)
        {
            if (arguments == null || string.IsNullOrWhiteSpace(arguments.CommandName))
            {
                return null;
            }

            ToolCommandName toolCommandName = new ToolCommandName(arguments.CommandName);
            if (_toolManifest.TryFind(toolCommandName, out var toolManifestPackage))
            {
                if (_localToolsResolverCache.TryLoad(
                    new RestoredCommandIdentifier(
                        toolManifestPackage.PackageId,
                        toolManifestPackage.Version,
                        NuGetFramework.Parse(BundledTargetFramework.GetTargetFrameworkMoniker()),
                        "any",
                        toolCommandName),
                    _nugetGlobalPackagesFolder,
                    out var restoredCommand))
                {
                    return CreatePackageCommandSpecUsingMuxer(
                        restoredCommand.Executable.Value,
                        arguments.CommandArguments);
                }
            }

            return null;
        }

        private CommandSpec CreatePackageCommandSpecUsingMuxer(
            string commandPath,
            IEnumerable<string> commandArguments)
        {
            var arguments = new List<string>();

            var muxer = new Muxer();

            var host = muxer.MuxerPath;
            if (host == null)
            {
                throw new Exception(LocalizableStrings.UnableToLocateDotnetMultiplexer);
            }

            arguments.Add(commandPath);

            if (commandArguments != null)
            {
                arguments.AddRange(commandArguments);
            }

            return CreateCommandSpec(host, arguments);
        }

        private CommandSpec CreateCommandSpec(
            string commandPath,
            IEnumerable<string> commandArguments)
        {
            var escapedArgs = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(commandArguments);

            return new CommandSpec(commandPath, escapedArgs);
        }
    }
}
