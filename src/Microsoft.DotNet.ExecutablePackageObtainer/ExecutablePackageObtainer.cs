// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ExecutablePackageObtainer
{
    public class ExecutablePackageObtainer
    {
        private readonly DirectoryPath _toolsPath;

        public ExecutablePackageObtainer(DirectoryPath toolsPath)
        {
            _toolsPath = toolsPath ?? throw new ArgumentNullException(nameof(toolsPath));
        }

        public FilePath ObtainAndReturnExecutablePath(string packageId, 
            string packageVersion,
            FilePath nugetconfig)
        {
            string nugetexePath = UnpackNugetexe();

            var processStartInfo = new ProcessStartInfo
            {
                FileName = nugetexePath,
                Arguments = $"install {packageId} -version {packageVersion} -OutputDirectory {_toolsPath.ToEscapedString()}",
                UseShellExecute = false
            };

            var exitcode = ExecuteAndCaptureOutput(processStartInfo, out var stdOut, out var stdErr);

            if (exitcode != 0)
            {
                throw new Exception("Nuget install failed" + "stdout: "+ stdOut + "stderr: "+stdErr);
            }
            return _toolsPath.WithCombineFollowing($"{packageId}.{packageVersion}", "lib", "netcoreapp2.0").CreateFilePath("consoleappababab.dll");
        }

        private static string UnpackNugetexe()
        {
            var thisAssembly = typeof(ExecutablePackageObtainer).GetTypeInfo().Assembly;

            string nugetexePath = Path.Combine(Path.GetTempPath(), "nuget.exe");

            using (Stream input = thisAssembly.GetManifestResourceStream("Microsoft.DotNet.ExecutablePackageObtainer.nuget.exe"))
            using (Stream output = File.Create(nugetexePath))
            {
                CopyStream(input, output);
            }

            return nugetexePath;
        }

        private static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8192];

            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
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
