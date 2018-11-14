// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Microsoft.DotNet.Cli.Utils.Tests
{
    public class MarkOfTheWebDetectorTests
    {
        [WindowsOnlyFact]
        public void DetectFileWithMarkOfTheWeb()
        {
            throw new NotImplementedException();
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
            private const uint GenericRead = 0x80000000;

            public static string WriteAlternateStream(string filePath, string altStreamName)
            {
                if (altStreamName == null)
                {
                    return null;
                }

                SafeFileHandle fileHandle = null;
                string returnstring = string.Empty;
                string altStream = filePath + ":" + altStreamName;

                fileHandle = CreateFile(altStream, GenericRead, 0, IntPtr.Zero, (uint)FileMode.Open, 0, IntPtr.Zero);
                if (!fileHandle.IsInvalid)
                {
                    using (StreamReader reader = new StreamWriter(new FileStream(fileHandle, FileAccess.Read)))
                    {
                        returnstring = reader.ReadToEnd();
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

                return returnstring;
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
