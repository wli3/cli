// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.PlatformAbstractions;

namespace Microsoft.DotNet.CommandFactory
{
    public class LocalToolsCommandResolver : ICommandResolver
    {
        public CommandSpec Resolve(CommandResolverArguments arguments)
        {
            var manifestFileName = "repotools.manifest.xml";
            var currentSearchDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

            do
            {
                var tryManifest = Path.Combine(currentSearchDirectory.FullName, manifestFileName);
                if (File.Exists(tryManifest))
                {
                    var serializer = new XmlSerializer(typeof(RepoTools));

                    RepoTools repoToolManifest;

                    // TODO wul to have proper message
                    using (var fs = new FileStream("DotnetToolSettingsGolden.xml", FileMode.Open))
                    {
                        var reader = XmlReader.Create(fs);
                        repoToolManifest = (RepoTools)serializer.Deserialize(reader);
                    }
                }
            } while (currentSearchDirectory.Parent == null);

            if (string.IsNullOrEmpty(arguments.CommandName))
            {
                return null;
            }

            var packageId = new DirectoryInfo(Path.Combine(_dotnetToolPath, arguments.CommandName));
            if (!packageId.Exists)
            {
                return null;
            }

            var version = packageId.GetDirectories()[0];
            var dll = version.GetDirectories("tools")[0]
                .GetDirectories()[0] // TFM
                .GetDirectories()[0] // RID
                .GetFiles($"{arguments.CommandName}.dll")[0];

            return CreatePackageCommandSpecUsingMuxer(
                dll.FullName,
                arguments.CommandArguments,
                CommandResolutionStrategy.DotnetToolsPackage);
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
