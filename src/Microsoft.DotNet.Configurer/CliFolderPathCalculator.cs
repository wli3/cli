// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.PlatformAbstractions;
using NuGet.Common;

namespace Microsoft.DotNet.Configurer
{
    public class CliFolderPathCalculator
    {
        public string CliFallbackFolderPath => GetExecutableBaseDirectory("NuGetFallbackFolder");
        
        public string ExecutablePackagesPath => Path.Combine(DotnetUserProfileFolderPath, "tools") ;

        public string DotnetUserProfileFolderPath
        {
            get
            {
                string profileDir = Environment.GetEnvironmentVariable(
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "USERPROFILE" : "HOME");

                return Path.Combine(profileDir, ".dotnet");
            }
        }

        public string NuGetUserSettingsDirectory =>
            NuGetEnvironment.GetFolderPath(NuGetFolderPath.UserSettingsDirectory);

        private static string GetExecutableBaseDirectory(string nugetfallbackfolder)
        {
            return Environment.GetEnvironmentVariable("DOTNET_CLI_TEST_FALLBACKFOLDER") ??
                   Path.Combine(new DirectoryInfo(AppContext.BaseDirectory).Parent.FullName, nugetfallbackfolder);
        }
    }
}
