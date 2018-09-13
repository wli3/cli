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
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using Newtonsoft.Json;
using Xunit;
using Parser = Microsoft.DotNet.Cli.Parser;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.ToolPackage;
using Moq;
using NuGet.Frameworks;
using NuGet.Versioning;
using LocalizableStrings = Microsoft.DotNet.Tools.Tool.Restore.LocalizableStrings;

namespace Microsoft.DotNet.Tests.Commands
{
    public class ToolRestoreCommandTests
    {
        private readonly IFileSystem _fileSystem;
        private readonly IToolPackageStore _toolPackageStore;
        private readonly IToolPackageStoreQuery _toolPackageStoreQuery;
        private readonly ToolPackageInstallerMock _toolPackageInstallerMock;
        private readonly AppliedOption _appliedCommand;
        private readonly ParseResult _parseResult;
        private readonly BufferedReporter _reporter;
        private readonly string _temporaryDirectory;
        private readonly string _pathToPlacePackages;
        private readonly ILocalToolsResolverCache _localToolsResolverCache;
        private readonly PackageId _packageIdA = new PackageId("local.tool.console.a");
        private readonly NuGetVersion _packageVersionA;
        private readonly NuGetFramework _targetFrameworkA;

        private readonly PackageId _packageIdB = new PackageId("local.tool.console.B");
        private readonly NuGetVersion _packageVersionB;
        private readonly NuGetFramework _targetFrameworkB;
        private DirectoryPath _nugetGlobalPackagesFolder;

        public ToolRestoreCommandTests()
        {
            _packageVersionA = NuGetVersion.Parse("1.0.4");
            _targetFrameworkA = NuGetFramework.Parse("netcoreapp2.1");

            _packageVersionB = NuGetVersion.Parse("1.0.4");
            _targetFrameworkB = NuGetFramework.Parse("netcoreapp2.1");

            _reporter = new BufferedReporter();
            _fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();
            _nugetGlobalPackagesFolder = new DirectoryPath(NuGetGlobalPackagesFolder.GetLocation());
            _temporaryDirectory = _fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            _pathToPlacePackages = Path.Combine(_temporaryDirectory, "pathToPlacePackage");
            var toolPackageStoreMock = new ToolPackageStoreMock(new DirectoryPath(_pathToPlacePackages), _fileSystem);
            _toolPackageStore = toolPackageStoreMock;
            _toolPackageStoreQuery = toolPackageStoreMock;
            _toolPackageInstallerMock = new ToolPackageInstallerMock(
                fileSystem: _fileSystem,
                store: _toolPackageStore,
                projectRestorer: new ProjectRestorerMock(
                    fileSystem: _fileSystem,
                    reporter: _reporter,
                    feeds: new MockFeed[]
                    {
                        new MockFeed
                        {
                            Type = MockFeedType.ImplicitAdditionalFeed,
                            Packages = new List<MockFeedPackage>
                            {
                                new MockFeedPackage
                                {
                                    PackageId = _packageIdA.ToString(),
                                    Version = _packageVersionA.ToNormalizedString()
                                },
                                new MockFeedPackage
                                {
                                    PackageId = _packageIdB.ToString(),
                                    Version = _packageVersionB.ToNormalizedString()
                                }
                            }
                        }
                    }));

            ParseResult result = Parser.Instance.Parse($"dotnet tool restore");
            _appliedCommand = result["dotnet"]["tool"]["restore"];
            var parser = Parser.Instance;
            _parseResult = parser.ParseFrom("dotnet tool", new[] {"restore"});

            _localToolsResolverCache
                = new LocalToolsResolverCache(
                    _fileSystem,
                    new DirectoryPath(Path.Combine(_temporaryDirectory, "cache")),
                    version: 1);
        }

        [Fact]
        public void WhenRunItCanSaveCommandsToCache()
        {
            IManifestFileFinder manifestFileFinder =
                new MockManifestFileFinder(new[]
                {
                    (_packageIdA, _packageVersionA, null),
                    (_packageIdB, _packageVersionB, _targetFrameworkB),
                });


            var toolRestoreCommand = new ToolRestoreCommand(_appliedCommand,
                _parseResult,
                _toolPackageInstallerMock,
                manifestFileFinder,
                _localToolsResolverCache,
                _nugetGlobalPackagesFolder,
                _reporter
            );

            toolRestoreCommand.Execute().Should().Be(0);

            _localToolsResolverCache.TryLoad(
                    new RestoredCommandIdentifier(_packageIdA, _packageVersionA, _targetFrameworkA, "any",
                        new ToolCommandName("SimulatorCommand")), _nugetGlobalPackagesFolder, out var restoredCommand)
                .Should().BeTrue();

            _fileSystem.File.Exists(restoredCommand.Executable.Value)
                .Should().BeTrue($"Cached command should be found at {restoredCommand.Executable.Value}");
        }

        [Fact(Skip = "pending")]
        public void WhenHasBothLocalAndGlobalAreTrueItThrows()
        {
        }

        private class MockManifestFileFinder : IManifestFileFinder
        {
            private readonly IEnumerable<(PackageId, NuGetVersion, NuGetFramework)> _toReturn;

            public MockManifestFileFinder(IEnumerable<(PackageId, NuGetVersion, NuGetFramework)> toReturn)
            {
                _toReturn = toReturn;
            }

            public IEnumerable<(PackageId, NuGetVersion, NuGetFramework)> GetPackages(FilePath? manifestFilePath = null)
            {
                return _toReturn;
            }
        }
    }
}
