// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
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
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string nestedFilePath = Path.Combine(directory, "filename");
            fileSystem.File.CreateEmptyFile(nestedFilePath);

            fileSystem.Directory.Exists(nestedFilePath).Should().BeFalse();
        }

        [WindowsOnlyTheory]
        [InlineData(false)]
        [InlineData(true)]
        public void DifferentDirectorySeparatorShouldBeSameFile(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string nestedFilePath = $"{directory}\\filename";
            fileSystem.File.CreateEmptyFile(nestedFilePath);

            fileSystem.File.Exists($"{directory}/filename").Should().BeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WhenDirectoryExistsShouldCreateEmptyFile(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string nestedFilePath = Path.Combine(directory, "filename");
            fileSystem.File.CreateEmptyFile(nestedFilePath);

            fileSystem.File.Exists(nestedFilePath).Should().BeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DirectoryExistsWithRelativePathShouldCountTheSameNameFile(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directory = fileSystem.Directory.GetCurrentDirectory();
            fileSystem.File.CreateEmptyFile("file");

            fileSystem.File.Exists(Path.Combine(directory, "file")).Should().BeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithRelativePathShouldCreateDirectory(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directory = fileSystem.Directory.GetCurrentDirectory();
            fileSystem.Directory.CreateDirectory("dir");

            fileSystem.Directory.Exists(Path.Combine(directory, "dir")).Should().BeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ShouldCreateDirectory(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;

            fileSystem.Directory.Exists(directory).Should().BeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CreateDirectoryWhenExistsShouldNotThrow(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;

            Action a = () => fileSystem.Directory.CreateDirectory(directory);
            a.ShouldNotThrow();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CreateDirectoryWhenExistsSameNameFileShouldThrow(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);

            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;

            string path = Path.Combine(directory, "sub");
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

            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string nestedFilePath = Path.Combine(directory, "subfolder", "filename");
            Action a = () => fileSystem.File.CreateEmptyFile(nestedFilePath);
            a.ShouldThrow<DirectoryNotFoundException>();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FileReadAllTextWhenExists(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            const string content = "content";
            string path = Path.Combine(directory, Path.GetRandomFileName());
            fileSystem.File.WriteAllText(path, content);

            fileSystem.File.ReadAllText(path).Should().Be(content);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FileThrowsWhenTryToReadNonExistFile(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string path = Path.Combine(directory, Path.GetRandomFileName());

            Action a = () => fileSystem.File.ReadAllText(path);
            a.ShouldThrow<FileNotFoundException>().And.Message.Should().Contain("Could not find file");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FileThrowsWhenTryToReadADictionary(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = Path.Combine(
                fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath,
                Path.GetRandomFileName());
            fileSystem.Directory.CreateDirectory(directory);

            Action a = () => fileSystem.File.ReadAllText(directory);
            a.ShouldThrow<UnauthorizedAccessException>().And.Message.Should().Contain("Access to the path");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FileOpenReadWhenExists(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            const string content = "content";
            string path = Path.Combine(directory, Path.GetRandomFileName());
            fileSystem.File.WriteAllText(path, content);

            string fullString = "";
            using (Stream fs = fileSystem.File.OpenRead(path))
            {
                byte[] b = new byte[1024];
                UTF8Encoding temp = new UTF8Encoding(true);

                while (fs.Read(b, 0, b.Length) > 0)
                {
                    fullString += temp.GetString(b);
                }
            }

            fullString.Should().StartWith(content);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MoveFileWhenBothSourceAndDestinationExist(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string sourceFile = Path.Combine(directory, Path.GetRandomFileName());
            fileSystem.File.CreateEmptyFile(sourceFile);
            string destinationFile = Path.Combine(directory, Path.GetRandomFileName());

            fileSystem.File.Move(sourceFile, destinationFile);

            fileSystem.File.Exists(sourceFile).Should().BeFalse();
            fileSystem.File.Exists(destinationFile).Should().BeTrue();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MoveFileThrowsWhenSourceDoesNotExist(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string sourceFile = Path.Combine(directory, Path.GetRandomFileName());

            string destinationFile = Path.Combine(directory, Path.GetRandomFileName());

            Action a = () => fileSystem.File.Move(sourceFile, destinationFile);

            a.ShouldThrow<FileNotFoundException>().And.Message.Should().Contain("Could not find file");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MoveFileThrowsWhenSourceIsADirectory(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string badSourceFile = Path.Combine(directory, Path.GetRandomFileName());
            fileSystem.Directory.CreateDirectory(badSourceFile);

            string destinationFile = Path.Combine(directory, Path.GetRandomFileName());

            Action a = () => fileSystem.File.Move(badSourceFile, destinationFile);

            a.ShouldThrow<FileNotFoundException>().And.Message.Should().Contain("Could not find file");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MoveFileThrowsWhenDestinationDirectoryDoesNotExist(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string sourceFile = Path.Combine(directory, Path.GetRandomFileName());
            fileSystem.File.CreateEmptyFile(sourceFile);

            string destinationFile = Path.Combine(directory, Path.GetRandomFileName(), Path.GetRandomFileName());

            Action a = () => fileSystem.File.Move(sourceFile, destinationFile);

            a.ShouldThrow<DirectoryNotFoundException>()
                .And.Message.Should().Contain("Could not find a part of the path");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CopyFileWhenBothSourceAndDestinationDirectoryExist(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string sourceFile = Path.Combine(directory, Path.GetRandomFileName());
            fileSystem.File.WriteAllText(sourceFile, "content");
            string destinationFile = Path.Combine(directory, Path.GetRandomFileName());

            fileSystem.File.Copy(sourceFile, destinationFile);

            fileSystem.File.ReadAllText(sourceFile).Should().Be(fileSystem.File.ReadAllText(destinationFile));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CopyFileThrowsWhenSourceDoesNotExist(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string sourceFile = Path.Combine(directory, Path.GetRandomFileName());
            string destinationFile = Path.Combine(directory, Path.GetRandomFileName());

            Action a = () => fileSystem.File.Copy(sourceFile, destinationFile);

            a.ShouldThrow<FileNotFoundException>().And.Message.Should().Contain("Could not find file");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CopyFileThrowsWhenSourceIsADirectory(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string badSourceFile = Path.Combine(directory, Path.GetRandomFileName());
            fileSystem.Directory.CreateDirectory(badSourceFile);
            string destinationFile = Path.Combine(directory, Path.GetRandomFileName());

            Action a = () => fileSystem.File.Copy(badSourceFile, destinationFile);

            a.ShouldThrow<UnauthorizedAccessException>().And.Message.Should().Contain("Access to the path");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CopyFileThrowsWhenDestinationDirectoryDoesNotExist(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string sourceFile = Path.Combine(directory, Path.GetRandomFileName());
            fileSystem.File.CreateEmptyFile(sourceFile);
            string destinationFile = Path.Combine(directory, Path.GetRandomFileName(), Path.GetRandomFileName());

            Action a = () => fileSystem.File.Copy(sourceFile, destinationFile);

            a.ShouldThrow<DirectoryNotFoundException>()
                .And.Message.Should().Contain("Could not find a part of the path");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CopyFileThrowsWhenDestinationExists(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string sourceFile = Path.Combine(directory, Path.GetRandomFileName());
            fileSystem.File.CreateEmptyFile(sourceFile);
            string destinationFile = Path.Combine(directory, Path.GetRandomFileName());
            fileSystem.File.CreateEmptyFile(destinationFile);

            Action a = () => fileSystem.File.Copy(sourceFile, destinationFile);

            a.ShouldThrow<IOException>()
                .And.Message.Should().Contain("already exists");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DeleteFile(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string file = Path.Combine(directory, Path.GetRandomFileName());
            fileSystem.File.CreateEmptyFile(file);

            fileSystem.File.Delete(file);

            fileSystem.File.Exists(file).Should().BeFalse();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DeleteFileShouldNotThrowWhenFileDoesNotExists(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string file = Path.Combine(directory, Path.GetRandomFileName());

            Action a = () => fileSystem.File.Delete(file);

            a.ShouldNotThrow();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DeleteFileShouldNotThrowWhenDirectoryDoesNotExists(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string file = Path.Combine(directory, Path.GetRandomFileName(), Path.GetRandomFileName());

            Action a = () => fileSystem.File.Delete(file);

            a.ShouldNotThrow();
        }


        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EnumerateAllFilesThrowsWhenDirectoryDoesNotExists(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string nonExistDirectory = Path.Combine(directory, Path.GetRandomFileName(), Path.GetRandomFileName());

            Action a = () => fileSystem.Directory.EnumerateAllFiles(nonExistDirectory);

            a.ShouldThrow<DirectoryNotFoundException>().And.Message.Should()
                .Contain("Could not find a part of the path");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EnumerateAllFilesThrowsWhenPathIsAFile(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string wrongFilePath = Path.Combine(directory, Path.GetRandomFileName());
            fileSystem.File.CreateEmptyFile(wrongFilePath);

            Action a = () => fileSystem.Directory.EnumerateAllFiles(wrongFilePath);

            a.ShouldThrow<IOException>().And.Message.Should()
                .Contain("Not a directory");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WhenEmptyEnumerateAllFiles(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string tempDirectory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string emptyDirectory = Path.Combine(tempDirectory, Path.GetRandomFileName());
            fileSystem.Directory.CreateDirectory(emptyDirectory);

            fileSystem.Directory.EnumerateAllFiles(emptyDirectory).Should().BeEmpty();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WhenFilesExistEnumerateAllFiles(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string tempDirectory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string testDirectory = Path.Combine(tempDirectory, Path.GetRandomFileName());
            string file1 = Path.Combine(testDirectory, Path.GetRandomFileName());
            string file2 = Path.Combine(testDirectory, Path.GetRandomFileName());

            fileSystem.Directory.CreateDirectory(testDirectory);
            fileSystem.File.CreateEmptyFile(file1);
            fileSystem.File.CreateEmptyFile(file2);

            fileSystem.Directory.EnumerateAllFiles(testDirectory).Should().Contain(file1);
            fileSystem.Directory.EnumerateAllFiles(testDirectory).Should().Contain(file2);
        }


        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EnumerateFileSystemEntriesThrowsWhenDirectoryDoesNotExists(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string nonExistDirectory = Path.Combine(directory, Path.GetRandomFileName(), Path.GetRandomFileName());

            Action a = () => fileSystem.Directory.EnumerateFileSystemEntries(nonExistDirectory);

            a.ShouldThrow<DirectoryNotFoundException>().And.Message.Should()
                .Contain("Could not find a part of the path");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EnumerateFileSystemEntriesThrowsWhenPathIsAFile(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string directory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string wrongFilePath = Path.Combine(directory, Path.GetRandomFileName());
            fileSystem.File.CreateEmptyFile(wrongFilePath);

            Action a = () => fileSystem.Directory.EnumerateFileSystemEntries(wrongFilePath);

            a.ShouldThrow<IOException>().And.Message.Should()
                .Contain("Not a directory");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WhenEmptyEnumerateFileSystemEntries(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string tempDirectory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string emptyDirectory = Path.Combine(tempDirectory, Path.GetRandomFileName());
            fileSystem.Directory.CreateDirectory(emptyDirectory);

            fileSystem.Directory.EnumerateFileSystemEntries(emptyDirectory).Should().BeEmpty();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WhenFilesExistEnumerateFileSystemEntries(bool testMockBehaviorIsInSync)
        {
            IFileSystem fileSystem = SetupSubjectFileSystem(testMockBehaviorIsInSync);
            string tempDirectory = fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;
            string testDirectory = Path.Combine(tempDirectory, Path.GetRandomFileName());
            string file1 = Path.Combine(testDirectory, Path.GetRandomFileName());
            string file2 = Path.Combine(testDirectory, Path.GetRandomFileName());
            string nestedDirectoryPath = Path.Combine(testDirectory, Path.GetRandomFileName());

            fileSystem.Directory.CreateDirectory(testDirectory);
            fileSystem.File.CreateEmptyFile(file1);
            fileSystem.File.CreateEmptyFile(file2);
            fileSystem.Directory.CreateDirectory(nestedDirectoryPath);

            fileSystem.Directory.EnumerateFileSystemEntries(testDirectory).Should().Contain(file1);
            fileSystem.Directory.EnumerateFileSystemEntries(testDirectory).Should().Contain(file2);
            fileSystem.Directory.EnumerateFileSystemEntries(testDirectory).Should().Contain(nestedDirectoryPath);
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
