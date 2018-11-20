// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Microsoft.DotNet.Cli.Utils.Tests
{
    public class MarkOfTheWebDetectorTests: TestBase
    {
        [WindowsOnlyFact]
        public void DetectFileWithMarkOfTheWeb()
        {
            var testFile = Path.Combine(TempRoot.Root, Path.GetRandomFileName());

            AlternateStream.WriteAlternateStream(testFile, "Zone.Identifier", "[ZoneTransfer]\r\nZoneId=3\r\nReferrerUrl=C:\\Users\\test.zip\r\n");
            MarkOfTheWebDetector.HasMarkOfTheWeb(testFile).Should().BeTrue();
        }

        [Fact]
        public void NoFile()
        {
            throw new NotImplementedException();
        }

        [NonWindowsOnlyFact]
        public void WhenRunOnNonWindowsReturnFalse()
        {
            throw new NotImplementedException();
        }

        private class AlternateStream
        {
            private enum StreamInfoLevels
            {
                FindStreamInfoStandard = 0
            }

            private const int ErrorHandleEOF = 38;
            private const uint GenericWrite = 0x40000000;

            public static void WriteAlternateStream(string filePath, string altStreamName, string content)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("message", nameof(filePath));
                }

                if (string.IsNullOrWhiteSpace(altStreamName))
                {
                    throw new ArgumentException("message", nameof(altStreamName));
                }

                if (content == null)
                {
                    throw new ArgumentNullException(nameof(content));
                }

                SafeFileHandle fileHandle = null;
                string returnstring = string.Empty;
                string altStream = filePath + ":" + altStreamName;

                fileHandle = CreateFile(altStream, GenericWrite, 0, IntPtr.Zero, (uint)FileMode.CreateNew, 0, IntPtr.Zero);
                if (!fileHandle.IsInvalid)
                {
                    using (var streamWriter = new StreamWriter(new FileStream(fileHandle, FileAccess.Write)))
                    {
                        streamWriter.WriteLine(content);
                        streamWriter.Flush();
                    }
                }
                else
                {
                    Exception ex = new Win32Exception(Marshal.GetLastWin32Error());
                    if (!ex.Message.Contains("cannot find the file"))
                    {
                        throw ex;
                    }
                }
            }

            [DllImport("kernel32", SetLastError = true)]
            private static extern SafeFileHandle CreateFile(
                string filename,
                uint desiredAccess,
                uint shareMode,
                IntPtr attributes,
                uint creationDisposition,
                uint flagsAndAttributes,
                IntPtr templateFile);
        }
    }
}
