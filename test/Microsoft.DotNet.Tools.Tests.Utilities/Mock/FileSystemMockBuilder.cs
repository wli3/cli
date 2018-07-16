﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Tools.Test.Utilities.Mock;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    internal class FileSystemMockBuilder
    {
        private FileSystemRoot _files;
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
                {
                    fileSystemMockWorkingDirectory = @"C:\";
                }
                else
                {
                    fileSystemMockWorkingDirectory = "/";
                }
            }

            _files = new FileSystemRoot();

            return new FileSystemMock(_files, TemporaryFolder, fileSystemMockWorkingDirectory);
        }

        private static bool TryGetLastNodeParent(FileSystemRoot fileSystemRoot, string path, out DirectoryNode current)
        {
            var pathModule = new PathModule(path);
            current = fileSystemRoot.Volume[pathModule.Volume];

            if (!fileSystemRoot.Volume.ContainsKey(pathModule.Volume))
            {
                return false;
            }

            for (int i = 0; i < pathModule.PathArray.Length - 1; i++)
            {
                var p = pathModule.PathArray[i];
                if (!current.Subs.ContainsKey(p))
                {
                    return false;
                }

                if (current.Subs[p] is DirectoryNode directoryNode)
                {
                    current = directoryNode;
                }
                else if (current.Subs[p] is FileNode)
                {
                    return false;
                }
            }

            return true;
        }


        public class PathModule
        {
            public PathModule(string path)
            {
                const char directorySeparatorChar = '\\';
                const char altDirectorySeparatorChar = '/';

                bool isRooted = false;
                if (string.IsNullOrWhiteSpace(path))
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
                Volume = volume;
                PathArray = pathArray;
                IsRootded = isRooted;
            }

            public bool IsRootded { get; }
            public string Volume { get; }
            public string[] PathArray { get; }

            public override string ToString()
            {
                return $"{nameof(IsRootded)}: {IsRootded}" +
                       $", {nameof(Volume)}: {Volume}" +
                       $", {nameof(PathArray)}: {string.Join("-", PathArray)}";
            }
        }

        private class FileSystemMock : IFileSystem
        {
            public FileSystemMock(FileSystemRoot files, string temporaryFolder, string workingDirectory)
            {
                if (files == null)
                {
                    throw new ArgumentNullException(nameof(files));
                }

                if (temporaryFolder == null)
                {
                    throw new ArgumentNullException(nameof(temporaryFolder));
                }

                // Don't support change working directory
                if (workingDirectory == null) throw new ArgumentNullException(nameof(workingDirectory));

                File = new FileMock(files, workingDirectory);
                Directory = new DirectoryMock(files, temporaryFolder, workingDirectory);
            }

            public IFile File { get; }

            public IDirectory Directory { get; }
        }

        private class FileMock : IFile
        {
            private readonly FileSystemRoot _files;
            private readonly string _workingDirectory;

            public FileMock(FileSystemRoot files, string workingDirectory)
            {
                _files = files ?? throw new ArgumentNullException(nameof(files));
                _workingDirectory = workingDirectory;
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
                if (TryGetLastNodeParent(_files, path, out var current))
                {
                    if (current != null)
                    {
                        var pathModule = new PathModule(path);
                        if (current.Subs.ContainsKey(pathModule.PathArray.Last()))
                        {
                            var possibleConflict = current.Subs[pathModule.PathArray.Last()];
                            if (possibleConflict is DirectoryNode)
                            {
                                throw new IOException($"{path} is a directory");
                            }
                        }
                        else
                        {
                            current.Subs[pathModule.PathArray.Last()] = new FileNode("");
                        }
                    }
                }
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
            private readonly FileSystemRoot _files;
            private readonly TemporaryDirectoryMock _temporaryDirectory;
            private readonly string _workingDirectory;

            public DirectoryMock(FileSystemRoot files, string temporaryDirectory, string workingDirectory)
            {
                _workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
                if (files != null)
                {
                    _files = files;
                }

                _temporaryDirectory = new TemporaryDirectoryMock(temporaryDirectory);
            }

            public bool Exists(string path)
            {
                if (TryGetLastNodeParent(_files, path, out var current))
                {
                    if (current != null)
                    {
                        var pathModule = new PathModule(path);
                        return current.Subs.ContainsKey(pathModule.PathArray.Last())
                               && current.Subs[pathModule.PathArray.Last()] is DirectoryNode;
                    }
                }

                return false;
            }

            public ITemporaryDirectory CreateTemporaryDirectory()
            {
                CreateDirectory(_temporaryDirectory.DirectoryPath);
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
                var pathModule = new PathModule(path);

                DirectoryNode current;
                if (!_files.Volume.ContainsKey(pathModule.Volume))
                {
                    current = new DirectoryNode();
                    _files.Volume[pathModule.Volume] = current;
                }
                else
                {
                    current = _files.Volume[pathModule.Volume];
                }

                foreach (var p in pathModule.PathArray)
                {
                    if (!current.Subs.ContainsKey(p))
                    {
                        DirectoryNode directoryNode = new DirectoryNode();
                        current.Subs[p] = directoryNode;
                        current = directoryNode;
                    }
                    else if (current.Subs[p] is DirectoryNode directoryNode)
                    {
                        current = directoryNode;
                    }
                    else if (current.Subs[p] is FileNode)
                    {
                        throw new IOException(
                            $"Cannot create '{path}' because a file or directory with the same name already exists.");
                    }
                }
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
            IEnumerable<string> DebugShowTreeLines();
        }

        private class DirectoryNode : IFileSystemTreeNode
        {
            public Dictionary<string, IFileSystemTreeNode> Subs { get; } =
                new Dictionary<string, IFileSystemTreeNode>();

            public IEnumerable<string> DebugShowTreeLines()
            {
                var lines = new List<string>();

                foreach (var fileSystemTreeNode in Subs)
                {
                    lines.Add(fileSystemTreeNode.Key);
                    lines.AddRange(fileSystemTreeNode.Value.DebugShowTreeLines().Select(l => "-- " + l));
                }

                return lines;
            }
        }

        private class FileSystemRoot
        {
            // in Linux there is only one Node, and the name is empty
            public Dictionary<string, DirectoryNode> Volume { get; } = new Dictionary<string, DirectoryNode>();

            public IEnumerable<string> DebugShowTree()
            {
                var lines = new List<string>();

                foreach (var fileSystemTreeNode in Volume)
                {
                    lines.Add(fileSystemTreeNode.Key);
                    lines.AddRange(fileSystemTreeNode.Value.DebugShowTreeLines().Select(l => "-- " + l));
                }

                return lines;
            }
        }

        private class FileNode : IFileSystemTreeNode
        {
            public FileNode(string content)
            {
                Content = content ?? throw new ArgumentNullException(nameof(content));
            }

            public string Content { get; } = "";

            public IEnumerable<string> DebugShowTreeLines()
            {
                return new List<string> {Content};
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
