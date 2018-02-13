// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Tools.Tests.ComponentMocks
{
    internal class ToolPackageMock : IToolPackage
    {
        private IFileSystem _fileSystem;
        private Lazy<IReadOnlyList<ToolCommand>> _commands;
        private Action _uninstallCallback;

        public ToolPackageMock(
            IFileSystem fileSystem,
            string packageId,
            string packageVersion,
            DirectoryPath packageDirectory,
            Action uninstallCallback = null)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            PackageId = packageId ?? throw new ArgumentNullException(nameof(packageId));
            PackageVersion = packageVersion ?? throw new ArgumentNullException(nameof(packageVersion));
            PackageDirectory = packageDirectory;
            _commands = new Lazy<IReadOnlyList<ToolCommand>>(GetCommands);
            _uninstallCallback = uninstallCallback;
        }

        public string PackageId { get; private set; }

        public string PackageVersion { get; private set; }

        public DirectoryPath PackageDirectory { get; private set; }

        public IReadOnlyList<ToolCommand> Commands
        {
            get
            {
                return _commands.Value;
            }
        }

        public void Uninstall()
        {
            var rootDirectory = PackageDirectory.GetParentPath();
            string tempPackageDirectory = null;

            TransactionalAction.Run(
                action: () => {
                    if (_fileSystem.Directory.Exists(PackageDirectory.Value))
                    {
                        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                        _fileSystem.Directory.Move(PackageDirectory.Value, tempPath);
                        tempPackageDirectory = tempPath;
                    }

                    if (_fileSystem.Directory.Exists(rootDirectory.Value) &&
                        !_fileSystem.Directory.EnumerateFileSystemEntries(rootDirectory.Value).Any())
                    {
                        _fileSystem.Directory.Delete(rootDirectory.Value, false);
                    }

                    if (_uninstallCallback != null)
                    {
                        _uninstallCallback();
                    }
                },
                commit: () => {
                    if (tempPackageDirectory != null)
                    {
                        _fileSystem.Directory.Delete(tempPackageDirectory, true);
                    }
                },
                rollback: () => {
                    if (tempPackageDirectory != null)
                    {
                        _fileSystem.Directory.CreateDirectory(rootDirectory.Value);
                        _fileSystem.Directory.Move(tempPackageDirectory, PackageDirectory.Value);
                    }
                });
        }

        private IReadOnlyList<ToolCommand> GetCommands()
        {
            try
            {
                // The mock restorer wrote the path to the executable into project.assets.json (not a real assets file)
                var executablePath = _fileSystem.File.ReadAllText(Path.Combine(PackageDirectory.Value, "project.assets.json"));
                return new ToolCommand[]
                {
                    new ToolCommand(ProjectRestorerMock.FakeCommandName, PackageDirectory.WithFile(executablePath))
                };
            }
            catch (IOException ex)
            {
                throw new ToolPackageException(
                    string.Format(
                        CommonLocalizableStrings.FailedToRetrieveToolConfiguration,
                        PackageId,
                        ex.Message),
                    ex);
            }
        }
    }
}
