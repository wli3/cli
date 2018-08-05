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
        public void WhenDirectoryExistsShouldCreateEmptyFile(bool testMockBehaviorIsInSync)
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
        public void DirectoryExistsWithRelativePathShouldCountTheSameNameFile(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directroy = fileSystem.Directory.GetCurrentDirectory();
            fileSystem.File.CreateEmptyFile("file");

            fileSystem.File.Exists(Path.Combine(directroy, "file")).Should().BeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithRelativePathShouldCreateDirectory(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directroy = fileSystem.Directory.GetCurrentDirectory();
            fileSystem.Directory.CreateDirectory("dir");

            fileSystem.Directory.Exists(Path.Combine(directroy, "dir")).Should().BeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ShouldCreateDirectory(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directroy = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;

            fileSystem.Directory.Exists(directroy).Should().BeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CreateDirectoryWhenExistsShouldNotThrow(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directroy = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;

            Action a = () => fileSystem.Directory.CreateDirectory(directroy);
            a.ShouldNotThrow();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CreateDirectoryWhenExistsSameNameFileShouldThrow(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directroy = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;

            string path = Path.Combine(directroy, "sub");
            fileSystem.File.CreateEmptyFile(path);
            Action a = () => fileSystem.Directory.CreateDirectory(path);
            a.ShouldThrow<IOException>();
        }

        [WindowsOnlyTheory]
        [InlineData(false)]
        [InlineData(true)]
        public void DirectoryDoesNotExistShouldThrow(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directroy = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string nestedFilePath = Path.Combine(directroy, "subfolder", "filename");
            Action a = () => fileSystem.File.CreateEmptyFile(nestedFilePath);
            a.ShouldThrow<DirectoryNotFoundException>();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FileReadAllTextWhenExists(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directroy = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            const string content = "content";
            string path = Path.Combine(directroy, Path.GetRandomFileName());
            fileSystem.File.WriteAllText(path, content);

            fileSystem.File.ReadAllText(path).Should().Be(content);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FileThrowsWhenTryToReadNonExistFile(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directroy = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string path = Path.Combine(directroy, Path.GetRandomFileName());

            Action a = () => fileSystem.File.ReadAllText(path);
            a.ShouldThrow<FileNotFoundException>().And.Message.Should().Contain("Could not find file");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FileThrowsWhenTryToReadADictonary(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = Path.Combine(
                fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath,
                Path.GetRandomFileName());
            fileSystem.Directory.CreateDirectory(directory);

            Action a = () => fileSystem.File.ReadAllText(directory);
            a.ShouldThrow<UnauthorizedAccessException>().And.Message.Should().Contain("Access to the path");
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
