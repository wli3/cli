﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.Tools;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ShellShim
{
    internal class OsxZshEnvironmentPathInstruction : IEnvironmentPathInstruction
    {
        private const string PathName = "PATH";
        private readonly BashPathUnderHomeDirectory _packageExecutablePath;
        private readonly IFile _fileSystem;
        private readonly IEnvironmentProvider _environmentProvider;
        private readonly IReporter _reporter;


        public OsxZshEnvironmentPathInstruction(
            BashPathUnderHomeDirectory executablePath,
            IReporter reporter,
            IEnvironmentProvider environmentProvider,
            IFile fileSystem
        )
        {
            _packageExecutablePath = executablePath;
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _environmentProvider
                = environmentProvider ?? throw new ArgumentNullException(nameof(environmentProvider));
            _reporter
                = reporter ?? throw new ArgumentNullException(nameof(reporter));
        }

        private bool PackageExecutablePathExists()
        {
            var value = _environmentProvider.GetEnvironmentVariable(PathName);
            if (value == null)
            {
                return false;
            }

            return value
                .Split(':')
                .Any(p => p == _packageExecutablePath.Path || p == _packageExecutablePath.PathWithTilde);
        }

        public void PrintAddPathInstructionIfPathDoesNotExist()
        {
            if (!PackageExecutablePathExists())
            {
                    // similar to https://code.visualstudio.com/docs/setup/mac
                    _reporter.WriteLine(
                        string.Format(
                            CommonLocalizableStrings.EnvironmentPathOSXZshManualInstructions,
                            _packageExecutablePath.Path));
            }
        }
    }
}
