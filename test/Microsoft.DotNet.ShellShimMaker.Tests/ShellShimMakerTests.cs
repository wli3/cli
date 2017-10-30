// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.ShellShimMaker.Tests
{
    public class ShellShimMakerTests : TestBase
    {
        [WindowsOnlyFact]
        public void GivenAnExecutablePathItCanGenerateShimFile()
        {
            var outputDll = MakeHelloWorldExecutableDll();

            var muxer = new Muxer();
            var shellShimMaker = new ShellShimMaker(Path.GetDirectoryName(muxer.MuxerPath));
            var shellCommandName = nameof(ShellShimMakerTests) + Path.GetRandomFileName();

            shellShimMaker.CreateShim(
                outputDll,
                shellCommandName);
            var stdOut = ExecuteInShell(shellCommandName);

            stdOut.Should().Contain("Hello World");

            // Tear down
            shellShimMaker.Remove(shellCommandName);
        }

        private static string ExecuteInShell(string shellCommandName)
        {
            ExecuteAndCaptureOutput(new ProcessStartInfo
            {
                FileName = "CMD.exe",
                Arguments = $"/C {shellCommandName}",
                UseShellExecute = false
            }, out var stdOut);
            return stdOut ?? "";
        }

        private static FileInfo MakeHelloWorldExecutableDll()
        {
            const string target = "netcoreapp2.1";
            const string testAppName = "TestAppSimple";
            var testInstance = TestAssets.Get(testAppName)
                .CreateInstance(testAppName + "_" + target.Replace('.', '_'))
                .WithSourceFiles().WithRestoreFiles().WithBuildFiles();

            var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";

            var outputDll = testInstance.Root.GetDirectory("bin", configuration, target)
                .GetFile($"{testAppName}.dll");
            return outputDll;
        }

        private static void ExecuteAndCaptureOutput(ProcessStartInfo startInfo, out string stdOut)
        {
            var outStream = new StreamForwarder().Capture();

            startInfo.RedirectStandardOutput = true;

            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.Start();

            var taskOut = outStream.BeginRead(process.StandardOutput);
            process.WaitForExit();

            taskOut.Wait();
            stdOut = outStream.CapturedOutput;
        }

        [Fact(Skip = "Pending implementation")]
        public void GivenAnExecutablePathThatRequiresPermission()
        {
        }
    }
}
