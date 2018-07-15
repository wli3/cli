// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Tools.Test.Utilities.Mock;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    internal class FileSystemMockBuilder
    {
        private VolumeNode _files;
        public string TemporaryFolder { get; set; }
        public string WorkingDirectory { get; set; }

        internal static IFileSystem Empty { get; } = Create().Build();

        public static FileSystemMockBuilder Create()
        {
            return new FileSystemMockBuilder();
        }

        public FileSystemMockBuilder AddFile(string name, string content = "")
        {
            // _files.Add(name, content); TODO wul add files
            return this;
        }

        public FileSystemMockBuilder AddFiles(string basePath, params string[] files)
        {
            foreach (string file in files) AddFile(Path.Combine(basePath, file));
            return this;
        }

        internal IFileSystem Build()
        {
            string fileSystemMockWorkingDirectory = WorkingDirectory;
            if (fileSystemMockWorkingDirectory == null)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    fileSystemMockWorkingDirectory = @"C:\";
                else
                    fileSystemMockWorkingDirectory = "/";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _files = new VolumeNode("c");
            }
            else
            {
                _files = new VolumeNode("");
            }

            return new FileSystemMock(_files, TemporaryFolder, fileSystemMockWorkingDirectory);
        }

        private static PathAppleSauce PathToArray(string path)
        {
            const char directorySeparatorChar = '\\';
            const char altDirectorySeparatorChar = '/';

            bool isRooted = false;
            if (!string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(nameof(path) + ": " + path);
            }

            string volume = "";
            if (Path.IsPathRooted(path))
            {
                isRooted = true;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    int charLocation = path.IndexOf(":", StringComparison.Ordinal);

                    if (charLocation > 0)
                    {
                        volume = path.Substring(0, charLocation);
                        path = path.Substring(charLocation + 2);
                    }
                }
            }

            string[] pathArray = path.Split(directorySeparatorChar, altDirectorySeparatorChar);
            return new PathAppleSauce(volume, pathArray, isRooted);
        }

        public class PathAppleSauce
        {
            public PathAppleSauce(string volume, string[] pathArray, bool isRootded)
            {
                Volume = volume ?? throw new ArgumentNullException(nameof(volume));
                PathArray = pathArray ?? throw new ArgumentNullException(nameof(pathArray));
                IsRootded = isRootded;
            }

            public bool IsRootded { get; }
            public string Volume { get; }
            public string[] PathArray { get; }
        }

        private class FileSystemMock : IFileSystem
        {
            public FileSystemMock(VolumeNode files, string temporaryFolder, string workingDirectory)
            {
                if (files == null)
                {
                    throw new ArgumentNullException(nameof(files));
                }

                if (temporaryFolder == null)
                {
                    throw new ArgumentNullException(nameof(temporaryFolder));
                }

                WorkingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
                File = new FileMock(files);
                Directory = new DirectoryMock(files, temporaryFolder);
            }

            public string WorkingDirectory { get; set; }

            public IFile File { get; }

            public IDirectory Directory { get; }
        }

        private class FileMock : IFile
        {
            private readonly VolumeNode _files;

            public FileMock(VolumeNode files)
            {
                _files = files ?? throw new ArgumentNullException(nameof(files));
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

            public Stream OpenFile(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare,
                int bufferSize,
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
            private readonly VolumeNode _files;
            private readonly TemporaryDirectoryMock _temporaryDirectory;

            public DirectoryMock(VolumeNode files, string temporaryDirectory)
            {
                if (files != null) _files = files;
                _temporaryDirectory = new TemporaryDirectoryMock(temporaryDirectory);
            }

            public bool Exists(string path)
            {
                throw new NotImplementedException();
            }

            public ITemporaryDirectory CreateTemporaryDirectory()
            {
                return _temporaryDirectory;
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
            string Name { get; set; }
        }

        private class DirectoryNode : IFileSystemTreeNode
        {
            public DirectoryNode(string name)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            public SortedSet<IFileSystemTreeNode> Subs { get; set; } = new SortedSet<IFileSystemTreeNode>();

            public string Name { get; set; }

            protected bool Equals(DirectoryNode other)
            {
                return string.Equals(Name, other.Name);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((DirectoryNode)obj);
            }

            public override int GetHashCode()
            {
                return Name != null ? Name.GetHashCode() : 0;
            }
        }

        private class VolumeNode : IFileSystemTreeNode
        {
            public VolumeNode(string name)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            public SortedSet<IFileSystemTreeNode> Subs { get; set; } = new SortedSet<IFileSystemTreeNode>();

            public string Name { get; set; }
        }

        private class FileNode : IFileSystemTreeNode
        {
            public FileNode(string name)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            public string Content { get; set; } = "";

            public string Name { get; set; }

            protected bool Equals(FileNode other)
            {
                return string.Equals(Name, other.Name);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((FileNode)obj);
            }

            public override int GetHashCode()
            {
                return Name != null ? Name.GetHashCode() : 0;
            }
        }

        private class TemporaryDirectoryMock : ITemporaryDirectoryMock
        {
            public TemporaryDirectoryMock(string temporaryDirectory)
            {
                DirectoryPath = temporaryDirectory;
            }

            public bool DisposedTemporaryDirectory { get; private set; }

            public string DirectoryPath { get; }

            public void Dispose()
            {
                DisposedTemporaryDirectory = true;
            }
        }
    }
}
