// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Tools.Common;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ShellShim
{
    internal class ZshDetector
    {
        private readonly IEnvironmentProvider _environmentProvider;
        private const string ZshFileName = "zsh";

        public ZshDetector(IEnvironmentProvider environmentProvider = null)
        {
            _environmentProvider = environmentProvider;
        }

        /// <summary>
        /// Return if the user use zsh as "The user's shell" instead of the current shell.
        /// By detecting $SHELL's value 
        /// </summary>
        public bool IsZshTheUsersShell()
        {
            string environmentVariable = _environmentProvider.GetEnvironmentVariable("SHELL");
            if (string.IsNullOrWhiteSpace(environmentVariable))
            {
                return false;
            }

            if (Path.GetFileName(environmentVariable).Equals(ZshFileName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
