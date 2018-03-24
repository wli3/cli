// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools;
using Microsoft.Extensions.EnvironmentAbstractions;
using Newtonsoft.Json;

namespace Microsoft.DotNet.ShellShim
{
    internal class ShellShimRepository : IShellShimRepository
    {
        private const string LauncherExeResourceName = "Microsoft.DotNet.Tools.Launcher.Executable";
        private const string LauncherConfigResourceName = "Microsoft.DotNet.Tools.Launcher.Config";

        private readonly DirectoryPath _shimsDirectory;

        public ShellShimRepository(DirectoryPath shimsDirectory)
        {
            _shimsDirectory = shimsDirectory;
        }

        public void CreateShim(FilePath targetExecutablePath, string commandName)
        {
            if (string.IsNullOrEmpty(targetExecutablePath.Value))
            {
                throw new ShellShimException(CommonLocalizableStrings.CannotCreateShimForEmptyExecutablePath);
            }
            if (string.IsNullOrEmpty(commandName))
            {
                throw new ShellShimException(CommonLocalizableStrings.CannotCreateShimForEmptyCommand);
            }

            if (ShimExists(commandName))
            {
                throw new ShellShimException(
                    string.Format(
                        CommonLocalizableStrings.ShellShimConflict,
                        commandName));
            }

            TransactionalAction.Run(
                action: () => {
                    try
                    {
                        if (!Directory.Exists(_shimsDirectory.Value))
                        {
                            Directory.CreateDirectory(_shimsDirectory.Value);
                        }

                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            CreateConfigFile(
                                outputPath: GetWindowsShimPath(commandName),
                                entryPoint: targetExecutablePath,
                                runner: "dotnet");

                            //using (var shim = File.Create(GetWindowsShimPath(commandName).Value))
                            //using (var resource = typeof(ShellShimRepository).Assembly.GetManifestResourceStream(LauncherExeResourceName))
                            //{
                            //    resource.CopyTo(shim);
                            //}
                        }
                        else
                        {
                            var script = new StringBuilder();
                            script.AppendLine("#!/bin/sh");
                            script.AppendLine($"dotnet {targetExecutablePath.ToQuotedString()} \"$@\"");

                            var shimPath = GetPosixShimPath(commandName);
                            File.WriteAllText(shimPath.Value, script.ToString());

                            SetUserExecutionPermission(shimPath);
                        }
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                    {
                        throw new ShellShimException(
                            string.Format(
                                CommonLocalizableStrings.FailedToCreateShellShim,
                                commandName,
                                ex.Message
                            ),
                            ex);
                    }
                },
                rollback: () => {
                    foreach (var file in GetShimFiles(commandName).Where(f => File.Exists(f.Value)))
                    {
                        File.Delete(file.Value);
                    }
                });
        }

        public void RemoveShim(string commandName)
        {
            var files = new Dictionary<string, string>();
            TransactionalAction.Run(
                action: () => {
                    try
                    {
                        foreach (var file in GetShimFiles(commandName).Where(f => File.Exists(f.Value)))
                        {
                            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                            File.Move(file.Value, tempPath);
                            files[file.Value] = tempPath;
                        }
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                    {
                        throw new ShellShimException(
                            string.Format(
                                CommonLocalizableStrings.FailedToRemoveShellShim,
                                commandName,
                                ex.Message
                            ),
                            ex);
                    }
                },
                commit: () => {
                    foreach (var value in files.Values)
                    {
                        File.Delete(value);
                    }
                },
                rollback: () => {
                    foreach (var kvp in files)
                    {
                        File.Move(kvp.Value, kvp.Key);
                    }
                });
        }

        internal void CreateConfigFile(FilePath outputPath, FilePath entryPoint, string runner)
        {

            //var appSettings = config.Descendants("appSettings").First();
            //appSettings.Add(new XElement("add", new XAttribute("key", "entryPoint"), new XAttribute("value", entryPoint.Value)));
            //appSettings.Add(new XElement("add", new XAttribute("key", "runner"), new XAttribute("value", runner ?? string.Empty)));

            var embedAppNameInHost = new EmbedAppNameInHost
            {
                AppHostSourcePath = @"C:\work\cli\.nuget\packages\runtime.win-x64.microsoft.netcore.dotnetapphost\2.1.0-preview2-26313-01\runtimes\win-x64\native\apphost.exe",
                AppHostDestinationDirectoryPath = outputPath.GetDirectoryPath().Value,
                AppBinaryName = Path.GetFileName(entryPoint.Value)
            };

            embedAppNameInHost.Run();

            var configjson = new RootObject { startupOptions = new StartupOptions { appRoot = entryPoint.GetDirectoryPath().Value } };

            var config = JsonConvert.SerializeObject(configjson);

            File.WriteAllText(outputPath.GetDirectoryPath().WithFile(Path.GetFileNameWithoutExtension(outputPath.Value) + ".startupconfig.json").Value, config);
            File.Move(embedAppNameInHost.ModifiedAppHostPath, outputPath.Value);
            // TODO rename the file

            //  config.Save(outputPath.Value);
        }

        public class StartupOptions
        {
            public string appRoot { get; set; }
        }

        public class RootObject
        {
            public StartupOptions startupOptions { get; set; }
        }

        private bool ShimExists(string commandName)
        {
            return GetShimFiles(commandName).Any(p => File.Exists(p.Value));
        }

        private IEnumerable<FilePath> GetShimFiles(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                yield break;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return GetWindowsShimPath(commandName);
                yield return GetWindowsConfigPath(commandName);
            }
            else
            {
                yield return GetPosixShimPath(commandName);
            }
        }

        private FilePath GetPosixShimPath(string commandName)
        {
            return _shimsDirectory.WithFile(commandName);
        }

        private FilePath GetWindowsShimPath(string commandName)
        {
            return new FilePath(_shimsDirectory.WithFile(commandName).Value + ".exe");
        }

        private FilePath GetWindowsConfigPath(string commandName)
        {
            return new FilePath(GetWindowsShimPath(commandName).Value + ".config");
        }

        private static void SetUserExecutionPermission(FilePath path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            CommandResult result = new CommandFactory()
                .Create("chmod", new[] { "u+x", path.Value })
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute();

            if (result.ExitCode != 0)
            {
                throw new ShellShimException(
                    string.Format(CommonLocalizableStrings.FailedSettingShimPermissions, result.StdErr));
            }
        }
    }
}
