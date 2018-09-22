// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Common;

namespace Microsoft.DotNet.Configurer
{
    public static class CliFolderPathCalculator
    {
        public const string DotnetHomeVariableName = "DOTNET_CLI_HOME";
        internal const string DotnetProfileDirectoryName = ".dotnet";
        internal const string ToolsShimFolderName = "tools";

        public static string CliFallbackFolderPath =>
            Environment.GetEnvironmentVariable("DOTNET_CLI_TEST_FALLBACKFOLDER") ??
                Path.Combine(new DirectoryInfo(AppContext.BaseDirectory).Parent.FullName, "NuGetFallbackFolder");

        public static string ToolsShimPath => Path.Combine(DotnetUserProfileFolderPath, ToolsShimFolderName);

        public static string ToolsPackagePath => ToolPackageFolderPathCalculator.GetToolPackageFolderPath(ToolsShimPath);

        public static BashPathUnderHomeDirectory ToolsShimPathInUnix =>
            new BashPathUnderHomeDirectory(
                DotnetHomePath,
                Path.Combine(DotnetProfileDirectoryName, ToolsShimFolderName));

        public static string DotnetUserProfileFolderPath =>
            Path.Combine(DotnetHomePath, DotnetProfileDirectoryName);

        public static string PlatformHomeVariableName =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "USERPROFILE" : "HOME";

        public static string DotnetHomePath
        {
            get
            {
                var home = Environment.GetEnvironmentVariable(DotnetHomeVariableName);
                if (string.IsNullOrEmpty(home))
                {
                    home = Environment.GetEnvironmentVariable(PlatformHomeVariableName);
                    if (string.IsNullOrEmpty(home))
                    {
                        throw new ConfigurationException(
                            string.Format(
                                LocalizableStrings.FailedToDetermineUserHomeDirectory,
                                DotnetHomeVariableName))
                            .DisplayAsError();
                    }
                }

                return home;
            }
        }

        public static string NuGetUserSettingsDirectory =>
            NuGetEnvironment.GetFolderPath(NuGetFolderPath.UserSettingsDirectory);
    }
}
