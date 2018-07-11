using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ToolPackage
{
    internal class CommandSettingsCacheStore
    {
        private DirectoryPath _cacheLocation;

        internal CommandSettingsCacheStore(DirectoryPath cacheLocation)
        {
            _cacheLocation = cacheLocation;
        }

        internal bool Exists(FilePath currentPath)
        {
            return File.Exists(Path.Combine(_cacheLocation.Value, GetShortFileName(currentPath.Value)));
        }

        internal (IReadOnlyList<CommandSettings> commandSettingsList, FilePath currentPath, DateTimeOffset currentTime) Load(FilePath currentPath)
        {
            DirectoryToolCache directoryToolCache;
            using (Stream stream = File.Open(Path.Combine(_cacheLocation.Value, GetShortFileName(currentPath.Value)), FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                directoryToolCache = (DirectoryToolCache)binaryFormatter.Deserialize(stream);
            }

            var commandSettingsList = new List<CommandSettings>();
            if (directoryToolCache == null)
            {
                throw new InvalidOperationException("Cannot deserialize directoryToolCache"); // TODO wul loc
            }
            if (directoryToolCache.CommandSettingsList != null)
            {
                foreach (var s in directoryToolCache.CommandSettingsList)
                {
                    commandSettingsList.Add(new CommandSettings(s.Name, s.Runner, new FilePath(s.Executable)));
                }
            }

            return (commandSettingsList,
                new FilePath(directoryToolCache.DirectoryPath), // TODO wul this is not needed
                DateTimeOffset.Parse(directoryToolCache.CurrentTime));
        }

        private static string GetShortFileName(string directoryPath)
        {
            MD5 md5 = MD5.Create();

            byte[] inputBytes = Encoding.ASCII.GetBytes(directoryPath);

            byte[] hash = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();

            foreach (byte x in hash)
            {
                sb.AppendFormat("{0:x2}", x);
            }
            return sb.ToString();

        }

        internal void Save(IReadOnlyList<CommandSettings> commandSettingsList, FilePath currentPath, DateTimeOffset currentTime)
        {
            var directoryToolCache = new DirectoryToolCache
            {
                CommandSettingsList = new List<SerializableCommandSettings>(),
                CurrentTime = currentTime.ToString("u"),
                DirectoryPath = currentPath.Value
            };

            foreach (CommandSettings c in commandSettingsList)
            {
                directoryToolCache.CommandSettingsList.Add(new SerializableCommandSettings
                {
                    Name = c.Name,
                    Runner = c.Runner,
                    Executable = c.Executable.Value
                });
            }

            string shortFileName = GetShortFileName(directoryToolCache.DirectoryPath);

            string cachePath = Path.Combine(_cacheLocation.Value, shortFileName);
            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }
            using (Stream stream = File.Open(cachePath, FileMode.CreateNew))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, directoryToolCache);
            }
        }
    }

    [Serializable]
    internal class DirectoryToolCache
    {
        public List<SerializableCommandSettings> CommandSettingsList { get; set; }
        public string DirectoryPath { get; set; }
        public string CurrentTime { get; set; }
    }

    [Serializable]
    internal class SerializableCommandSettings
    {
        public string Name { get; set; }

        public string Runner { get; set; }

        public string Executable { get; set; }
    }
}
