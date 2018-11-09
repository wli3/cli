// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools.Tool.Install;
using Microsoft.DotNet.Tools.Tests.ComponentMocks;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.DotNet.ShellShim;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using Newtonsoft.Json;
using Xunit;
using Parser = Microsoft.DotNet.Cli.Parser;
using System.Runtime.InteropServices;
using NuGet.Versioning;
using LocalizableStrings = Microsoft.DotNet.Tools.Tool.Install.LocalizableStrings;

namespace Microsoft.DotNet.Tests.Commands
{
    public class ToolInstallLocalCommandTests
    {
        private readonly IFileSystem _fileSystem;
        private readonly IToolPackageStore _toolPackageStore;
        private readonly ToolPackageInstallerMock _toolPackageInstallerMock;
        private readonly AppliedOption _appliedCommand;
        private readonly ParseResult _parseResult;
        private readonly BufferedReporter _reporter;
        private readonly string _temporaryDirectory;
        private readonly string _pathToPlacePackages;
        private readonly ILocalToolsResolverCache _localToolsResolverCache;
        private readonly PackageId _packageIdA = new PackageId("local.tool.console.a");

        private readonly PackageId _packageIdWithCommandNameCollisionWithA =
            new PackageId("command.name.collision.with.package.a");

        private readonly NuGetVersion _packageVersionWithCommandNameCollisionWithA;
        private readonly NuGetVersion _packageVersionA;
        private readonly ToolCommandName _toolCommandNameA = new ToolCommandName("a");

        private readonly PackageId _packageIdB = new PackageId("local.tool.console.B");
        private readonly NuGetVersion _packageVersionB;
        private readonly ToolCommandName _toolCommandNameB = new ToolCommandName("b");
        private readonly DirectoryPath _nugetGlobalPackagesFolder;

        public ToolInstallLocalCommandTests()
        {
            _packageVersionA = NuGetVersion.Parse("1.0.4");
            _packageVersionWithCommandNameCollisionWithA = NuGetVersion.Parse("1.0.9");
            _packageVersionB = NuGetVersion.Parse("1.0.4");

            _reporter = new BufferedReporter();
            _fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();
            _nugetGlobalPackagesFolder = new DirectoryPath(NuGetGlobalPackagesFolder.GetLocation());
            _temporaryDirectory = _fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            _pathToPlacePackages = Path.Combine(_temporaryDirectory, "pathToPlacePackage");
            ToolPackageStoreMock toolPackageStoreMock =
                new ToolPackageStoreMock(new DirectoryPath(_pathToPlacePackages), _fileSystem);
            _toolPackageStore = toolPackageStoreMock;
            _toolPackageInstallerMock = new ToolPackageInstallerMock(
                _fileSystem,
                _toolPackageStore,
                new ProjectRestorerMock(
                    _fileSystem,
                    _reporter,
                    new[]
                    {
                        new MockFeed
                        {
                            Type = MockFeedType.ImplicitAdditionalFeed,
                            Packages = new List<MockFeedPackage>
                            {
                                new MockFeedPackage
                                {
                                    PackageId = _packageIdA.ToString(),
                                    Version = _packageVersionA.ToNormalizedString(),
                                    ToolCommandName = _toolCommandNameA.ToString()
                                },
                                new MockFeedPackage
                                {
                                    PackageId = _packageIdB.ToString(),
                                    Version = _packageVersionB.ToNormalizedString(),
                                    ToolCommandName = _toolCommandNameB.ToString()
                                },
                                new MockFeedPackage
                                {
                                    PackageId = _packageIdWithCommandNameCollisionWithA.ToString(),
                                    Version = _packageVersionWithCommandNameCollisionWithA.ToNormalizedString(),
                                    ToolCommandName = "A"
                                }
                            }
                        }
                    }));

            ParseResult result = Parser.Instance.Parse("dotnet tool restore");
            _appliedCommand = result["dotnet"]["tool"]["restore"];
            Cli.CommandLine.Parser parser = Parser.Instance;
            _parseResult = parser.ParseFrom("dotnet tool", new[] {"restore"});

            _localToolsResolverCache
                = new LocalToolsResolverCache(
                    _fileSystem,
                    new DirectoryPath(Path.Combine(_temporaryDirectory, "cache")),
                    1);
        }

//        [Fact]
//        public void WhenRunWithPackageIdItShouldSaveToCacheAndAddToManifestFile()
//        {
//            var toolInstallGlobalOrToolPathCommand = new ToolInstallLocalCommand(_appliedCommand,
//                _parseResult,
//                _createToolPackageStoreAndInstaller,
//                _createShellShimRepository,
//                _environmentPathInstructionMock,
//                _reporter);
//
//            toolInstallGlobalOrToolPathCommand.Execute().Should().Be(0);
//
//            // It is hard to simulate shell behavior. Only Assert shim can point to executable dll
//            _fileSystem.File.Exists(ExpectedCommandPath()).Should().BeTrue();
//            var deserializedFakeShim = JsonConvert.DeserializeObject<AppHostShellShimMakerMock.FakeShim>(
//                _fileSystem.File.ReadAllText(ExpectedCommandPath()));
//
//            _fileSystem.File.Exists(deserializedFakeShim.ExecutablePath).Should().BeTrue();
//        }

        // TODO no manifest file throw
        // TODO 

        [Fact]
        public void WhenRunFromToolInstallRedirectCommandWithPackageIdItShouldSaveToCacheAndAddToManifestFile()
        {
        }

        [Fact]
        public void WhenRunWithPackageIdItShouldShowSuccessMessage()
        {
        }

        [Fact]
        public void GivenFailedPackageInstallWhenRunWithPackageIdItShouldFail()
        {
        }

        [Fact]
        public void GivenManifestFileConflictItShouldNotAddToCache()
        {
        }

        [Fact]
        public void GivenFailedPackageInstallWhenRunWithPackageIdItShouldNotChangeManifestFile()
        {
        }

        [Fact]
        public void WhenRunWithExactVersionItShouldSucceed()
        {
        }

        [Fact]
        public void WhenRunWithValidVersionRangeItShouldSucceed()
        {
        }

        [Fact]
        public void WhenRunWithoutAMatchingRangeItShouldFail()
        {
        }
    }
}
