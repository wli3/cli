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
        private readonly ManifestFileFinder _manifestFileFinder;
        private readonly BufferedReporter _reporter;
        private readonly string _temporaryDirectory;
        private readonly string _pathToPlacePackages;
        private readonly ILocalToolsResolverCache _localToolsResolverCache;
        private const string PackageId = "global.tool.console.demo";
        private const string PackageVersion = "1.0.4";

        public ToolRestoreCommandTests()
        {
            _reporter = new BufferedReporter();
            _fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();
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
                    reporter: _reporter));

            ParseResult result = Parser.Instance.Parse($"dotnet tool restore {PackageId}");
            _appliedCommand = result["dotnet"]["tool"]["restore"];
            var parser = Parser.Instance;
            _parseResult = parser.ParseFrom("dotnet tool", new[] { "restore", PackageId });

            _manifestFileFinder = new ManifestFileFinder(_fileSystem);
            
            _localToolsResolverCache
                = new LocalToolsResolverCache(
                    _fileSystem, 
                    new DirectoryPath(Path.Combine(_temporaryDirectory, "cache")), 
                    version: 1);
        }
        
        [Fact]
        public void WhenRunItCanSaveCommandsToCache()
        {
            
            
            var installToolCommand = new ToolRestoreCommand(_appliedCommand,
                _parseResult,
                _toolPackageInstallerMock,
                _manifestFileFinder,
                _localToolsResolverCache,
                _reporter
                );

            installToolCommand.Execute().Should().Be(0);

            // It is hard to simulate shell behavior. Only Assert shim can point to executable dll
            _fileSystem.File.Exists(ExpectedCommandPath()).Should().BeTrue();
            var deserializedFakeShim = JsonConvert.DeserializeObject<AppHostShellShimMakerMock.FakeShim>(
                _fileSystem.File.ReadAllText(ExpectedCommandPath()));

            _fileSystem.File.Exists(deserializedFakeShim.ExecutablePath).Should().BeTrue();
        }
        
        [Fact(Skip = "pending")]
        public void WhenHasBothLocalAndGlobalAreTrueItThrows()
        {
        }
    }
}
