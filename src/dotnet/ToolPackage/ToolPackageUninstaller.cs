using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;

internal static class ToolPackageUninstaller
{
    public static void Uninstall(
        DirectoryPath packageDirectory,
        IToolPackageStoreQuery toolPackageStore,
        PackageId packageId)
    {
        var rootDirectory = packageDirectory.GetParentPath();
        string tempPackageDirectory = null;

        TransactionalAction.Run(
            action: () =>
            {
                try
                {
                    if (Directory.Exists(packageDirectory.Value))
                    {
                        // Use the staging directory for uninstall
                        // This prevents cross-device moves when temp is mounted to a different device
                        var tempPath = toolPackageStore.GetRandomStagingDirectory().Value;
                        FileAccessRetrier.RetryOnMoveAccessFailure(() => Directory.Move(packageDirectory.Value, tempPath));
                        tempPackageDirectory = tempPath;
                    }

                    if (Directory.Exists(rootDirectory.Value) &&
                        !Directory.EnumerateFileSystemEntries(rootDirectory.Value).Any())
                    {
                        Directory.Delete(rootDirectory.Value, false);
                    }
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                {
                    throw new ToolPackageException(
                        String.Format(
                            CommonLocalizableStrings.FailedToUninstallToolPackage,
                            packageId,
                            ex.Message),
                        ex);
                }
            },
            commit: () =>
            {
                if (tempPackageDirectory != null)
                {
                    Directory.Delete(tempPackageDirectory, true);
                }
            },
            rollback: () =>
            {
                if (tempPackageDirectory != null)
                {
                    Directory.CreateDirectory(rootDirectory.Value);
                    FileAccessRetrier.RetryOnMoveAccessFailure(() => Directory.Move(tempPackageDirectory, packageDirectory.Value));
                }
            });
    }
    
    internal class ToolPackageUninstallException : Exception
    {
        public ToolPackageUninstallException(string message) : base(message)
        {
        }
    }
}
