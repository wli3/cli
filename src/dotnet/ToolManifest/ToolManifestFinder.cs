// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;
using Newtonsoft.Json;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace Microsoft.DotNet.ToolManifest
{
    internal class ToolManifestFinder : IToolManifestFinder, IToolManifestInspector
    {
        private readonly DirectoryPath _probeStart;
        private readonly IFileSystem _fileSystem;
        private readonly ToolManifestEditor _toolManifestEditor;
        private const string ManifestFilenameConvention = "dotnet-tools.json";

        public ToolManifestFinder(DirectoryPath probeStart, IFileSystem fileSystem = null)
        {
            _probeStart = probeStart;
            _fileSystem = fileSystem ?? new FileSystemWrapper();
            _toolManifestEditor = new ToolManifestEditor(_fileSystem);
        }

        public IReadOnlyCollection<ToolManifestPackage> Find(FilePath? filePath = null)
        {
            IEnumerable<(FilePath manifestfile, DirectoryPath _)> allPossibleManifests =
                filePath != null
                    ? new[] {(filePath.Value, filePath.Value.GetDirectoryPath())}
                    : EnumerateDefaultAllPossibleManifests();

            var findAnyManifest =
                TryFindToolManifestPackages(allPossibleManifests, out var toolManifestPackageAndSource);

            if (!findAnyManifest)
            {
                throw new ToolManifestCannotBeFoundException(
                    string.Format(LocalizableStrings.CannotFindAnyManifestsFileSearched,
                        string.Join(Environment.NewLine, allPossibleManifests.Select(f => f.manifestfile.Value))));
            }

            return toolManifestPackageAndSource.Select(t => t.toolManifestPackage).ToArray();
        }

        public IReadOnlyCollection<(ToolManifestPackage toolManifestPackage, FilePath SourceManifest)> Inspect(
            FilePath? filePath = null)
        {
            IEnumerable<(FilePath manifestfile, DirectoryPath _)> allPossibleManifests =
                filePath != null
                    ? new[] {(filePath.Value, filePath.Value.GetDirectoryPath())}
                    : EnumerateDefaultAllPossibleManifests();

            TryFindToolManifestPackages(allPossibleManifests, out var toolManifestPackageAndSource);

            return toolManifestPackageAndSource.ToArray();
        }

        private bool TryFindToolManifestPackages(
            IEnumerable<(FilePath manifestfile, DirectoryPath _)> allPossibleManifests, 
            out List<(ToolManifestPackage toolManifestPackage, FilePath SourceManifest)> toolManifestPackageAndSource)
        {
            bool findAnyManifest = false;
            toolManifestPackageAndSource 
                = new List<(ToolManifestPackage toolManifestPackage, FilePath SourceManifest)>();
            foreach ((FilePath possibleManifest, DirectoryPath correspondingDirectory) in allPossibleManifests)
            {
                if (!_fileSystem.File.Exists(possibleManifest.Value))
                {
                    continue;
                }

                findAnyManifest = true;

                (List<ToolManifestPackage> toolManifestPackageFromOneManifestFile, bool isRoot) =
                    _toolManifestEditor.Read(possibleManifest, correspondingDirectory);

                foreach (ToolManifestPackage toolManifestPackage in toolManifestPackageFromOneManifestFile)
                {
                    if (!toolManifestPackageAndSource.Any(addedToolManifestPackages =>
                        addedToolManifestPackages.toolManifestPackage.PackageId.Equals(toolManifestPackage.PackageId)))
                    {
                        toolManifestPackageAndSource.Add((toolManifestPackage, possibleManifest));
                    }
                }

                if (isRoot)
                {
                    return findAnyManifest;
                }
            }

            return findAnyManifest;
        }

        public bool TryFind(ToolCommandName toolCommandName, out ToolManifestPackage toolManifestPackage)
        {
            toolManifestPackage = default(ToolManifestPackage);
            foreach ((FilePath possibleManifest, DirectoryPath correspondingDirectory) in
                EnumerateDefaultAllPossibleManifests())
            {
                if (!_fileSystem.File.Exists(possibleManifest.Value))
                {
                    continue;
                }

                (List<ToolManifestPackage> manifestPackages, bool isRoot) =
                    _toolManifestEditor.Read(possibleManifest, correspondingDirectory);

                foreach (var package in manifestPackages)
                {
                    if (package.CommandNames.Contains(toolCommandName))
                    {
                        toolManifestPackage = package;
                        return true;
                    }
                }

                if (isRoot)
                {
                    return false;
                }
            }

            return false;
        }

        private IEnumerable<(FilePath manifestfile, DirectoryPath manifestFileFirstEffectDirectory)>
            EnumerateDefaultAllPossibleManifests()
        {
            DirectoryPath? currentSearchDirectory = _probeStart;
            while (currentSearchDirectory.HasValue)
            {
                var currentSearchDotConfigDirectory =
                    currentSearchDirectory.Value.WithSubDirectories(Constants.DotConfigDirectoryName);
                var tryManifest = currentSearchDirectory.Value.WithFile(ManifestFilenameConvention);
                yield return (currentSearchDotConfigDirectory.WithFile(ManifestFilenameConvention),
                    currentSearchDirectory.Value);
                yield return (tryManifest, currentSearchDirectory.Value);
                currentSearchDirectory = currentSearchDirectory.Value.GetParentPathNullable();
            }
        }

        public FilePath FindFirst()
        {
            foreach ((FilePath possibleManifest, DirectoryPath _) in EnumerateDefaultAllPossibleManifests())
            {
                if (_fileSystem.File.Exists(possibleManifest.Value))
                {
                    return possibleManifest;
                }
            }

            throw new ToolManifestCannotBeFoundException(
                string.Format(LocalizableStrings.CannotFindAnyManifestsFileSearched,
                    string.Join(Environment.NewLine,
                        EnumerateDefaultAllPossibleManifests().Select(f => f.manifestfile.Value))));
        }
    }
}
