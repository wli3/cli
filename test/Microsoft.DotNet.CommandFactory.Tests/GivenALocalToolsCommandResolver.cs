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
using LocalizableStrings = Microsoft.DotNet.CommandFactory.LocalizableStrings;

namespace Microsoft.DotNet.Tests
{
    public class GivenALocalToolsCommandResolver : TestBase
    {
        private readonly IFileSystem _fileSystem;
        private readonly IToolPackageStore _toolPackageStore;
        private readonly ToolPackageInstallerMock _toolPackageInstallerMock;
        private readonly BufferedReporter _reporter;
        private readonly string _temporaryDirectory;
        private readonly string _pathToPlacePackages;
        private readonly ILocalToolsResolverCache _localToolsResolverCache;
        private readonly ToolManifestFinder _toolManifest;
        private FilePath _fakeExecutable;
        private readonly PackageId _packageIdA = new PackageId("local.tool.console.a");

        private readonly NuGetVersion _packageVersionWithCommandNameCollisionWithA;
        private readonly NuGetVersion _packageVersionA;
        private readonly ToolCommandName _toolCommandNameA = new ToolCommandName("a");

        private readonly DirectoryPath _nugetGlobalPackagesFolder;
        private readonly string _testDirectoryRoot;
        private readonly LocalToolsCommandResolver _localToolsCommandResolver;
        private const string _manifestFilename = "localtool.manifest.json";

        public GivenALocalToolsCommandResolver()
        {
            _packageVersionA = NuGetVersion.Parse("1.0.4");
            _packageVersionWithCommandNameCollisionWithA = NuGetVersion.Parse("1.0.9");

            _reporter = new BufferedReporter();
            _fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();
            _nugetGlobalPackagesFolder = new DirectoryPath(NuGetGlobalPackagesFolder.GetLocation());
            _temporaryDirectory = _fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            _testDirectoryRoot = _fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
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
                            }
                        }
                    }));

            _localToolsResolverCache
                = new LocalToolsResolverCache(
                    _fileSystem,
                    new DirectoryPath(Path.Combine(_temporaryDirectory, "cache")),
                    1);

            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, _manifestFilename), _jsonContent);
            _toolManifest = new ToolManifestFinder(new DirectoryPath(_testDirectoryRoot), _fileSystem);

            _fakeExecutable = _nugetGlobalPackagesFolder.WithFile("fakeExecutable.dll");
            _fileSystem.Directory.CreateDirectory(_nugetGlobalPackagesFolder.Value);
            _fileSystem.File.CreateEmptyFile(_fakeExecutable.Value);
            _localToolsResolverCache.Save(
                new Dictionary<RestoredCommandIdentifier, RestoredCommand>
                {
                    [new RestoredCommandIdentifier(
                        _packageIdA,
                        _packageVersionA,
                        NuGetFramework.Parse(BundledTargetFramework.GetTargetFrameworkMoniker()),
                        Constants.AnyRid,
                        _toolCommandNameA)]
                            = new RestoredCommand(_toolCommandNameA, "dotnet", _fakeExecutable)
                }, _nugetGlobalPackagesFolder);

            _localToolsCommandResolver = new LocalToolsCommandResolver(_toolManifest, _localToolsResolverCache, _fileSystem, _nugetGlobalPackagesFolder);
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
                CommandName = _toolCommandNameA.ToString(),
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
                CommandName = _toolCommandNameA.ToString()
            });

            action.ShouldThrow<GracefulException>(string.Format(LocalizableStrings.NeedRunToolRestore, _toolCommandNameA.ToString()));
        }
    }
}
