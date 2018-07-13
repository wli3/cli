// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.DotNet.PlatformAbstractions;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.DotNet.ToolPackage.ToolConfigurationDeserialization;
using System.Xml;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Versioning;

namespace Microsoft.DotNet.Cli.Utils
{
    // TODO wul total no test and find a better way to DI this
    public class RepoToolsCommandResolver : IRepoToolsCommandResolver
    {
        public CommandSpec Resolve(CommandResolverArguments arguments)
        {
            var manifestFileName = "repotools.manifest.xml";
            var currentSearchDirectory =new DirectoryInfo(Directory.GetCurrentDirectory());

            do
            {
                IReadOnlyList<CommandSettings> commandSettingsList = new List<CommandSettings>();
                var tryManifest = Path.Combine(currentSearchDirectory.FullName, manifestFileName);
                if (File.Exists(tryManifest))
                {
                    DateTimeOffset? cacheTimeStamp = null;

                    DirectoryPath cacheLocation = new DirectoryPath(CliFolderPathCalculator.RepotoolcachePath);
                    if (!Directory.Exists(cacheLocation.Value))
                    {
                        Directory.CreateDirectory(cacheLocation.Value);
                    }

                    var commandSettingsCacheStore = new CommandSettingsCacheStore(cacheLocation);
                    if (commandSettingsCacheStore.Exists(new FilePath(tryManifest)))
                    {
                        (commandSettingsList, _, cacheTimeStamp) = commandSettingsCacheStore.Load(new FilePath(tryManifest));
                    }

                    if (cacheTimeStamp.HasValue && cacheTimeStamp.Value > DateTime.SpecifyKind(new FileInfo(tryManifest).CreationTimeUtc, DateTimeKind.Utc))
                    {
                        foreach (var cached in commandSettingsList)
                        {
                            if (arguments.CommandName == $"dotnet-{cached.Name}")
                            {
                                return CreatePackageCommandSpecUsingMuxer(cached.Executable.Value,
                                        arguments.CommandArguments,
                                        CommandResolutionStrategy.DotnetToolsPackage);
                            }
                        }
                    }

                    var serializer = new XmlSerializer(typeof(RepoTools));

                    RepoTools repoToolManifest;

                    // TODO wul to have proper message
                    using (var fs = new FileStream(tryManifest, FileMode.Open))
                    {
                        var reader = XmlReader.Create(fs);
                        repoToolManifest = (RepoTools)serializer.Deserialize(reader);
                    }

                    (var packageStore, var packageInstaller) = ToolPackageFactory.CreateToolPackageStoreAndInstaller();

                    var restoredList = new List<CommandSettings>();
                    foreach (var repotool in repoToolManifest.Commands)
                    {
                        FilePath? nugetConfig = null;
                        if (repotool.Configfile != null)
                        {
                            nugetConfig = new FilePath(repotool.Configfile);
                        }

                        VersionRange versionRange = null;
                        if (repotool.Version != null)
                        {
                            versionRange = VersionRange.Parse(repotool.Version);
                        }
                        restoredList.AddRange(packageInstaller.InstallPackageToNuGetCache(
                            new PackageId(repotool.PackageId),
                            versionRange,
                            nugetConfig,
                            additionalFeeds: repotool.AddSource == null ? null : new[] { repotool.AddSource }, 
                            targetFramework: repotool.Framework));
                    }

                    commandSettingsCacheStore.Save(restoredList, new FilePath(tryManifest), DateTimeOffset.UtcNow);

                    // The following is dup
                    foreach (var cached in restoredList)
                    {
                        if (arguments.CommandName == $"dotnet-{cached.Name}")
                        {
                            return CreatePackageCommandSpecUsingMuxer(cached.Executable.Value,
                                    arguments.CommandArguments,
                                    CommandResolutionStrategy.DotnetToolsPackage);
                        }
                    }
                }
                else
                {
                    currentSearchDirectory = currentSearchDirectory.Parent;
                }

            }
            while (currentSearchDirectory.Parent == null);

            return null;
        }

        private CommandSpec CreatePackageCommandSpecUsingMuxer(
            string commandPath,
            IEnumerable<string> commandArguments,
            CommandResolutionStrategy commandResolutionStrategy)
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

            return CreateCommandSpec(host, arguments, commandResolutionStrategy);
        }

        private CommandSpec CreateCommandSpec(
            string commandPath,
            IEnumerable<string> commandArguments,
            CommandResolutionStrategy commandResolutionStrategy)
        {
            var escapedArgs = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(commandArguments);

            return new CommandSpec(commandPath, escapedArgs, commandResolutionStrategy);
        }
    }
}
