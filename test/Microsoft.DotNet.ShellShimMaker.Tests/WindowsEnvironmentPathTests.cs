// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.DependencyModel.Tests;
using Xunit;

namespace Microsoft.DotNet.ShellShimMaker.Tests
{
    public class WindowsEnvironmentPathTests
    {
        [Fact]
        public void GivenEnvironementAndReporterItCanPrintOutInstructionToAddPath()
        {
            var fakeReporter = new FakeReporter();
            var windowsEnvironementPath = new WindowsEnvironmentPath(
                @"executable\path",
                fakeReporter,
                new FakeEnvironmentProvider(
                    new Dictionary<string, string>
                    {
                        {"PATH", ""}
                    }),
                FakeFile.Empty);

            windowsEnvironementPath.PrintAddPathInstructionIfPathDoesNotExist();

            // similar to https://code.visualstudio.com/docs/setup/mac
            fakeReporter.Message.Should().Be(
                $"Cannot find tools executable path in environement PATH. Please ensure executable\\path is added to your PATH.{Environment.NewLine}" +
                $"If you are using bash, you can add it by running following command:{Environment.NewLine}{Environment.NewLine}" +
                "setx PATH \"%PATH%;executable\\path\"");
        }

        [Fact]
        public void GivenEnvironementAndReporterItPrintsNothingWhenEnvironementExists()
        {
            var fakeReporter = new FakeReporter();
            var windowsEnvironementPath = new WindowsEnvironmentPath(
                @"executable\path",
                fakeReporter,
                new FakeEnvironmentProvider(
                    new Dictionary<string, string>
                    {
                        {"PATH", @"executable\path"}
                    }),
                FakeFile.Empty);

            windowsEnvironementPath.PrintAddPathInstructionIfPathDoesNotExist();

            fakeReporter.Message.Should().BeEmpty();
        }

        [Fact]
        public void GivenAddPackageExecutablePathToUserPathJustRunItPrintsInstructionToLogout()
        {
            // arrange
            var fakeReporter = new FakeReporter();
            var linuxEnvironementPath = new LinuxEnvironmentPath(
                @"executable\path",
                fakeReporter,
                new FakeEnvironmentProvider(
                    new Dictionary<string, string>
                    {
                        {"PATH", @""}
                    }),
                FakeFile.Empty);
            linuxEnvironementPath.AddPackageExecutablePathToUserPath();

            // act
            linuxEnvironementPath.PrintAddPathInstructionIfPathDoesNotExist();

            // asset
            fakeReporter.Message.Should().Be("You need logout to be able to run new installed command from shell");
        }
    }
}
