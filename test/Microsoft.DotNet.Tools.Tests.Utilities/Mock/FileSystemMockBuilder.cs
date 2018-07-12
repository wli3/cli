// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.DotNet.Tools.Test.Utilities.Mock;
using Microsoft.Extensions.EnvironmentAbstractions;

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
                File = new FileMock(files);
                Directory = new DirectoryMock(files, temporaryFolder);
            }

            public IFile File { get; }

            public IDirectory Directory { get; }
        }

        private class FileMock : IFile
        {
            public FileMock(Dictionary<string, string> files)
            {
                throw new NotImplementedException();
            }

            public bool Exists(string path)
            {
                throw new NotImplementedException();
            }

            public string ReadAllText(string path)
            {
                throw new NotImplementedException();
            }

            public Stream OpenRead(string path)
            {
                throw new NotImplementedException();
            }

            public Stream OpenFile(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, int bufferSize,
                FileOptions fileOptions)
            {
                throw new NotImplementedException();
            }

            public void CreateEmptyFile(string path)
            {
                throw new NotImplementedException();
            }

            public void WriteAllText(string path, string content)
            {
                throw new NotImplementedException();
            }

            public void Move(string source, string destination)
            {
                throw new NotImplementedException();
            }

            public void Copy(string source, string destination)
            {
                throw new NotImplementedException();
            }

            public void Delete(string path)
            {
                throw new NotImplementedException();
            }
        }

        private class DirectoryMock : IDirectory
        {
            public DirectoryMock(Dictionary<string, string> files, string temporaryFolder)
            {
                throw new NotImplementedException();
            }

            public bool Exists(string path)
            {
                throw new NotImplementedException();
            }

            public ITemporaryDirectory CreateTemporaryDirectory()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> EnumerateFiles(string path, string searchPattern)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> EnumerateFileSystemEntries(string path)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern)
            {
                throw new NotImplementedException();
            }

            public string GetDirectoryFullName(string path)
            {
                throw new NotImplementedException();
            }

            public void CreateDirectory(string path)
            {
                throw new NotImplementedException();
            }

            public void Delete(string path, bool recursive)
            {
                throw new NotImplementedException();
            }

            public void Move(string source, string destination)
            {
                throw new NotImplementedException();
            }
        }

        private interface IFileSystemTreeNode
        {
            string Name { get; set;}
        }
        
        private class DirectoryNode : IFileSystemTreeNode
        {
            public DirectoryNode(string name)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            public string Name { get; set; }
            public List<IFileSystemTreeNode> Subs { get; set; } = new List<IFileSystemTreeNode>();
        }
        
        private class FileNode : IFileSystemTreeNode
        {
            public FileNode(string name)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            public string Name { get;  set; }
            public string Content { get; set; } = "";
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
