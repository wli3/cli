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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Versioning;
using Xunit;
using Xunit.Sdk;
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
                    new[] {new ToolCommandName("t-rex")},
                    NuGetFramework.Parse("netcoreapp2.1")),
                new ToolManifestFindingResultIndividualTool(
                    new PackageId("dotnetsay"),
                    NuGetVersion.Parse("2.1.4"),
                    new[] {new ToolCommandName("dotnetsay")})
            };
        }

        [Fact]
        public void GivenManifestFileOnSameDirectoryItGetContent()
        {
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, _manifestFilename), _jsonContent);
            var toolManifest = new ToolManifest(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            var manifestResult = toolManifest.Find();

            manifestResult.ShouldBeEquivalentTo(_defaultExpectedResult);
        }

        [Fact]
        public void GivenManifestFileOnParentDirectoryItGetContent()
        {
            var subdirectoryOfTestRoot = Path.Combine(_testDirectoryRoot, "sub");
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, _manifestFilename), _jsonContent);
            var toolManifest = new ToolManifest(new DirectoryPath(subdirectoryOfTestRoot), _fileSystem);
            var manifestResult = toolManifest.Find();

            manifestResult.ShouldBeEquivalentTo(_defaultExpectedResult);
        }

        [Fact]
        // https://github.com/JamesNK/Newtonsoft.Json/issues/931#issuecomment-224104005
        // Due to a limitation of newtonsoft json
        public void GivenManifestWithDuplicatedPackageIdItReturnsTheLastValue()
        {
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, _manifestFilename),
                _jsonWithDuplicatedPackagedId);
            var toolManifest = new ToolManifest(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            var manifestResult = toolManifest.Find();

            manifestResult.Should()
                .Contain(
                    new ToolManifestFindingResultIndividualTool(
                        new PackageId("t-rex"),
                        NuGetVersion.Parse("2.1.4"),
                        new[] {new ToolCommandName("t-rex")}));
        }

        [Fact]
        public void WhenCalledWithFilePathItGetContent()
        {
            string customFileName = "customname.file";
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, customFileName), _jsonContent);
            var toolManifest = new ToolManifest(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            var manifestResult =
                toolManifest.Find(new FilePath(Path.Combine(_testDirectoryRoot, customFileName)));

            manifestResult.ShouldBeEquivalentTo(_defaultExpectedResult);
        }

        [Fact]
        public void WhenCalledWithNonExistsFilePathItThrows()
        {
            var toolManifest = new ToolManifest(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            Action a = () => toolManifest.Find(new FilePath(Path.Combine(_testDirectoryRoot, "non-exits")));
            a.ShouldThrow<ToolManifestException>().And.Message.Should().Contain("Cannot find any manifests file");
        }

        [Fact]
        public void GivenNoManifestFileItThrows()
        {
            var toolManifest = new ToolManifest(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            Action a = () => toolManifest.Find();
            a.ShouldThrow<ToolManifestException>().And.Message.Should().Contain("Cannot find any manifests file");
        }

        [Fact]
        public void GivenMissingFieldManifestFileItReturnError()
        {
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, _manifestFilename), _jsonWithMissingField);
            var toolManifest = new ToolManifest(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            Action a = () => toolManifest.Find();

            a.ShouldThrow<ToolManifestException>().And.Message.Should().Contain(
                "Invalid manifest file. In package t-rex: field version is missing, field commands is missing.");
        }

        [Fact]
        public void GivenInvalidFieldsManifestFileItReturnError()
        {
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, _manifestFilename), _jsonWithInvalidField);
            var toolManifest = new ToolManifest(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            Action a = () => toolManifest.Find();

            a.ShouldThrow<ToolManifestException>().And.Message.Should().Contain(
                "Invalid manifest file. In package t-rex: version 1.* is invalid, TargetFramework abc is unsupported.");
        }

        // Remove this test when the follow pending test is enabled and feature is implemented.
        [Fact]
        public void RequireRootAndVersionIs1()
        {
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot, _manifestFilename), _jsonWithNonRoot);
            var toolManifest = new ToolManifest(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            Action a = () => toolManifest.Find();

            a.ShouldThrow<ToolManifestException>().And.Message.Should().Contain(
                "Invalid manifest file. isRoot is false is not supported. version that is not 1 is not supported.");
        }

        [Fact(Skip = "pending implementation")]
        public void GivenConflictedManifestFileInDifferentFieldsItReturnMergedContent()
        {
        }

        [Fact(Skip = "pending implementation")]
        public void DifferentVersionOfManifestFileItShouldHaveWarnings()
        {
        }

        [Fact(Skip = "pending implementation")]
        public void DifferentVersionOfManifestFileItShouldNotThrow()
        {
        }

        private string _jsonContent =
            "{\"version\":1,\"isRoot\":true,\"tools\":{\"t-rex\":{\"version\":\"1.0.53\",\"commands\":[\"t-rex\"],\"targetFramework\":\"netcoreapp2.1\"},\"dotnetsay\":{\"version\":\"2.1.4\",\"commands\":[\"dotnetsay\"]}}}";

        private string _jsonWithDuplicatedPackagedId =
            "{\"version\":1,\"isRoot\":true,\"tools\":{\"t-rex\":{\"version\":\"1.0.53\",\"commands\":[\"t-rex\"],\"targetFramework\":\"netcoreapp2.1\"},\"t-rex\":{\"version\":\"2.1.4\",\"commands\":[\"t-rex\"]}}}";

        private string _jsonWithMissingField =
            "{\"version\":1,\"isRoot\":true,\"tools\":{\"t-rex\":{\"extra\":1}}}";

        private string _jsonWithInvalidField =
            "{\"version\":1,\"isRoot\":true,\"tools\":{\"t-rex\":{\"version\":\"1.*\",\"commands\":[\"t-rex\"],\"targetFramework\":\"abc\"}}}";

        private string _jsonWithNonRoot =
            "{\"version\":2,\"isRoot\":false,\"tools\":{\"t-rex\":{\"version\":\"1.0.53\",\"commands\":[\"t-rex\"]}}}";

        private readonly List<ToolManifestFindingResultIndividualTool> _defaultExpectedResult;
        private readonly string _testDirectoryRoot;
        private const string _manifestFilename = "localtool.manifest.json";
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
            return obj is ToolManifestFindingResultIndividualTool tool &&
                   Equals(tool);
        }

        public bool Equals(ToolManifestFindingResultIndividualTool other)
        {
            return PackageId.Equals(other.PackageId) &&
                   EqualityComparer<NuGetVersion>.Default.Equals(Version, other.Version) &&
                   CommandName.SequenceEqual(other.CommandName) &&
                   EqualityComparer<NuGetFramework>.Default.Equals(OptionalNuGetFramework,
                       other.OptionalNuGetFramework);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PackageId, Version, CommandName, OptionalNuGetFramework);
        }

        public static bool operator ==(ToolManifestFindingResultIndividualTool tool1,
            ToolManifestFindingResultIndividualTool tool2)
        {
            return tool1.Equals(tool2);
        }

        public static bool operator !=(ToolManifestFindingResultIndividualTool tool1,
            ToolManifestFindingResultIndividualTool tool2)
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

        public ToolManifest(DirectoryPath probStart, IFileSystem fileSystem = null)
        {
            _probStart = probStart;
            _fileSystem = fileSystem ?? new FileSystemWrapper();
        }

        public IReadOnlyCollection<ToolManifestFindingResultIndividualTool> Find(FilePath? filePath = null)
        {
            var fieldErrors = new Dictionary<string, List<string>>();
            var result = new List<ToolManifestFindingResultIndividualTool>();

            IEnumerable<FilePath> allPossibleManifests =
                filePath != null
                    ? new[] {filePath.Value}
                    : EnumerateDefaultAllPossibleManifests();

            foreach (FilePath possibleManifest in allPossibleManifests)
            {
                if (_fileSystem.File.Exists(possibleManifest.Value))
                {
                    var jsonResult = JsonConvert.DeserializeObject<LocalTools>(
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
                                packageLevelErrors.Add($"version {versionString} is invalid");
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
                                    $"TargetFramework {targetFrameworkString} is unsupported"); // TODO wul no check in loc
                            }
                        }

                        if (tools.Value.commands != null)
                        {
                            if (tools.Value.commands.Length == 0)
                            {
                                packageLevelErrors.Add($"field commands is missing"); // TODO wul no check in loc
                            }
                        }
                        else
                        {
                            packageLevelErrors.Add($"field commands is missing"); // TODO wul no check in loc
                        }

                        if (packageLevelErrors.Any())
                        {
                            var joined = string.Join(", ", packageLevelErrors);
                            errors.Add($"In package {packageId.ToString()}: {joined}.");
                        }
                        else
                        {
                            result.Add(new ToolManifestFindingResultIndividualTool(
                                packageId,
                                version,
                                ToolCommandName.Convert(tools.Value.commands),
                                targetFramework));
                        }
                    }

                    if (errors.Any())
                    {
                        throw new ToolManifestException("Invalid manifest file. " +
                                                        string.Join(" ", errors)); // TODO wul no check in loc
                    }

                    return result;
                }
            }

            throw new ToolManifestException(
                $"Cannot find any manifests file. Searched {string.Join("; ", allPossibleManifests.Select(f => f.Value))}"); // TODO wul no check in loc
        }

        private class LocalTools
        {
            [JsonProperty(Required = Required.Always)]
            public int version { get; set; }

            [JsonProperty(Required = Required.AllowNull)]
            public bool isRoot { get; set; }

            [JsonProperty(Required = Required.Always)]
            public Dictionary<string, localtool> tools { get; set; }
        }

        private class localtool
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