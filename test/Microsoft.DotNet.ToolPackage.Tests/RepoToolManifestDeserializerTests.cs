// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using FluentAssertions;
using Microsoft.DotNet.ToolPackage.ToolConfigurationDeserialization;
using Xunit;

namespace Microsoft.DotNet.ToolPackage.Tests
{
    public class RepoToolManifestDeserializerTests
    {
        [Fact(Skip = "pending")]
        public void ItShouldNotUseSerializableTypes()
        {
        }

        [Fact]
        public void GivenGoldernFileItCanDeserialize()
        {
            var serializer = new XmlSerializer(typeof(RepoTools));

            RepoTools repoToolManifest;

            // TODO wul to have proper message
            using (var fs = new FileStream("DotnetToolSettingsGolden.xml", FileMode.Open))
            {
                var reader = XmlReader.Create(fs);
                repoToolManifest = (RepoTools)serializer.Deserialize(reader);
            }

            repoToolManifest.Commands.First().PackageId.Should().Be("my.command.specific");
            repoToolManifest.Commands.First().Version.Should().Be("1.0");
            repoToolManifest.Commands.First().Configfile.Should().Be("build/nuget.config");
            repoToolManifest.Commands.Skip(1).First().PackageId.Should().Be("my.command.specific2");
            repoToolManifest.Commands.Skip(1).First().Version.Should().Be("1.0");
            repoToolManifest.Commands.Skip(1).First().AddSource.Should().Be("https://www.myget.org/F/wultest/api/v3/index.json");
            repoToolManifest.Commands.Skip(1).First().Framework.Should().Be("netcoreapp2.1");

            repoToolManifest.Version.Should().Be(1);
        }
    }
}
