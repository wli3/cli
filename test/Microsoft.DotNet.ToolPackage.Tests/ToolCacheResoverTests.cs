// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Transactions;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Tools.Tool.Install;
using Microsoft.DotNet.Tools.Tests.ComponentMocks;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Versioning;
using Xunit;
using System.Security.Cryptography;

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

            var directoryToolCache = new DirectoryToolCache();

            directoryToolCache.CommandSettingsList = new List<SerializableCommandSettings>();
            foreach (var c in commandSettingsList)
            {
                directoryToolCache.CommandSettingsList.Add(new SerializableCommandSettings
                {
                    Name = c.Name,
                    Runner = c.Runner,
                    Executable = c.Executable.Value
                });
            }

            directoryToolCache.CurrentTime = currentTime.ToString("u");
            directoryToolCache.DirectoryPath = currentPath.Value;

            var commandSettingsCacheStore = new CommandSettingsCacheStore(cacheLocation);
            commandSettingsCacheStore.Save(directoryToolCache);

            var restoredDirectoryToolCache = commandSettingsCacheStore.Load(currentPath);
            restoredDirectoryToolCache.CommandSettingsList.First().Name.Should().Be("a");
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

        internal DirectoryToolCache Load(FilePath currentPath)
        {
            using (Stream stream = File.Open(Path.Combine(_cacheLocation.Value, GetShortFileName(currentPath.Value)), FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (DirectoryToolCache)binaryFormatter.Deserialize(stream);
            }
        }

        internal void Save(DirectoryToolCache directoryToolCache)
        {
            string shortFileName = GetShortFileName(directoryToolCache.DirectoryPath);

            using (Stream stream = File.Open(Path.Combine(_cacheLocation.Value, shortFileName), FileMode.CreateNew))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, directoryToolCache);
            }
        }

        private static string GetShortFileName(string directoryPath)
        {
            return string.Format("{0:X}", directoryPath.GetHashCode());
        }
    }
}
