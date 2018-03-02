// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.DotNet.Tools.Test.Utilities.Mock;
using Microsoft.Extensions.EnvironmentAbstractions;
using System.IO.Abstractions.TestingHelpers;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    class FileSystemMockBuilder
    {
        private Dictionary<string, string> _files = new Dictionary<string, string>();

        public string TemporaryFolder { get; set; }

        internal static IFileSystem Empty { get; } = Create().Build();

        public static FileSystemMockBuilder Create()
        {
            return new FileSystemMockBuilder();
        }

        public FileSystemMockBuilder AddFile(string name, string content = "")
        {
            _files.Add(name, content);
            return this;
        }

        public FileSystemMockBuilder AddFiles(string basePath, params string[] files)
        {
            foreach (var file in files)
            {
                AddFile(Path.Combine(basePath, file));
            }
            return this;
        }

        internal IFileSystem Build()
        {
            return new FileSystemMock(_files, TemporaryFolder);
        }

        private class FileSystemMock : IFileSystem
        {
            public FileSystemMock(Dictionary<string, string> files, string temporaryFolder)
            {
                MockFileSystem mockFileSystem = new MockFileSystem();

                File = new FileMock(mockFileSystem);
                Directory = new DirectoryMock(mockFileSystem, temporaryFolder);
            }

            public IFile File { get; }

            public IDirectory Directory { get; }
        }

        private class FileMock : IFile
        {

            public FileMock(MockFileSystem mockFileSystem)
            {
                _mockFileSystem = mockFileSystem;
            }

            private MockFileSystem _mockFileSystem;

            public FileMock(Dictionary<string, string> files)
            {
                foreach (var kv in files)
                {
                    WriteAllText(kv.Key, kv.Value);
                }

            }

            public bool Exists(string path)
            {
                return _mockFileSystem.File.Exists(path);
            }

            public string ReadAllText(string path)
            {
                return _mockFileSystem.File.ReadAllText(path);
            }

            public Stream OpenRead(string path)
            {
                return _mockFileSystem.File.OpenRead(path);
            }

            public Stream OpenFile(
                string path,
                FileMode fileMode,
                FileAccess fileAccess,
                FileShare fileShare,
                int bufferSize,
                FileOptions fileOptions)
            {
                throw new NotImplementedException();
            }

            public void CreateEmptyFile(string path)
            {
                WriteAllText(path, string.Empty);
            }

            public void WriteAllText(string path, string content)
            {
                _mockFileSystem.File.WriteAllText(path, content);
            }

            public void Move(string source, string destination)
            {
                _mockFileSystem.File.Move(source, destination);
            }

            public void Delete(string path)
            {
                _mockFileSystem.File.Delete(path);
            }
        }

        private class DirectoryMock : IDirectory
        {
            private readonly MockFileSystem _mockFileSystem;
            private readonly TemporaryDirectoryMock _temporaryDirectory;

            public DirectoryMock(MockFileSystem mockFileSystem, string temporaryDirectory)
            {
                _mockFileSystem = mockFileSystem;
                _temporaryDirectory = new TemporaryDirectoryMock(temporaryDirectory);
            }

            public ITemporaryDirectory CreateTemporaryDirectory()
            {
                return _temporaryDirectory;
            }

            public IEnumerable<string> EnumerateFileSystemEntries(string path)
            {
                return _mockFileSystem.Directory.EnumerateFileSystemEntries(path);
            }

            public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern)
            {
                return _mockFileSystem.Directory.EnumerateFileSystemEntries(path, searchPattern);
            }

            public string GetDirectoryFullName(string path)
            {
                throw new NotImplementedException();
            }

            public bool Exists(string path)
            {
                return _mockFileSystem.Directory.Exists(path);
            }

            public void CreateDirectory(string path)
            {
                _mockFileSystem.Directory.CreateDirectory(path);
            }

            public void Delete(string path, bool recursive)
            {
                _mockFileSystem.Directory.Delete(path, recursive);
            }

            public void Move(string source, string destination)
            {
                _mockFileSystem.Directory.Move(source, destination);
            }
        }

        private class TemporaryDirectoryMock : ITemporaryDirectoryMock
        {
            public bool DisposedTemporaryDirectory { get; private set; }

            public TemporaryDirectoryMock(string temporaryDirectory)
            {
                DirectoryPath = temporaryDirectory;
            }

            public string DirectoryPath { get; }

            public void Dispose()
            {
                DisposedTemporaryDirectory = true;
            }
        }
    }

}
