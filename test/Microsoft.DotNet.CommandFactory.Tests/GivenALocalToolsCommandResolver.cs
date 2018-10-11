// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.CommandFactory;
using Microsoft.DotNet.ToolManifest;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.DotNet.Tools.Tests.ComponentMocks;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Frameworks;
using NuGet.Versioning;
using Xunit;

namespace Microsoft.DotNet.Tests
{
    public class GivenALocalToolsCommandResolver : TestBase
    {
        private readonly IFileSystem _fileSystem;
        private readonly FilePath _fakeExecutable;
        private readonly PackageId _packageIdA = new PackageId("local.tool.console.a");
        private readonly ToolCommandName _toolCommandNameA = new ToolCommandName("a");
        private readonly LocalToolsCommandResolver _localToolsCommandResolver;
        private const string ManifestFilename = "localtool.manifest.json";

        public GivenALocalToolsCommandResolver()
        {
            NuGetVersion packageVersionA = NuGetVersion.Parse("1.0.4");
            _fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();
            DirectoryPath nugetGlobalPackagesFolder = new DirectoryPath(NuGetGlobalPackagesFolder.GetLocation());
            string temporaryDirectory = _fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string testDirectoryRoot = _fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;

            ILocalToolsResolverCache localToolsResolverCache = new LocalToolsResolverCache(
                _fileSystem,
                new DirectoryPath(Path.Combine(temporaryDirectory, "cache")));

            _fileSystem.File.WriteAllText(Path.Combine(testDirectoryRoot, ManifestFilename), _jsonContent);
            ToolManifestFinder toolManifest = new ToolManifestFinder(new DirectoryPath(testDirectoryRoot), _fileSystem);

            _fakeExecutable = nugetGlobalPackagesFolder.WithFile("fakeExecutable.dll");
            _fileSystem.Directory.CreateDirectory(nugetGlobalPackagesFolder.Value);
            _fileSystem.File.CreateEmptyFile(_fakeExecutable.Value);
            localToolsResolverCache.Save(
                new Dictionary<RestoredCommandIdentifier, RestoredCommand>
                {
                    [new RestoredCommandIdentifier(
                            _packageIdA,
                            packageVersionA,
                            NuGetFramework.Parse(BundledTargetFramework.GetTargetFrameworkMoniker()),
                            Constants.AnyRid,
                            _toolCommandNameA)]
                        = new RestoredCommand(_toolCommandNameA, "dotnet", _fakeExecutable)
                }, nugetGlobalPackagesFolder);

            _localToolsCommandResolver = new LocalToolsCommandResolver(toolManifest, localToolsResolverCache,
                _fileSystem, nugetGlobalPackagesFolder);
        }

        private string _jsonContent =
            @"{
   ""version"":1,
   ""isRoot"":true,
   ""tools"":{
      ""local.tool.console.a"":{
         ""version"":""1.0.4"",
         ""commands"":[
            ""a""
         ]
      }
   }
}";

        [Fact]
        public void ItCanFindToolExecutable()
        {
            var result = _localToolsCommandResolver.Resolve(new CommandResolverArguments()
            {
                CommandName = $"dotnet-{_toolCommandNameA.ToString()}",
            });

            result.Should().NotBeNull();

            var commandPath = result.Args.Trim('"');
            _fileSystem.File.Exists(commandPath).Should().BeTrue("the following path exists: " + commandPath);
            commandPath.Should().Be(_fakeExecutable.Value);
        }

        [Fact]
        public void WhenNuGetGlobalPackageLocationIsCleanedAfterRestoreItShowError()
        {
            _fileSystem.File.Delete(_fakeExecutable.Value);

            Action action = () => _localToolsCommandResolver.Resolve(new CommandResolverArguments()
            {
                CommandName = $"dotnet-{_toolCommandNameA.ToString()}",
            });

            action.ShouldThrow<GracefulException>(string.Format(LocalizableStrings.NeedRunToolRestore,
                _toolCommandNameA.ToString()));
        }

        [Fact(Skip = "pending")]
        public void ItCanResolveAmbiguityCausedByPrefixDotnetDash()
        {
/*
/// ##### Global tools to local tools command map

| global tools command invoke | local tools command invoke |
| ------ | ------ |
| dotnetsay | dotnet dotnetsay |
| dotnet say(aka dotnet - say) | dotnet say |
| dotnetsay and dotnet say both exist | dotnet dotnetsay and dotnet dotnet-say |
*/
        }
    }
}
