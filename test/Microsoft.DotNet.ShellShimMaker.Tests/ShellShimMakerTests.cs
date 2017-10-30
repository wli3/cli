// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Test.Utilities;
using NuGet.Protocol.Core.Types;
using Xunit;

namespace Microsoft.DotNet.ShellShimMaker.Tests
{
    public class ShellShimMakerTests : TestBase
    {
        [Fact]
        public void GivenAnExecutablePathItCanGenerateShimFile()
        {
            const string target = "netcoreapp2.1";
            const string testAppName = "TestAppSimple";
            var testInstance = TestAssets.Get(testAppName)
                .CreateInstance(testAppName + "_" + target.Replace('.', '_'))
                .WithSourceFiles().WithRestoreFiles().WithBuildFiles();

            var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";

            var outputDll = testInstance.Root.GetDirectory("bin", configuration, target)
                .GetFile($"{testAppName}.dll");

            var muxer = new Muxer();
            var shellShimMaker = new ShellShimMaker(muxer.MuxerPath);
            var shellCommandName = Path.GetRandomFileName();
            shellShimMaker.CreateShim(executablePath: outputDll, shellCommandName: shellCommandName);

            var returnValue = ExecuteAndCaptureOutput(new ProcessStartInfo
            {
                FileName = shellCommandName,
                UseShellExecute = false
            }, out var stdOut, out var _);

            returnValue.Should().Be(0);
            stdOut.Should().Be("Hello World");
        }

        private static int ExecuteAndCaptureOutput(ProcessStartInfo startInfo, out string stdOut, out string stdErr) 
        {
            var outStream = new StreamForwarder().Capture();
            var errStream = new StreamForwarder().Capture();

            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            var process = new Process
            {
                StartInfo = startInfo
            };

            process.EnableRaisingEvents = true;

            process.Start();

            var taskOut = outStream.BeginRead(process.StandardOutput);
            var taskErr = errStream.BeginRead(process.StandardError);

            process.WaitForExit();

            taskOut.Wait();
            taskErr.Wait();

            stdOut = outStream.CapturedOutput;
            stdErr = errStream.CapturedOutput;

            return process.ExitCode;
        }
    }
}
