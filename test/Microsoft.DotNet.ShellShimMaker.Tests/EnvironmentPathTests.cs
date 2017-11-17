// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.ShellShimMaker.Tests
{
    public class EnvironmentPathTests
    {
        [Fact]
        public void GivenEnvironementAndReporterItCanPrintOutInstructionToAddPath()
        {
            var fakeReporter = new FakeReporter();
            var linuxEnvironementPath = new LinuxEnvironmentPath(
                @"executable\path",
                fakeReporter,
                new FakeEnvironmentProvider(
                    new Dictionary<string, string>
                    {
                        {"PATH", ""}
                    }));

            linuxEnvironementPath.PrintAddPathInstructionIfPathDoesNotExist();

            // similar to https://code.visualstudio.com/docs/setup/mac
            fakeReporter.Message.Should().Be($"Cannot find tools executable path in environement PATH. Please ensure executable\\path is added to your PATH.{Environment.NewLine}" +
                                             $"If you are using bash, you can add it by running following command:{Environment.NewLine}{Environment.NewLine}" +
                                             $"cat << EOF >> ~/.bash_profile{Environment.NewLine}" +
                                             $"# Add dotnet-sdk tools{Environment.NewLine}" +
                                             $"export PATH=\"$PATH:executable\\path\"{Environment.NewLine}" +
                                             $"EOF");
        }
        
        [Fact]
        public void GivenEnvironementAndReporterItPrintsNothingWhenEnvironementExists()
        {
            var fakeReporter = new FakeReporter();
            var linuxEnvironementPath = new LinuxEnvironmentPath(
                @"executable\path",
                fakeReporter,
                new FakeEnvironmentProvider(
                    new Dictionary<string, string>
                    {
                        {"PATH", @"executable\path"}
                    }));

            linuxEnvironementPath.PrintAddPathInstructionIfPathDoesNotExist();

            fakeReporter.Message.Should().BeEmpty();
        }

        private class FakeReporter : IReporter
        {
            public string Message { get; private set; } = "";

            public void WriteLine(string message)
            {
                Message = message;
            }

            public void WriteLine()
            {
                throw new NotImplementedException();
            }

            public void Write(string message)
            {
                throw new NotImplementedException();
            }
        }

        private class FakeEnvironmentProvider : IEnvironmentProvider
        {
            private readonly Dictionary<string, string> _environmentVariables;

            public FakeEnvironmentProvider(Dictionary<string, string> environmentVariables)
            {
                _environmentVariables =
                    environmentVariables ?? throw new ArgumentNullException(nameof(environmentVariables));
            }

            public IEnumerable<string> ExecutableExtensions { get; }

            public string GetCommandPath(string commandName, params string[] extensions)
            {
                throw new NotImplementedException();
            }

            public string GetCommandPathFromRootPath(string rootPath, string commandName, params string[] extensions)
            {
                throw new NotImplementedException();
            }

            public string GetCommandPathFromRootPath(string rootPath, string commandName,
                IEnumerable<string> extensions)
            {
                throw new NotImplementedException();
            }

            public bool GetEnvironmentVariableAsBool(string name, bool defaultValue)
            {
                throw new NotImplementedException();
            }

            public string GetEnvironmentVariable(string name)
            {
                return _environmentVariables.ContainsKey(name) ? _environmentVariables[name] : "";
            }
        }
    }
}
