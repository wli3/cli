using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Tools;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.ProjectModel;

namespace Microsoft.DotNet.ToolPackage
{
    // This is named "ToolPackageInstance" because "ToolPackage" would conflict with the namespace
    internal class ToolPackageInstance : IToolPackage
    {
        private Lazy<IReadOnlyList<ToolCommand>> _commands;

        public ToolPackageInstance(
            string packageId,
            string packageVersion,
            DirectoryPath packageDirectory)
        {
            if (packageId == null)
            {
                throw new ArgumentNullException(nameof(packageId));
            }
            if (packageVersion == null)
            {
                throw new ArgumentNullException(nameof(packageVersion));
            }

            PackageId = packageId;
            PackageVersion = packageVersion;
            PackageDirectory = packageDirectory;
            _commands = new Lazy<IReadOnlyList<ToolCommand>>(GetCommands);
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
            try
            {
                var rootDirectory = PackageDirectory.GetParentPath();
                string tempPackageDirectory = null;

                TransactionalAction.Run(
                    action: () => {
                        if (Directory.Exists(PackageDirectory.Value))
                        {
                            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                            Directory.Move(PackageDirectory.Value, tempPath);
                            tempPackageDirectory = tempPath;
                        }

                        if (Directory.Exists(rootDirectory.Value) &&
                            !Directory.EnumerateFileSystemEntries(rootDirectory.Value).Any())
                        {
                            Directory.Delete(rootDirectory.Value, false);
                        }
                    },
                    commit: () => {
                        if (tempPackageDirectory != null)
                        {
                            Directory.Delete(tempPackageDirectory, true);
                        }
                    },
                    rollback: () => {
                        if (tempPackageDirectory != null)
                        {
                            Directory.CreateDirectory(rootDirectory.Value);
                            Directory.Move(tempPackageDirectory, PackageDirectory.Value);
                        }
                    });
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
            {
                throw new ToolPackageException(
                    string.Format(
                        CommonLocalizableStrings.FailedToUninstallToolPackage,
                        PackageId,
                        ex.Message),
                    ex);
            }
        }

        private IReadOnlyList<ToolCommand> GetCommands()
        {
            const string AssetsFileName = "project.assets.json";
            const string ToolSettingsFileName = "DotnetToolSettings.xml";

            try
            {
                var commands = new List<ToolCommand>();
                var lockFile = new LockFileFormat().Read(PackageDirectory.WithFile(AssetsFileName).Value);

                var library = FindLibraryInLockFile(lockFile);
                var dotnetToolSettings = FindItemInTargetLibrary(library, ToolSettingsFileName);
                if (dotnetToolSettings == null)
                {
                    throw new ToolPackageException(
                        string.Format(
                            CommonLocalizableStrings.ToolPackageMissingSettingsFile,
                            PackageId));
                }

                var toolConfigurationPath =
                    PackageDirectory
                        .WithSubDirectories(
                            PackageId,
                            library.Version.ToNormalizedString())
                        .WithFile(dotnetToolSettings.Path);

                var configuration = ToolConfigurationDeserializer.Deserialize(toolConfigurationPath.Value);

                var entryPointFromLockFile = FindItemInTargetLibrary(library, configuration.ToolAssemblyEntryPoint);
                if (entryPointFromLockFile == null)
                {
                    throw new ToolPackageException(
                        string.Format(
                            CommonLocalizableStrings.ToolPackageMissingEntryPointFile,
                            PackageId,
                            configuration.ToolAssemblyEntryPoint));
                }

                commands.Add(new ToolCommand(
                    configuration.CommandName,
                    PackageDirectory
                        .WithSubDirectories(
                            PackageId,
                            library.Version.ToNormalizedString())
                        .WithFile(entryPointFromLockFile.Path)));

                return commands;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
            {
                throw new ToolPackageException(
                    string.Format(
                        CommonLocalizableStrings.FailedToRetrieveToolConfiguration,
                        PackageId,
                        ex.Message),
                    ex);
            }
        }

        private LockFileTargetLibrary FindLibraryInLockFile(LockFile lockFile)
        {
            return lockFile
                ?.Targets?.SingleOrDefault(t => t.RuntimeIdentifier != null)
                ?.Libraries?.SingleOrDefault(l => l.Name == PackageId);
        }

        private static LockFileItem FindItemInTargetLibrary(LockFileTargetLibrary library, string targetRelativeFilePath)
        {
            return library
                ?.ToolsAssemblies
                ?.SingleOrDefault(t => LockFileMatcher.MatchesFile(t, targetRelativeFilePath));
        }
    }
}
