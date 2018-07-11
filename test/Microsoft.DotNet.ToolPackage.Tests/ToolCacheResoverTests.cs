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

            (IReadOnlyList<CommandSettings> restoredCommandSettingsList,
                FilePath restoredCurrentPath,
                DateTimeOffset restoredCurrentTime) = commandSettingsCacheStore.Load(currentPath);
            restoredCommandSettingsList.First().Name.Should().Be("a");
        }
    }
}
