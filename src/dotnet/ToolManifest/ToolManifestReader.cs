// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;
using Newtonsoft.Json;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace Microsoft.DotNet.ToolManifest
{
    internal class ToolManifestReader
    {
        private readonly DirectoryPath _probStart;
        private readonly IFileSystem _fileSystem;
        private const string _manifestFilenameConvention = "localtool.manifest.json";

        public ToolManifestReader(DirectoryPath probStart, IFileSystem fileSystem = null)
        {
            _probStart = probStart;
            _fileSystem = fileSystem ?? new FileSystemWrapper();
        }

        public IReadOnlyCollection<ToolManifestFindingResultSinglePackage> Find(FilePath? filePath = null)
        {
            var result = new List<ToolManifestFindingResultSinglePackage>();

            IEnumerable<FilePath> allPossibleManifests =
                filePath != null
                    ? new[] {filePath.Value}
                    : EnumerateDefaultAllPossibleManifests();

            foreach (FilePath possibleManifest in allPossibleManifests)
            {
                if (_fileSystem.File.Exists(possibleManifest.Value))
                {
                    var jsonResult = JsonConvert.DeserializeObject<SerializableLocalToolsManifest>(
                        _fileSystem.File.ReadAllText(possibleManifest.Value), new JsonSerializerSettings
                        {
                            MissingMemberHandling = MissingMemberHandling.Ignore
                        });

                    var errors = new List<string>();

                    if (!jsonResult.isRoot)
                    {
                        errors.Add("isRoot is false is not supported."); // TODO wul no check in loc
                    }

                    if (jsonResult.version != 1)
                    {
                        errors.Add("version that is not 1 is not supported."); // TODO wul no check in loc
                    }

                    foreach (var tools in jsonResult.tools)
                    {
                        var packageLevelErrors = new List<string>();
                        var packageIdString = tools.Key;

                        var packageId = new PackageId(packageIdString);

                        string versionString = tools.Value.version;

                        NuGetVersion version = null;
                        if (versionString is null)
                        {
                            packageLevelErrors.Add("field version is missing"); // TODO wul no check in loc
                        }
                        else
                        {
                            var versionParseResult = NuGetVersion.TryParse(
                                versionString, out version);

                            if (!versionParseResult)
                            {
                                packageLevelErrors.Add(string.Format("version {0} is invalid", versionString));
                            }
                        }

                        NuGetFramework targetFramework = null;
                        var targetFrameworkString = tools.Value.targetFramework;

                        if (!(targetFrameworkString is null))
                        {
                            targetFramework = NuGetFramework.Parse(
                                targetFrameworkString);

                            if (targetFramework.IsUnsupported)
                            {
                                packageLevelErrors.Add(
                                    string.Format("TargetFramework {0} is unsupported", targetFrameworkString)); // TODO wul no check in loc
                            }
                        }

                        if (tools.Value.commands != null)
                        {
                            if (tools.Value.commands.Length == 0)
                            {
                                packageLevelErrors.Add("field commands is missing"); // TODO wul no check in loc
                            }
                        }
                        else
                        {
                            packageLevelErrors.Add("field commands is missing"); // TODO wul no check in loc
                        }

                        if (packageLevelErrors.Any())
                        {
                            var joined = string.Join(", ", packageLevelErrors);
                            errors.Add(string.Format("In package {0}: {1}.", packageId.ToString(), joined));
                        }
                        else
                        {
                            result.Add(new ToolManifestFindingResultSinglePackage(
                                packageId,
                                version,
                                ToolCommandName.Convert(tools.Value.commands),
                                targetFramework));
                        }
                    }

                    if (errors.Any())
                    {
                        throw new ToolManifestException(string.Format("Invalid manifest file. {0}",
                            string.Join(" ", errors))); // TODO wul no check in loc
                    }

                    return result;
                }
            }

            throw new ToolManifestException(
                string.Format("Cannot find any manifests file. Searched {0}",
                    string.Join("; ", allPossibleManifests.Select(f => f.Value)))); // TODO wul no check in loc
        }

        private class SerializableLocalToolsManifest
        {
            [JsonProperty(Required = Required.Always)]
            public int version { get; set; }

            [JsonProperty(Required = Required.Always)]
            public bool isRoot { get; set; }

            [JsonProperty(Required = Required.Always)]
            public Dictionary<string, SerializableLocalToolSinglePackage> tools { get; set; }
        }

        private class SerializableLocalToolSinglePackage
        {
            public string version { get; set; }
            public string[] commands { get; set; }
            public string targetFramework { get; set; }
        }

        private IEnumerable<FilePath> EnumerateDefaultAllPossibleManifests()
        {
            DirectoryPath? currentSearchDirectory = _probStart;
            while (currentSearchDirectory != null)
            {
                var tryManifest = currentSearchDirectory.Value.WithFile(_manifestFilenameConvention);

                yield return tryManifest;

                currentSearchDirectory = currentSearchDirectory.Value.GetParentPathNullable();
            }
        }
    }
}
