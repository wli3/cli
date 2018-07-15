// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Tools.Test.Utilities.Mock;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Common;

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

        
        public class PathAppleSauce
        {
            public PathAppleSauce(string path)
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

                File = new FileMock(files ,workingDirectory);
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
            private readonly string _workingDirectory;
            private readonly FileSystemRoot _files;
            private readonly TemporaryDirectoryMock _temporaryDirectory;

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
                // TODO could extract this
                var pathAppleSauce = new PathAppleSauce(path);
                DirectoryNode current;
                if (!_files.Volume.ContainsKey(pathAppleSauce.Volume))
                {
                    return false;
                }
                else
                {
                    current = _files.Volume[pathAppleSauce.Volume];
                }

                foreach (var p in pathAppleSauce.PathArray)
                {
                    if (!current.Subs.ContainsKey(p))
                    {
                        return false;
                    }
                    else if (current.Subs[p] is DirectoryNode directoryNode)
                    {
                        current = directoryNode;
                    }
                    else if (current.Subs[p] is FileNode)
                    {
                        return false;
                    }
                }

                if (current != null)
                {
                    return true;
                }

                return false;
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
                var pathAppleSauce = new PathAppleSauce(path);
                DirectoryNode current;
                if (!_files.Volume.ContainsKey(pathAppleSauce.Volume))
                {
                    current = new DirectoryNode();
                    _files.Volume[pathAppleSauce.Volume] = current;
                }
                else
                {
                    current = _files.Volume[pathAppleSauce.Volume];
                }

                foreach (var p in pathAppleSauce.PathArray)
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
            public Dictionary<string, IFileSystemTreeNode> Subs { get; set; } =
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
            public Dictionary<string, DirectoryNode> Volume { get; set; } = new Dictionary<string, DirectoryNode>();
            
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

            public string Content { get; set; } = "";

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
