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
using Microsoft.DotNet.ToolManifest;
using NuGet.Frameworks;


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
        private readonly string _manifestFilePath;
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

        private readonly ToolManifestFinder _toolManifestFinder;
        private readonly ToolManifestEditor _toolManifestEditor;

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

            _localToolsResolverCache
                = new LocalToolsResolverCache(
                    _fileSystem,
                    new DirectoryPath(Path.Combine(_temporaryDirectory, "cache")),
                    1);

            _manifestFilePath = Path.Combine(_temporaryDirectory, "dotnet-tools.json");
            _fileSystem.File.WriteAllText(Path.Combine(_temporaryDirectory, _manifestFilePath), _jsonContent);
            _toolManifestFinder = new ToolManifestFinder(new DirectoryPath(_temporaryDirectory), _fileSystem);
            _toolManifestEditor = new ToolManifestEditor(_fileSystem);

            ParseResult result = Parser.Instance.Parse($"dotnet tool install {_packageIdA.ToString()}");
            _appliedCommand = result["dotnet"]["tool"]["install"];
            Cli.CommandLine.Parser parser = Parser.Instance;
            _parseResult = parser.ParseFrom("dotnet tool", new[] {"install", _packageIdA.ToString()});

            _localToolsResolverCache
                = new LocalToolsResolverCache(
                    _fileSystem,
                    new DirectoryPath(Path.Combine(_temporaryDirectory, "cache")),
                    1);
        }

        [Fact]
        public void WhenRunWithPackageIdItShouldSaveToCacheAndAddToManifestFile()
        {
            var toolInstallLocalCommand = GetDefaultTestToolInstallLocalCommand();

            toolInstallLocalCommand.Execute().Should().Be(0);

            AssertDefaultInstallSuccess();
        }

        // TODO no manifest file throw
        // TODO can find file in the current directory
        // TODO throw when framework is specified

        [Fact]
        public void WhenRunFromToolInstallRedirectCommandWithPackageIdItShouldSaveToCacheAndAddToManifestFile()
        {
            var toolInstallLocalCommand = GetDefaultTestToolInstallLocalCommand();

            var toolInstallCommand = new ToolInstallCommand(
                _appliedCommand,
                _parseResult,
                toolInstallLocalCommand: toolInstallLocalCommand);

            toolInstallCommand.Execute().Should().Be(0);
            AssertDefaultInstallSuccess();
        }

        [Fact]
        public void WhenRunWithPackageIdItShouldShowSuccessMessage()
        {
            var toolInstallLocalCommand = GetDefaultTestToolInstallLocalCommand();

            toolInstallLocalCommand.Execute().Should().Be(0);

            _reporter.Lines[0].Should()
                .Contain(
                    string.Format(LocalizableStrings.LocalToolInstallationSucceeded,
                        _toolCommandNameA.ToString(),
                        _packageIdA,
                        _packageVersionA.ToNormalizedString(),
                        _manifestFilePath).Green());
        }

        [Fact]
        public void GivenFailedPackageInstallWhenRunWithPackageIdItShouldNotChangeManifestFile()
        {
            ParseResult result = Parser.Instance.Parse($"dotnet tool install non-exist");
            var appliedCommand = result["dotnet"]["tool"]["install"];
            Cli.CommandLine.Parser parser = Parser.Instance;
            var parseResult = parser.ParseFrom("dotnet tool", new[] {"install", "non-exist"});

            var installLocalCommand = new ToolInstallLocalCommand(
                appliedCommand,
                parseResult,
                _toolPackageInstallerMock,
                _toolManifestFinder,
                _toolManifestEditor,
                _localToolsResolverCache,
                _fileSystem,
                _nugetGlobalPackagesFolder,
                _reporter);

            Action a = () => installLocalCommand.Execute();
            a.ShouldThrow<GracefulException>()
                .And.Message.Should()
                .Contain(LocalizableStrings.ToolInstallationRestoreFailed);

            _fileSystem.File.ReadAllText(_manifestFilePath).Should()
                .Be(_jsonContent, "Manifest file should not be changed");
        }

        [Fact]
        public void GivenManifestFileConflictItShouldNotAddToCache()
        {
            _toolManifestEditor.Add(
                new FilePath(_manifestFilePath),
                _packageIdA,
                new NuGetVersion(1, 1, 1),
                new[] {_toolCommandNameA});

            var toolInstallLocalCommand = GetDefaultTestToolInstallLocalCommand();

            Action a = () => toolInstallLocalCommand.Execute();
            a.ShouldThrow<GracefulException>();

            _localToolsResolverCache.TryLoad(new RestoredCommandIdentifier(
                    _packageIdA,
                    _packageVersionA,
                    NuGetFramework.Parse(BundledTargetFramework.GetTargetFrameworkMoniker()),
                    Constants.AnyRid,
                    _toolCommandNameA),
                _nugetGlobalPackagesFolder,
                out RestoredCommand restoredCommand
            ).Should().BeFalse("it should not add to cache if add to manifest failed. " +
                               "But restore do not need to 'revert' since it just set in nuget global directory");
        }

        private ToolInstallLocalCommand GetDefaultTestToolInstallLocalCommand()
        {
            return new ToolInstallLocalCommand(
                _appliedCommand,
                _parseResult,
                _toolPackageInstallerMock,
                _toolManifestFinder,
                _toolManifestEditor,
                _localToolsResolverCache,
                _fileSystem,
                _nugetGlobalPackagesFolder,
                _reporter);
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

        private void AssertDefaultInstallSuccess()
        {
            var manifestPackages = _toolManifestFinder.Find();
            manifestPackages.Should().HaveCount(1);
            var addedPackage = manifestPackages.Single();
            _localToolsResolverCache.TryLoad(new RestoredCommandIdentifier(
                    addedPackage.PackageId,
                    addedPackage.Version,
                    NuGetFramework.Parse(BundledTargetFramework.GetTargetFrameworkMoniker()),
                    Constants.AnyRid,
                    addedPackage.CommandNames.Single()),
                _nugetGlobalPackagesFolder,
                out RestoredCommand restoredCommand
            ).Should().BeTrue();

            _fileSystem.File.Exists(restoredCommand.Executable.Value);
        }


        private string _jsonContent =
            @"{
   ""version"":1,
   ""isRoot"":true,
   ""tools"":{
   }
}";
    }
}
