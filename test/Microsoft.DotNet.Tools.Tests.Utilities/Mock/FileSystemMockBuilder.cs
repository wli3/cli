// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.DotNet.Tools.Test.Utilities.Mock;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    internal class FileSystemMockBuilder
    {
        private MockFileSystemModel _mockFileSystemModel;
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
            // TODO WUL this is wrong, just call model
            foreach (string file in files) AddFile(Path.Combine(basePath, file));
            return this;
        }

        internal IFileSystem Build()
        {
            _mockFileSystemModel =
                new MockFileSystemModel(TemporaryFolder, fileSystemMockWorkingDirectory: WorkingDirectory);

            return new FileSystemMock(_mockFileSystemModel);
        }

        private class MockFileSystemModel
        {
            public MockFileSystemModel(string temporaryFolder,
                FileSystemRoot files = null,
                string fileSystemMockWorkingDirectory = null)
            {
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

                WorkingDirectory = fileSystemMockWorkingDirectory;
                TemporaryFolder = temporaryFolder ?? throw new ArgumentNullException(nameof(temporaryFolder));
                Files = files ?? new FileSystemRoot();
                CreateDirectory(WorkingDirectory);
            }

            public string WorkingDirectory { get; }
            public string TemporaryFolder { get; }
            public FileSystemRoot Files { get; }

            public bool TryGetLastNodeParent(string path, out DirectoryNode current)
            {
                PathModel pathModule = CreateFullPathModule(path);
                current = Files.Volume[pathModule.Volume];

                if (!Files.Volume.ContainsKey(pathModule.Volume))
                {
                    return false;
                }

                for (int i = 0; i < pathModule.PathArray.Length - 1; i++)
                {
                    string p = pathModule.PathArray[i];
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

            public void CreateDirectory(string path)
            {
                PathModel pathModule = CreateFullPathModule(path);

                DirectoryNode current;
                if (!Files.Volume.ContainsKey(pathModule.Volume))
                {
                    current = new DirectoryNode();
                    Files.Volume[pathModule.Volume] = current;
                }
                else
                {
                    current = Files.Volume[pathModule.Volume];
                }

                foreach (string p in pathModule.PathArray)
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
                            $"Cannot create '{pathModule}' because a file or directory with the same name already exists.");
                    }
                }
            }

            private PathModel CreateFullPathModule(string path)
            {
                if (!Path.IsPathRooted(path))
                {
                    path = Path.Combine(WorkingDirectory, path);
                }

                PathModel pathModule = new PathModel(path);

                return pathModule;
            }

            public void CreateFile(string path, string content)
            {
                PathModel pathModule = CreateFullPathModule(path);

                if (TryGetLastNodeParent(path, out DirectoryNode current))
                {
                    if (current != null)
                    {
                        if (current.Subs.ContainsKey(pathModule.FileOrDirectoryName()))
                        {
                            IFileSystemTreeNode possibleConflict = current.Subs[pathModule.FileOrDirectoryName()];
                            if (possibleConflict is DirectoryNode)
                            {
                                throw new IOException($"{path} is a directory");
                            }
                        }
                        else
                        {
                            current.Subs[pathModule.FileOrDirectoryName()] = new FileNode(content);
                        }
                    }
                }
                else
                {
                    throw new DirectoryNotFoundException($"{path} is a directory");
                }
            }

            public (DirectoryNode, FileNode) GetParentDirectoryAndFileNode(string path, Action onNotAFile)
            {
                if (TryGetLastNodeParent(path, out DirectoryNode current) && current != null)
                {
                    PathModel pathModule = new PathModel(path);
                    if (current.Subs.ContainsKey(pathModule.FileOrDirectoryName()))
                    {
                        if (!(current.Subs[pathModule.FileOrDirectoryName()] is FileNode fileNode))
                        {
                            onNotAFile();
                        }
                        else
                        {
                            return (current, fileNode);
                        }
                    }
                }

                throw new FileNotFoundException($"Could not find file '{path}'");
            }
        }

        private class PathModel
        {
            public PathModel(string path)
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

                string[] pathArray = path.Split(
                    new[] {directorySeparatorChar, altDirectorySeparatorChar},
                    StringSplitOptions.RemoveEmptyEntries);
                Volume = volume;
                PathArray = pathArray;
                IsRootded = isRooted;
            }

            public PathModel(bool isRootded, string volume, string[] pathArray)
            {
                IsRootded = isRootded;
                Volume = volume ?? throw new ArgumentNullException(nameof(volume));
                PathArray = pathArray ?? throw new ArgumentNullException(nameof(pathArray));
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

            public string FileOrDirectoryName()
            {
                return PathArray[PathArray.Length - 1];
            }
        }

        private class FileSystemMock : IFileSystem
        {
            public FileSystemMock(MockFileSystemModel files)
            {
                if (files == null)
                {
                    throw new ArgumentNullException(nameof(files));
                }

                File = new FileMock(files);
                Directory = new DirectoryMock(files);
            }

            public IFile File { get; }

            public IDirectory Directory { get; }
        }

        // fasade
        private class FileMock : IFile
        {
            private readonly MockFileSystemModel _files;

            public FileMock(MockFileSystemModel files)
            {
                _files = files ?? throw new ArgumentNullException(nameof(files));
            }

            public bool Exists(string path)
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                if (_files.TryGetLastNodeParent(path, out DirectoryNode current))
                {
                    if (current != null)
                    {
                        PathModel pathModule = new PathModel(path);
                        return current.Subs.ContainsKey(pathModule.FileOrDirectoryName())
                               && current.Subs[pathModule.FileOrDirectoryName()] is FileNode;
                    }
                }

                return false;
            }

            public string ReadAllText(string path)
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                if (_files.TryGetLastNodeParent(path, out DirectoryNode current) && current != null)
                {
                    PathModel pathModule = new PathModel(path);
                    if (current.Subs.ContainsKey(pathModule.FileOrDirectoryName()))
                    {
                        if (!(current.Subs[pathModule.FileOrDirectoryName()] is FileNode fileNode))
                        {
                            throw new UnauthorizedAccessException($"Access to the path '{path}' is denied.");
                        }

                        return fileNode.Content;
                    }
                }

                throw new FileNotFoundException($"Could not find file '{path}'");
            }

            public Stream OpenRead(string path)
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                return new MemoryStream(Encoding.UTF8.GetBytes(ReadAllText(path)));
            }

            public Stream OpenFile(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare,
                int bufferSize,
                FileOptions fileOptions)
            {
                if (fileMode == FileMode.Open && fileAccess == FileAccess.Read)
                {
                    return OpenRead(path);
                }

                throw new NotImplementedException();
            }

            public void CreateEmptyFile(string path)
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                _files.CreateFile(path, string.Empty);
            }

            public void WriteAllText(string path, string content)
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                if (content == null)
                {
                    throw new ArgumentNullException(nameof(content));
                }

                _files.CreateFile(path, content);
            }

            public void Move(string source, string destination)
            {
                if (source == null)
                {
                    throw new ArgumentNullException(nameof(source));
                }

                if (destination == null)
                {
                    throw new ArgumentNullException(nameof(destination));
                }

                (DirectoryNode sourceParent, FileNode sourceFileNode)
                    = _files.GetParentDirectoryAndFileNode(
                        source,
                        () => throw new FileNotFoundException($"Could not find file '{source}'"));

                sourceParent.Subs.Remove(new PathModel(source).FileOrDirectoryName());

                if (_files.TryGetLastNodeParent(destination, out DirectoryNode current) && current != null)
                {
                    current.Subs.Add(new PathModel(destination).FileOrDirectoryName(), sourceFileNode);
                }
                else
                {
                    throw new DirectoryNotFoundException($"Could not find a part of the path {destination}");
                }
            }

            public void Copy(string source, string destination)
            {
                if (source == null)
                {
                    throw new ArgumentNullException(nameof(source));
                }

                if (destination == null)
                {
                    throw new ArgumentNullException(nameof(destination));
                }

                (_, FileNode sourceFileNode) = _files.GetParentDirectoryAndFileNode(source,
                    () => throw new UnauthorizedAccessException($"Access to the path {source} is denied")
                );

                if (_files.TryGetLastNodeParent(destination, out DirectoryNode current) && current != null)
                {
                    if (current.Subs.ContainsKey(new PathModel(destination).FileOrDirectoryName()))
                    {
                        throw new IOException($"Path {destination} already exists");
                    }

                    current.Subs.Add(new PathModel(destination).FileOrDirectoryName(),
                        new FileNode(sourceFileNode.Content));
                }
                else
                {
                    throw new DirectoryNotFoundException($"Could not find a part of the path {destination}");
                }
            }

            public void Delete(string path)
            {
                if (!Exists(path))
                {
                    return;
                }

                if (_files.TryGetLastNodeParent(path, out DirectoryNode current))
                {
                    if (current != null)
                    {
                        PathModel pathModule = new PathModel(path);
                        current.Subs.Remove(pathModule.FileOrDirectoryName());
                    }
                }
            }
        }

        // fasade
        private class DirectoryMock : IDirectory
        {
            private readonly MockFileSystemModel _files;

            public DirectoryMock(MockFileSystemModel files)
            {
                if (files != null)
                {
                    _files = files;
                }
            }

            public bool Exists(string path)
            {
                if (_files.TryGetLastNodeParent(path, out DirectoryNode current))
                {
                    if (current != null)
                    {
                        PathModel pathModule = new PathModel(path);

                        return current.Subs.ContainsKey(pathModule.FileOrDirectoryName())
                               && current.Subs[pathModule.FileOrDirectoryName()] is DirectoryNode;
                    }
                }

                return false;
            }

            public ITemporaryDirectory CreateTemporaryDirectory()
            {
                TemporaryDirectoryMock temporaryDirectoryMock = new TemporaryDirectoryMock(_files.TemporaryFolder);
                CreateDirectory(temporaryDirectoryMock.DirectoryPath);
                return temporaryDirectoryMock;
            }

            public IEnumerable<string> EnumerateAllFiles(string path)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> EnumerateFileSystemEntries(string path)
            {
                throw new NotImplementedException();
            }

            public string GetCurrentDirectory()
            {
                return _files.WorkingDirectory;
            }

            public void CreateDirectory(string path)
            {
                _files.CreateDirectory(path);
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
                List<string> lines = new List<string>();

                foreach (KeyValuePair<string, IFileSystemTreeNode> fileSystemTreeNode in Subs)
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
                List<string> lines = new List<string>();

                foreach (KeyValuePair<string, DirectoryNode> fileSystemTreeNode in Volume)
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
