// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.ToolPackage;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.DotNet.Tools.Tests.ComponentMocks;
using Microsoft.DotNet.Tools.Tool.Restore;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using Newtonsoft.Json.Linq;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Versioning;
using Xunit;
using LocalizableStrings = Microsoft.DotNet.Tools.Tool.Restore.LocalizableStrings;
using Parser = Microsoft.DotNet.Cli.Parser;

namespace Microsoft.DotNet.Tests.Commands
{
    public class ToolManifestTests
    {
        private readonly IFileSystem _fileSystem;


        public ToolManifestTests()
        {
            _fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();
            _testDirectoryRoot = _fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;

            _defaultExpectedResult = new List<ToolManifestFindingResultIndividualTool>
            {
                new ToolManifestFindingResultIndividualTool(
                    new PackageId("t-rex"),
                    NuGetVersion.Parse("1.0.53"),
                    new [] {new ToolCommandName("t-rex") },
                    NuGetFramework.Parse("netcoreapp2.1")),
                new ToolManifestFindingResultIndividualTool(
                    new PackageId("dotnetsay"),
                    NuGetVersion.Parse("2.1.4"),
                    new [] {new ToolCommandName("dotnetsay") })
            };
        }

        [Fact]
        public void GivenManifestFileOnSameDirectoryItGetContent()
        {
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, _manifestFilename), _jsonContent);
            var toolManifest = new ToolManifest(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            ToolManifestFindingResult manifestResult = toolManifest.Find();
            manifestResult.Errors.Should().BeEmpty();

            manifestResult.Result.ShouldBeEquivalentTo(_defaultExpectedResult);
        }

        [Fact]
        public void GivenManifestFileOnParentDirectoryItGetContent()
        {
            var subdirectoryOfTestRoot = Path.Combine(_testDirectoryRoot, "sub");
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, _manifestFilename), _jsonContent);
            var toolManifest = new ToolManifest(new DirectoryPath(subdirectoryOfTestRoot), _fileSystem);
            ToolManifestFindingResult manifestResult = toolManifest.Find();
            manifestResult.Errors.Should().BeEmpty();

            manifestResult.Result.ShouldBeEquivalentTo(_defaultExpectedResult);
        }

        [Fact]
        // https://github.com/JamesNK/Newtonsoft.Json/issues/931#issuecomment-224104005
        // Due to a limitation of newtonsoft json
        public void GivenManifestWithDuplicatedPackageIdItReturnsTheLastValue()
        {
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, _manifestFilename), _jsonWithDuplicatedPackagedId);
            var toolManifest = new ToolManifest(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            ToolManifestFindingResult manifestResult = toolManifest.Find();

            manifestResult.Result.Should()
                .Contain(
                new ToolManifestFindingResultIndividualTool(
                    new PackageId("t-rex"),
                    NuGetVersion.Parse("2.1.4"),
                    new[] { new ToolCommandName("t-rex") }));
        }

        [Fact]
        public void WhenCalledWithFilePathItGetContent()
        {
            string customFileName = "customname.file";
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, customFileName), _jsonContent);
            var toolManifest = new ToolManifest(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            ToolManifestFindingResult manifestResult = toolManifest.Find(new FilePath(Path.Combine(_testDirectoryRoot, customFileName)));
            manifestResult.Errors.Should().BeEmpty();

            manifestResult.Result.ShouldBeEquivalentTo(_defaultExpectedResult);
        }

        [Fact(Skip = "pending")]
        public void WhenCalledWithNonExistsFilePathItReturnError()
        {

        }

        [Fact(Skip = "pending")]
        public void GivenMissingFieldManifestFileItReturnError()
        {

        }

        [Fact(Skip = "pending")]
        public void GivenConflictedManifestFileInDifferentFieldsItReturnMergedContent()
        {

        }

        [Fact(Skip = "pending")]
        public void DifferentVersionOfManifestFileItShouldHaveWarnings()
        {

        }

        private string _jsonContent =
            "{\"version\":\"1\",\"isRoot\":true,\"tools\":{\"t-rex\":{\"version\":\"1.0.53\",\"commands\":[\"t-rex\"],\"targetFramework\":\"netcoreapp2.1\"},\"dotnetsay\":{\"version\":\"2.1.4\",\"commands\":[\"dotnetsay\"]}}}";

        private string _jsonWithDuplicatedPackagedId=
            "{\"version\":\"1\",\"isRoot\":true,\"tools\":{\"t-rex\":{\"version\":\"1.0.53\",\"commands\":[\"t-rex\"],\"targetFramework\":\"netcoreapp2.1\"},\"t-rex\":{\"version\":\"2.1.4\",\"commands\":[\"t-rex\"]}}}";
        private List<ToolManifestFindingResultIndividualTool> _defaultExpectedResult;
        private readonly string _testDirectoryRoot;
        private readonly string _manifestFilename = "localtool.manifest.json";
    }

    internal struct ToolManifestFindingResult
    {
        private ToolManifestFindingResult(IReadOnlyCollection<string> errors,
            IReadOnlyCollection<ToolManifestFindingResultIndividualTool> result)
        {
            Errors = errors;
            Result = result;
        }

        public static ToolManifestFindingResult ToolManifestFindingResultWithResult(
            IReadOnlyCollection<ToolManifestFindingResultIndividualTool> result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return new ToolManifestFindingResult(Array.Empty<string>(), result);
        }

        public static ToolManifestFindingResult ToolManifestFindingResultWithError(IReadOnlyCollection<string> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            return new ToolManifestFindingResult(errors, Array.Empty<ToolManifestFindingResultIndividualTool>());
        }

        public IReadOnlyCollection<string> Errors { get; set; }
        public IReadOnlyCollection<ToolManifestFindingResultIndividualTool> Result { get; set; }
    }

    internal struct ToolManifestFindingResultIndividualTool : IEquatable<ToolManifestFindingResultIndividualTool>
    {
        public PackageId PackageId { get; }
        public NuGetVersion Version { get; }
        public ToolCommandName[] CommandName { get; }
        public NuGetFramework OptionalNuGetFramework { get; }

        public ToolManifestFindingResultIndividualTool(
            PackageId packagePackageId,
            NuGetVersion version,
            ToolCommandName[] toolCommandName,
            NuGetFramework optionalNuGetFramework = null)
        {
            PackageId = packagePackageId;
            Version = version;
            CommandName = toolCommandName;
            OptionalNuGetFramework = optionalNuGetFramework;
        }

        public override bool Equals(object obj)
        {
            return obj is ToolManifestFindingResultIndividualTool && Equals((ToolManifestFindingResultIndividualTool)obj);
        }

        public bool Equals(ToolManifestFindingResultIndividualTool other)
        {
            return PackageId.Equals(other.PackageId) &&
                   EqualityComparer<NuGetVersion>.Default.Equals(Version, other.Version) &&
                   Enumerable.SequenceEqual(CommandName, other.CommandName) &&
                   EqualityComparer<NuGetFramework>.Default.Equals(OptionalNuGetFramework, other.OptionalNuGetFramework);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PackageId, Version, CommandName, OptionalNuGetFramework);
        }

        public static bool operator ==(ToolManifestFindingResultIndividualTool tool1, ToolManifestFindingResultIndividualTool tool2)
        {
            return tool1.Equals(tool2);
        }

        public static bool operator !=(ToolManifestFindingResultIndividualTool tool1, ToolManifestFindingResultIndividualTool tool2)
        {
            return !(tool1 == tool2);
        }
    }

    internal class ToolManifestException : Exception
    {
        public ToolManifestException()
        {
        }

        public ToolManifestException(string message) : base(message)
        {
        }

        public ToolManifestException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    internal class ToolManifest
    {
        private readonly DirectoryPath _probStart;
        private readonly IFileSystem _fileSystem;
        private readonly string _manifestFilenameConvention = "localtool.manifest.json";
        private const string ToolsJsonNodeName = "tools";

        public ToolManifest(DirectoryPath probStart, IFileSystem fileSystem = null)
        {
            _probStart = probStart;
            _fileSystem = fileSystem ?? new FileSystemWrapper();
        }

        public ToolManifestFindingResult Find(FilePath? filePath = null)
        {
            var errors = new List<string>();
            var result = new List<ToolManifestFindingResultIndividualTool>();

            IEnumerable<FilePath> allPossibleManifests;
            if (filePath != null)
            {
                allPossibleManifests = new FilePath[] { filePath.Value };
            }
            else
            {
                allPossibleManifests = EnumerateDefaultAllPossibleManifests();
            }

            foreach (FilePath possibleManifest in allPossibleManifests)
            {
                if (_fileSystem.File.Exists(possibleManifest.Value))
                {
                    JObject manifest = JObject.Parse(_fileSystem.File.ReadAllText(possibleManifest.Value));
                    foreach (var tools in manifest[ToolsJsonNodeName])
                    {
                        var packageIdString = tools.ToObject<JProperty>().Name;
                        NuGet.Packaging.PackageIdValidator.IsValidPackageId(packageIdString);

                        errors.Add($"Package Id {packageIdString} is not valid");

                        var packageId = new PackageId(packageIdString);

                        // TODO WUL NULL CHECK for all field
                        var versionParseResult = NuGetVersion.TryParse(manifest[ToolsJsonNodeName][packageIdString].Value<string>("version"), out var version);

                        NuGetFramework targetframework = null;
                        var targetFrameworkString = manifest[ToolsJsonNodeName][packageIdString].Value<string>("targetFramework");
                        if (!(targetFrameworkString is null))
                        {
                            targetframework = NuGetFramework.Parse(
                                targetFrameworkString);
                        }

                        var toolCommandNameStringArray =
                            manifest[ToolsJsonNodeName][packageIdString]["commands"].ToObject<string[]>();

                        result.Add(new ToolManifestFindingResultIndividualTool(
                            packageId,
                            version,
                            ToolCommandName.Convert(toolCommandNameStringArray),
                            targetframework));
                    }

                    return ToolManifestFindingResult.ToolManifestFindingResultWithResult(result);
                }
            }

            throw new ToolManifestException($"Cannot find any manifests file. Searched {string.Join("; ", allPossibleManifests.Select(f => f.Value))}");
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
