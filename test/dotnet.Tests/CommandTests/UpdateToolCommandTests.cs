// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Tools.Install.Tool;
using Microsoft.DotNet.Tools.Uninstall.Tool;
using Microsoft.DotNet.Tools.Tests.ComponentMocks;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.DotNet.Tools.Update.Tool;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.TemplateEngine.Cli;
using Xunit;
using Parser = Microsoft.DotNet.Cli.Parser;
using LocalizableStrings = Microsoft.DotNet.Tools.Uninstall.Tool.LocalizableStrings;
using InstallLocalizableStrings = Microsoft.DotNet.Tools.Install.Tool.LocalizableStrings;

namespace Microsoft.DotNet.Tests.Commands
{
    public class UpdateToolCommandTests
    {
        private readonly BufferedReporter _reporter;
        private readonly IFileSystem _fileSystem;
        private readonly EnvironmentPathInstructionMock _environmentPathInstructionMock;
        private readonly ToolPackageStoreMock _store;
        private readonly ToolPackageInstallerMock _packageInstallerMock;
        private readonly PackageId _packageId = new PackageId("global.tool.console.demo");
        private const string LowerPackageVersion = "1.0.4";
        private const string HigherPackageVersion = "1.0.5";
        private const string ShimsDirectory = "shims";
        private const string ToolsDirectory = "tools";

        public UpdateToolCommandTests()
        {
            _reporter = new BufferedReporter();
            _fileSystem = new FileSystemMockBuilder().Build();
            _environmentPathInstructionMock = new EnvironmentPathInstructionMock(_reporter, ShimsDirectory);
            _store = new ToolPackageStoreMock(new DirectoryPath(ToolsDirectory), _fileSystem);
            _packageInstallerMock = new ToolPackageInstallerMock(
                _fileSystem,
                _store,
                new ProjectRestorerMock(
                    _fileSystem,
                    _reporter,
                    new List<MockFeed>
                    {
                        new MockFeed
                        {
                            Type = MockFeedType.FeedFromLookUpNugetConfig,
                            Packages = new List<MockFeedPackage>
                                {
                                    new MockFeedPackage
                                    {
                                        PackageId = _packageId.ToString(),
                                        Version = LowerPackageVersion
                                    },
                                    new MockFeedPackage
                                    {
                                        PackageId = _packageId.ToString(),
                                        Version = HigherPackageVersion
                                    }
                                }
                        }
                    }
                ));
        }

        [Fact]
        public void GivenANonExistentPackageItErrors()
        {
            var packageId = "does.not.exist";
            var command = CreateUpdateCommand($"-g {packageId}");

            Action a = () => command.Execute();

            a.ShouldThrow<GracefulException>().And.Message
                .Should().Contain(
                    string.Format(
                    "Tool '{0}' is not currently installed.", // TODO wul loc
                    packageId));
        }

        [Fact]
        public void GivenAExistedLowversionInstallationWhenCallItCanUpdateThePackageVersion()
        {
            CreateInstallCommand($"-g {_packageId} --version {LowerPackageVersion}").Execute();

            var command = CreateUpdateCommand($"-g {_packageId}");

            command.Execute();

            _store.EnumeratePackageVersions(_packageId).Single().Version.ToFullString().Should().Be(HigherPackageVersion);
        }

        [Fact]
        public void GivenAExistedLowversionInstallationWhenCallItCanPrintSucessMessage()
        {
            CreateInstallCommand($"-g {_packageId} --version {LowerPackageVersion}").Execute();

            var command = CreateUpdateCommand($"-g {_packageId}");

            command.Execute();

            _reporter.Lines.First().Should().Be("Update sucess");
        }


        [Fact]
        public void WhenRunWithBothGlobalAndToolPathShowErrorMessage()
        {
            var command = CreateUpdateCommand($"-g --tool-path /tmp/folder {_packageId}");

            Action a = () => command.Execute();

            a.ShouldThrow<GracefulException>().And.Message
                .Should().Contain("(--global) conflicts with the tool path option (--tool-path). Please specify only one of the options."); // TODO wul loc
        }

        [Fact]
        public void WhenRunWithNeitherOfGlobalNorToolPathShowErrorMessage()
        {
            var command = CreateUpdateCommand($"{_packageId}");

            Action a = () => command.Execute();

            a.ShouldThrow<GracefulException>().And.Message
                .Should().Contain("Please specify either the global option (--global) or the tool path option (--tool-path)."); // TODO wul loc
        }

        private InstallToolCommand CreateInstallCommand(string options)
        {
            ParseResult result = Parser.Instance.Parse("dotnet install tool " + options);

            return new InstallToolCommand(
                result["dotnet"]["install"]["tool"],
                result,
                (_) => (_store, _packageInstallerMock),
                (_) => new ShellShimRepositoryMock(new DirectoryPath(ShimsDirectory), _fileSystem),
                _environmentPathInstructionMock,
                _reporter);
        }

        private UpdateToolCommand CreateUpdateCommand(string options)
        {
            ParseResult result = Parser.Instance.Parse("dotnet update tool " + options);

            return new UpdateToolCommand(
                result["dotnet"]["update"]["tool"],
                result,
                (_) => (_store, _packageInstallerMock),
                (_) => new ShellShimRepositoryMock(new DirectoryPath(ShimsDirectory), _fileSystem),
                _environmentPathInstructionMock,
                _reporter);
        }
    }
}
