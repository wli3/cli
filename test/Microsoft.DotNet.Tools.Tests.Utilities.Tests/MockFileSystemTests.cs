// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Tests.Utilities.Tests
{
    public class MockFileSystemTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DirectoryExistsShouldCountTheSameNameFile(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directroy = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string nestedFilePath = Path.Combine(directroy, "filename");
            fileSystem.File.CreateEmptyFile(nestedFilePath);

            fileSystem.Directory.Exists(nestedFilePath).Should().BeFalse();
        }

        [WindowsOnlyTheory]
        [InlineData(false)]
        [InlineData(true)]
        public void DifferentDirectorySeparatorShouldBeSameFile(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directroy = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string nestedFilePath = $"{directroy}\\filename";
            fileSystem.File.CreateEmptyFile(nestedFilePath);

            fileSystem.File.Exists($"{directroy}/filename").Should().BeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CouldCreateEmptyFileWhenDirectoryExists(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directroy = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string nestedFilePath = Path.Combine(directroy, "filename");
            fileSystem.File.CreateEmptyFile(nestedFilePath);

            fileSystem.File.Exists(nestedFilePath).Should().BeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CouldCreateDirectory(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directroy = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;

            fileSystem.Directory.Exists(directroy).Should().BeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ShouldNotThrowOnCreateDirectoryWhenExists(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directroy = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;

            Action a = () => fileSystem.Directory.CreateDirectory(directroy);
            a.ShouldNotThrow();
        }

        [WindowsOnlyTheory]
        [InlineData(false)]
        [InlineData(true)]
        public void CouldThrowWhenDirectoryDoesNotExist(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directroy = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string nestedFilePath = Path.Combine(directroy, "subfolder", "filename");
            Action a = () => fileSystem.File.CreateEmptyFile(nestedFilePath);
            a.ShouldThrow<DirectoryNotFoundException>();
        }

        private static IFileSystem SetupSubjectFileSystem(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem;
            if (testMockBehaviorIsInSync)
            {
                FileSystemMockBuilder temporaryFolder = new FileSystemMockBuilder
                {
                    TemporaryFolder = Path.GetTempPath()
                };
                fileSystem = temporaryFolder.Build();
            }
            else
            {
                fileSystem = new FileSystemWrapper();
            }

            return fileSystem;
        }
    }
}
