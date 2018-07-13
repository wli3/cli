// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.EnvironmentAbstractions;
using Xunit;

namespace Microsoft.DotNet.ToolPackage.Tests
{
    public class ToolCacheResolverTests : TestBase
    {
        [Fact]
        public void GivenCommandSettingsAndManifestDirectoryItCanSaveAndLoad()
        {
            IReadOnlyList<CommandSettings> commandSettingsList = new List<CommandSettings>()
            {
                new CommandSettings("a", "dotnet", new FilePath("/tool/a.dll")),
                new CommandSettings("b", "dotnet", new FilePath("/tool/b.dll"))
            };

            var currentPath = new FilePath("/currentPath");
            var currentTime = DateTimeOffset.Parse("7/12/18 11:02:34 PM +00:00");
            var cacheLocation = new DirectoryPath(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Directory.CreateDirectory(cacheLocation.Value);


            var commandSettingsCacheStore = new CommandSettingsCacheStore(cacheLocation);
            commandSettingsCacheStore.Save(commandSettingsList, currentPath, currentTime);

            (IReadOnlyList<CommandSettings> restoredCommandSettingsList, FilePath restoredCurrentPath, DateTimeOffset restoredCurrentTime) = commandSettingsCacheStore.Load(currentPath);
            restoredCommandSettingsList.First().Name.Should().Be("a");
        }

    }

    [Serializable]
    internal class DirectoryToolCache
    {
        public List<SerializableCommandSettings> CommandSettingsList { get; set; }
        public string DirectoryPath { get; set; }
        public string CurrentTime { get; set; }
    }

    [Serializable]
    internal class SerializableCommandSettings
    {
        public string Name { get; set; }

        public string Runner { get; set; }

        public string Executable { get; set; }
    }

    internal class CommandSettingsCacheStore
    {
        private DirectoryPath _cacheLocation;

        internal CommandSettingsCacheStore(DirectoryPath cacheLocation)
        {
            _cacheLocation = cacheLocation;
        }

        internal (IReadOnlyList<CommandSettings> commandSettingsList, FilePath currentPath, DateTimeOffset currentTime) Load(FilePath currentPath)
        {
            DirectoryToolCache directoryToolCache;
            using (Stream stream = File.Open(Path.Combine(_cacheLocation.Value, GetShortFileName(currentPath.Value)), FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                directoryToolCache = (DirectoryToolCache)binaryFormatter.Deserialize(stream);
            }

            var commandSettingsList = new List<CommandSettings>();
            if (directoryToolCache == null)
            {
                throw new InvalidOperationException("Cannot deserialize directoryToolCache"); // TODO wul loc
            }
            if (directoryToolCache.CommandSettingsList != null)
            {
                foreach (var s in directoryToolCache.CommandSettingsList)
                {
                    commandSettingsList.Add(new CommandSettings(s.Name, s.Runner, new FilePath(s.Executable)));
                }
            }

            return (commandSettingsList,
                new FilePath(directoryToolCache.DirectoryPath),
                DateTimeOffset.Parse(directoryToolCache.CurrentTime));
        }

        private static string GetShortFileName(string directoryPath)
        {
            return string.Format("{0:X}", directoryPath.GetHashCode());
        }

        internal void Save(IReadOnlyList<CommandSettings> commandSettingsList, FilePath currentPath, DateTimeOffset currentTime)
        {
            var directoryToolCache = new DirectoryToolCache
            {
                CommandSettingsList = new List<SerializableCommandSettings>(),
                CurrentTime = currentTime.ToString("u"),
                DirectoryPath = currentPath.Value
            };

            foreach (CommandSettings c in commandSettingsList)
            {
                directoryToolCache.CommandSettingsList.Add(new SerializableCommandSettings
                {
                    Name = c.Name,
                    Runner = c.Runner,
                    Executable = c.Executable.Value
                });
            }

            string shortFileName = GetShortFileName(directoryToolCache.DirectoryPath);

            using (Stream stream = File.Open(Path.Combine(_cacheLocation.Value, shortFileName), FileMode.CreateNew))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, directoryToolCache);
            }
        }
    }
}
