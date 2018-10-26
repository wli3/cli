// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ToolManifest;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.DotNet.Tools.Tests.ComponentMocks;
using Microsoft.DotNet.Tools.Tool.Restore;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.TemplateEngine.Cli;
using NuGet.Frameworks;
using NuGet.Versioning;
using Xunit;
using LocalizableStrings = Microsoft.DotNet.Tools.Tool.Restore.LocalizableStrings;
using Parser = Microsoft.DotNet.Cli.Parser;

namespace Microsoft.DotNet.Tests.Commands
{
    public class ToolRestoreCommandWithMultipleNugetConfigTests
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

        public ToolRestoreCommandWithMultipleNugetConfigTests()
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

        [Fact]
        public void WhenRunItCanSaveCommandsToCache()
        {
            IToolManifestFinder manifestFileFinder =
                new MockManifestFileFinder(new[]
                {
                    new ToolManifestPackage(_packageIdA, _packageVersionA,
                        new[] {_toolCommandNameA}, TODO),
                    new ToolManifestPackage(_packageIdB, _packageVersionB,
                        new[] {_toolCommandNameB}, TODO)
                });

            ToolRestoreCommand toolRestoreCommand = new ToolRestoreCommand(_appliedCommand,
                _parseResult,
                _toolPackageInstallerMock,
                manifestFileFinder,
                _localToolsResolverCache,
                _fileSystem,
                _nugetGlobalPackagesFolder,
                _reporter
            );

            toolRestoreCommand.Execute().Should().Be(0);

            _localToolsResolverCache.TryLoad(
                    new RestoredCommandIdentifier(
                        _packageIdA,
                        _packageVersionA,
                        NuGetFramework.Parse(BundledTargetFramework.GetTargetFrameworkMoniker()),
                        Constants.AnyRid,
                        _toolCommandNameA), _nugetGlobalPackagesFolder, out RestoredCommand restoredCommand)
                .Should().BeTrue();

            _fileSystem.File.Exists(restoredCommand.Executable.Value)
                .Should().BeTrue($"Cached command should be found at {restoredCommand.Executable.Value}");
        }

        private class MockManifestFileFinder : IToolManifestFinder
        {
            private readonly IReadOnlyCollection<ToolManifestPackage> _toReturn;

            public MockManifestFileFinder(IReadOnlyCollection<ToolManifestPackage> toReturn)
            {
                _toReturn = toReturn;
            }

            public IReadOnlyCollection<ToolManifestPackage> Find(FilePath? filePath = null)
            {
                return _toReturn;
            }
        }
    }
}
