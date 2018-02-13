// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Transactions;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Tools.Install.Tool;
using Microsoft.DotNet.Tools.Tests.ComponentMocks;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using Xunit;

namespace Microsoft.DotNet.ToolPackage.Tests
{
    public class ToolPackageInstallerTests : TestBase
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenNoFeedInstallFailsWithException(bool testMockBehaviorIsInSync)
        {
            var (installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                feeds: new MockFeed[0]);

            Action a = () => installer.InstallPackage(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion,
                targetFramework: _testTargetframework);

            a.ShouldThrow<ToolPackageException>().WithMessage(LocalizableStrings.ToolInstallationRestoreFailed);

            reporter.Lines.Count.Should().Be(1);
            reporter.Lines[0].Should().Contain(TestPackageId);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenOfflineFeedInstallSuceeds(bool testMockBehaviorIsInSync)
        {
            var (installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                offlineFeed: new DirectoryPath(GetTestLocalFeedPath()),
                feeds: GetOfflineMockFeed());

            var package = installer.InstallPackage(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion,
                targetFramework: _testTargetframework);

            AssertPackageInstall(reporter, fileSystem, package, installer.Repository);

            package.Uninstall();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenNugetConfigInstallSucceeds(bool testMockBehaviorIsInSync)
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();

            var (installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                nugetConfig: nugetConfigPath,
                feeds: GetMockFeedsForConfigFile(nugetConfigPath));

            var package = installer.InstallPackage(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion,
                targetFramework: _testTargetframework);

            AssertPackageInstall(reporter, fileSystem, package, installer.Repository);

            package.Uninstall();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenNugetConfigInstallSucceedsInTransaction(bool testMockBehaviorIsInSync)
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();

            var (installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                nugetConfig: nugetConfigPath,
                feeds: GetMockFeedsForConfigFile(nugetConfigPath));

            IToolPackage package = null;
            using (var transactionScope = new TransactionScope())
            {
                package = installer.InstallPackage(
                    packageId: TestPackageId,
                    packageVersion: TestPackageVersion,
                    targetFramework: _testTargetframework);

                transactionScope.Complete();
            }

            AssertPackageInstall(reporter, fileSystem, package, installer.Repository);

            package.Uninstall();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenNugetConfigInstallCreatesAnAssetFile(bool testMockBehaviorIsInSync)
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();

            var (installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                nugetConfig: nugetConfigPath,
                feeds: GetMockFeedsForConfigFile(nugetConfigPath));

            var package = installer.InstallPackage(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion,
                targetFramework: _testTargetframework);

            AssertPackageInstall(reporter, fileSystem, package, installer.Repository);

            /*
              From mytool.dll to project.assets.json
               <root>/packageid/version/packageid/version/tools/framework/rid/mytool.dll
                                       /project.assets.json
             */
            var assetJsonPath = package.Commands[0].Executable
                .GetDirectoryPath()
                .GetParentPath()
                .GetParentPath()
                .GetParentPath()
                .GetParentPath()
                .GetParentPath()
                .WithFile("project.assets.json").Value;

            fileSystem.File.Exists(assetJsonPath).Should().BeTrue();

            package.Uninstall();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAConfigFileInCurrentDirectoryPackageInstallSucceeds(bool testMockBehaviorIsInSync)
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();

            var (installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                feeds: GetMockFeedsForConfigFile(nugetConfigPath));

            /*
             * In test, we don't want NuGet to keep look up, so we point current directory to nugetconfig.
             */
            Directory.SetCurrentDirectory(nugetConfigPath.GetDirectoryPath().Value);

            var package = installer.InstallPackage(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion,
                targetFramework: _testTargetframework);

            AssertPackageInstall(reporter, fileSystem, package, installer.Repository);

            package.Uninstall();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAllButNoPackageVersionItCanInstallThePackage(bool testMockBehaviorIsInSync)
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();

            var (installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                nugetConfig: nugetConfigPath,
                feeds: GetMockFeedsForConfigFile(nugetConfigPath));

            var package = installer.InstallPackage(
                packageId: TestPackageId,
                targetFramework: _testTargetframework);

            AssertPackageInstall(reporter, fileSystem, package, installer.Repository);

            package.Uninstall();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAllButNoTargetFrameworkItCanDownloadThePackage(bool testMockBehaviorIsInSync)
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();

            var (installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                nugetConfig: nugetConfigPath,
                feeds: GetMockFeedsForConfigFile(nugetConfigPath));

            var package = installer.InstallPackage(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion);

            AssertPackageInstall(reporter, fileSystem, package, installer.Repository);

            package.Uninstall();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenASourceInstallSucceeds(bool testMockBehaviorIsInSync)
        {
            var source = GetTestLocalFeedPath();

            var (installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                source: source,
                feeds: GetMockFeedsForSource(source));

            var package = installer.InstallPackage(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion,
                targetFramework: _testTargetframework);

            AssertPackageInstall(reporter, fileSystem, package, installer.Repository);

            package.Uninstall();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenFailedRestoreInstallWillRollback(bool testMockBehaviorIsInSync)
        {
            var (installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync);

            Action a = () => {
                using (var t = new TransactionScope())
                {
                    installer.InstallPackage("non.existent.package.id");

                    t.Complete();
                }
            };

            a.ShouldThrow<ToolPackageException>().WithMessage(LocalizableStrings.ToolInstallationRestoreFailed);

            AssertInstallRollBack(fileSystem, installer.Repository);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenFailureAfterRestoreInstallWillRollback(bool testMockBehaviorIsInSync)
        {
            var source = GetTestLocalFeedPath();

            var (installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                source: source,
                feeds: GetMockFeedsForSource(source));

            void FailedStepAfterSuccessRestore() => throw new GracefulException("simulated error");

            Action a = () => {
                using (var t = new TransactionScope())
                {
                    installer.InstallPackage(
                        packageId: TestPackageId,
                        packageVersion: TestPackageVersion,
                        targetFramework: _testTargetframework);

                    FailedStepAfterSuccessRestore();
                    t.Complete();
                }
            };

            a.ShouldThrow<GracefulException>().WithMessage("simulated error");

            AssertInstallRollBack(fileSystem, installer.Repository);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenSecondInstallInATransactionTheFirstInstallShouldRollback(bool testMockBehaviorIsInSync)
        {
            var source = GetTestLocalFeedPath();

            var (installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                source: source,
                feeds: GetMockFeedsForSource(source));

            Action a = () => {
                using (var t = new TransactionScope())
                {
                    Action first = () => installer.InstallPackage(
                        packageId: TestPackageId,
                        packageVersion: TestPackageVersion,
                        targetFramework: _testTargetframework);

                    first.ShouldNotThrow();

                    installer.InstallPackage(
                        packageId: TestPackageId,
                        packageVersion: TestPackageVersion,
                        targetFramework: _testTargetframework);

                    t.Complete();
                }
            };

            a.ShouldThrow<ToolPackageException>().Where(
                ex => ex.Message ==
                    string.Format(
                        CommonLocalizableStrings.ToolPackageConflictPackageId,
                        TestPackageId,
                        TestPackageVersion));

            AssertInstallRollBack(fileSystem, installer.Repository);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenSecondInstallWithoutATransactionTheFirstShouldNotRollback(bool testMockBehaviorIsInSync)
        {
            var source = GetTestLocalFeedPath();

            var (installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                source: source,
                feeds: GetMockFeedsForSource(source));

            var package = installer.InstallPackage(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion,
                targetFramework: _testTargetframework);

            AssertPackageInstall(reporter, fileSystem, package, installer.Repository);

            Action secondCall = () => installer.InstallPackage(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion,
                targetFramework: _testTargetframework);

            reporter.Lines.Should().BeEmpty();

            secondCall.ShouldThrow<ToolPackageException>().Where(
                ex => ex.Message ==
                    string.Format(
                        CommonLocalizableStrings.ToolPackageConflictPackageId,
                        TestPackageId,
                        TestPackageVersion));

            fileSystem
                .Directory
                .Exists(installer.Repository.Root.WithSubDirectories(TestPackageId).Value)
                .Should()
                .BeTrue();

            package.Uninstall();

            fileSystem
                .Directory
                .EnumerateFileSystemEntries(installer.Repository.Root.WithSubDirectories(".stage").Value)
                .Should()
                .BeEmpty();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAnInstalledPackageUninstallRemovesThePackage(bool testMockBehaviorIsInSync)
        {
            var source = GetTestLocalFeedPath();

            var (installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                source: source,
                feeds: GetMockFeedsForSource(source));

            var package = installer.InstallPackage(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion,
                targetFramework: _testTargetframework);

            AssertPackageInstall(reporter, fileSystem, package, installer.Repository);

            package.Uninstall();

            installer.Repository.GetInstalledPackages(TestPackageId).Should().BeEmpty();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAnInstalledPackageUninstallRollsbackWhenTransactionAborts(bool testMockBehaviorIsInSync)
        {
            var source = GetTestLocalFeedPath();

            var (installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                source: source,
                feeds: GetMockFeedsForSource(source));

            var package = installer.InstallPackage(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion,
                targetFramework: _testTargetframework);

            AssertPackageInstall(reporter, fileSystem, package, installer.Repository);

            using (var scope = new TransactionScope())
            {
                package.Uninstall();
                installer.Repository.GetInstalledPackages(TestPackageId).Should().BeEmpty();
            }

            package = installer.Repository.GetInstalledPackages(TestPackageId).First();
            
            AssertPackageInstall(reporter, fileSystem, package, installer.Repository);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAnInstalledPackageUninstallRemovesThePackageWhenTransactionCommits(bool testMockBehaviorIsInSync)
        {
            var source = GetTestLocalFeedPath();

            var (installer, reporter, fileSystem) = Setup(
                useMock: testMockBehaviorIsInSync,
                source: source,
                feeds: GetMockFeedsForSource(source));

            var package = installer.InstallPackage(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion,
                targetFramework: _testTargetframework);

            AssertPackageInstall(reporter, fileSystem, package, installer.Repository);

            using (var scope = new TransactionScope())
            {
                package.Uninstall();
                scope.Complete();
            }

            installer.Repository.GetInstalledPackages(TestPackageId).Should().BeEmpty();
        }

        private static void AssertPackageInstall(
            BufferedReporter reporter,
            IFileSystem fileSystem,
            IToolPackage package,
            IToolPackageRepository repository)
        {
            reporter.Lines.Should().BeEmpty();

            package.PackageId.Should().Be(TestPackageId);
            package.PackageVersion.Should().Be(TestPackageVersion);
            package.PackageDirectory.Value.Should().Contain(repository.Root.Value);
            
            repository.GetInstalledPackages(TestPackageId).Select(p => p.PackageVersion).Should().Equal(TestPackageVersion);

            package.Commands.Count.Should().Be(1);
            fileSystem.File.Exists(package.Commands[0].Executable.Value).Should().BeTrue($"{package.Commands[0].Executable.Value} should exist");
            package.Commands[0].Executable.Value.Should().Contain(repository.Root.Value);
        }

        private static void AssertInstallRollBack(IFileSystem fileSystem, IToolPackageRepository repository)
        {
            if (!fileSystem.Directory.Exists(repository.Root.Value))
            {
                return;
            }

            fileSystem
                .Directory
                .EnumerateFileSystemEntries(repository.Root.Value)
                .Should()
                .NotContain(e => Path.GetFileName(e) != ".stage");

            fileSystem
                .Directory
                .EnumerateFileSystemEntries(repository.Root.WithSubDirectories(".stage").Value)
                .Should()
                .BeEmpty();
        }

        private static FilePath GetUniqueTempProjectPathEachTest()
        {
            var tempProjectDirectory =
                new DirectoryPath(Path.GetTempPath()).WithSubDirectories(Path.GetRandomFileName());
            var tempProjectPath =
                tempProjectDirectory.WithFile(Path.GetRandomFileName() + ".csproj");
            return tempProjectPath;
        }

        private static IEnumerable<MockFeed> GetMockFeedsForConfigFile(FilePath nugetConfig)
        {
            return new MockFeed[]
            {
                new MockFeed
                {
                    Type = MockFeedType.ExplicitNugetConfig,
                    Uri = nugetConfig.Value,
                    Packages = new List<MockFeedPackage>
                    {
                        new MockFeedPackage
                        {
                            PackageId = TestPackageId,
                            Version = TestPackageVersion
                        }
                    }
                }
            };
        }

        private static IEnumerable<MockFeed> GetMockFeedsForSource(string source)
        {
            return new MockFeed[]
            {
                new MockFeed
                {
                    Type = MockFeedType.Source,
                    Uri = source,
                    Packages = new List<MockFeedPackage>
                    {
                        new MockFeedPackage
                        {
                            PackageId = TestPackageId,
                            Version = TestPackageVersion
                        }
                    }
                }
            };
        }

        private static IEnumerable<MockFeed> GetOfflineMockFeed()
        {
            return new MockFeed[]
            {
                new MockFeed
                {
                    Type = MockFeedType.OfflineFeed,
                    Uri = GetTestLocalFeedPath(),
                    Packages = new List<MockFeedPackage>
                    {
                        new MockFeedPackage
                        {
                            PackageId = TestPackageId,
                            Version = TestPackageVersion
                        }
                    }
                }
            };
        }

        private static (IToolPackageInstaller, BufferedReporter, IFileSystem) Setup(
            bool useMock,
            FilePath? nugetConfig = null,
            string source = null,
            IEnumerable<MockFeed> feeds = null,
            FilePath? tempProject = null,
            DirectoryPath? offlineFeed = null)
        {
            var root = new DirectoryPath(Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName()));
            var reporter = new BufferedReporter();
            IFileSystem fileSystem = useMock ? new FileSystemMockBuilder().Build() : new FileSystemWrapper();
            
            IToolPackageInstaller installer;
            if (useMock)
            {
                installer = new ToolPackageInstallerMock(
                    fileSystem: fileSystem,
                    repository: new ToolPackageRepositoryMock(root, fileSystem),
                    projectRestorer: new ProjectRestorerMock(
                        fileSystem: fileSystem,
                        reporter: reporter,
                        feeds: feeds,
                        nugetConfig: nugetConfig,
                        source: source));
            }
            else
            {
                installer = new ToolPackageInstaller(
                    repository: new ToolPackageRepository(root),
                    projectRestorer: new ProjectRestorer(
                        nugetConfig: nugetConfig,
                        source: source,
                        verbosity: null,
                        reporter: reporter),
                    tempProject: tempProject ?? GetUniqueTempProjectPathEachTest(),
                    offlineFeed: offlineFeed ?? new DirectoryPath("does not exist"));
            }

            return (installer, reporter, fileSystem);
        }

        private static FilePath WriteNugetConfigFileToPointToTheFeed()
        {
            var nugetConfigName = "nuget.config";

            var tempPathForNugetConfigWithWhiteSpace =
                Path.Combine(Path.GetTempPath(),
                    Path.GetRandomFileName() + " " + Path.GetRandomFileName());
            Directory.CreateDirectory(tempPathForNugetConfigWithWhiteSpace);

            NuGetConfig.Write(
                directory: tempPathForNugetConfigWithWhiteSpace,
                configname: nugetConfigName,
                localFeedPath: GetTestLocalFeedPath());

            return new FilePath(Path.GetFullPath(Path.Combine(tempPathForNugetConfigWithWhiteSpace, nugetConfigName)));
        }

        private static string GetTestLocalFeedPath() => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestAssetLocalNugetFeed");
        private readonly string _testTargetframework = BundledTargetFramework.GetTargetFrameworkMoniker();
        private const string TestPackageVersion = "1.0.4";
        private const string TestPackageId = "global.tool.console.demo";
    }
}
