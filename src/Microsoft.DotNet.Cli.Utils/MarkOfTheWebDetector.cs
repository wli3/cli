using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.PlatformAbstractions;
using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;

namespace Microsoft.DotNet.Cli.Utils
{
    internal class MarkOfTheWebDetector : IMarkOfTheWebDetector
    {
        private const string ZoneIdentifierStreamName = "Zone.Identifier";
        private const string ZoneIdIs3String = "ZoneId=3";

        public bool HasMarkOfTheWeb(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{filePath} cannot be found");
            }

            if (RuntimeEnvironment.OperatingSystemPlatform != Platform.Windows)
            {
                return false;
            }

            if (ZoneIdIs3(filePath))
            {
                return true;
            }

            return false;
        }

        private bool ZoneIdIs3(string filePath)
        {
            return AlternateStream.ReadAlternateStream(filePath, ZoneIdentifierStreamName)
                            .Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
                            .Where(l => l.Equals(ZoneIdIs3String, StringComparison.Ordinal))
                            .Any();
        }

        private class AlternateStream
        {
            private enum StreamInfoLevels
            {
                FindStreamInfoStandard = 0
            }

            private const int ErrorHandleEOF = 38;
            private const uint GenericRead = 0x80000000;

            public static string ReadAlternateStream(string filePath, string altStreamName)
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
                    using (StreamReader reader = new StreamReader(new FileStream(fileHandle, FileAccess.Read)))
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
