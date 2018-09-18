// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
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
        }

        [Fact(Skip ="")]
        public void GivenManifestFileOnSameDirectoryItGetContent()
        {
            _fileSystem.File.WriteAllText(Path.Combine(_testDirectoryRoot,_manifestFilename), jsonContent);
            var toolManifest = new ToolManifest(new DirectoryPath(_testDirectoryRoot), _fileSystem);
            ToolManifestFindingResult manifestResult = toolManifest.Find();
            manifestResult.Errors.Should().BeEmpty();

            var expectedResult = new List<ToolManifestFindingResultIndividualTool>
            {
                new ToolManifestFindingResultIndividualTool(
                    new PackageId("t-rex"),
                    NuGetVersion.Parse("1.0.53"),
                    new ToolCommandName("t-rex"),
                    NuGetFramework.Parse("netcoreapp2.1")),
                new ToolManifestFindingResultIndividualTool(
                    new PackageId("dotnetsay"),
                    NuGetVersion.Parse("2.1.4"),
                    new ToolCommandName("dotnetsay"))
            };

            manifestResult.Result.ShouldBeEquivalentTo(expectedResult);
        }
        
        [Fact(Skip ="")]
        public void GivenManifestFileOnParentDirectoryItGetContent()
        {
            
        }
        
        [Fact(Skip ="")]
        public void GivenManifestWithDuplicatedPackageIdItReturnError()
        {
            
        }
        
        [Fact(Skip ="")]
        public void WhenCalledWithFilePathItGetContent()
        {
            
        }
        
        [Fact(Skip ="")]
        public void WhenCalledWithNonExistsFilePathItReturnError()
        {
            
        }
        
        [Fact(Skip ="")]
        public void GivenMissingFieldManifestFileItReturnError()
        {
            
        }
        
        [Fact(Skip ="")]
        public void GivenConflictedManifestFileInDifferentFieldsItReturnMergedContent()
        {
            
        }
        
        [Fact(Skip ="")]
        public void DifferentVersionOfManifestFileItShouldHaveWarnings()
        {
            
        }

        private string jsonContent =
            "{\"version\":\"1\",\"isRoot\":true,\"tools\":{\"t-rex\":{\"version\":\"1.0.53\",\"commands\":[\"t-rex\"],\"targetFramework\":\"netcoreapp2.1\"},\"dotnetsay\":{\"version\":\"2.1.4\",\"commands\":[\"dotnetsay\"]}}}";
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

            return new ToolManifestFindingResult(null, result);
        }

        public static ToolManifestFindingResult ToolManifestFindingResultWithError(IReadOnlyCollection<string> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            return new ToolManifestFindingResult(errors, null);
        }

        public IReadOnlyCollection<string> Errors { get; set; }
        public IReadOnlyCollection<ToolManifestFindingResultIndividualTool> Result { get; set; }
    }

    internal struct ToolManifestFindingResultIndividualTool
    {
        public PackageId PackageId { get; }
        public NuGetVersion Version { get; }
        public ToolCommandName CommandName { get; }
        public NuGetFramework OptionalNuGetFramework { get; }

        public ToolManifestFindingResultIndividualTool(
            PackageId packagePackageId, 
            NuGetVersion version, 
            ToolCommandName toolCommandName,
            NuGetFramework optionalNuGetFramework = null)
        {
            PackageId = packagePackageId;
            Version = version;
            CommandName = toolCommandName;
            OptionalNuGetFramework = optionalNuGetFramework;
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

        public ToolManifestFindingResult Find()
        {
            string manifestFilePath = _probStart.WithSubDirectories(_manifestFilenameConvention).Value;

            var errors = new List<string>();
            var result = new List<ToolManifestFindingResultIndividualTool>();
            if (_fileSystem.File.Exists(manifestFilePath))
            {
                JObject manifest = JObject.Parse(_fileSystem.File.ReadAllText(manifestFilePath));
                foreach (var tools in manifest["tools"])
                {
                    var packageIdString = tools.Root.Value<string>();
                    NuGet.Packaging.PackageIdValidator.IsValidPackageId(packageIdString);
                    
                    errors.Add($"Package Id {packageIdString} is not valid");
                    
                    var packageId = new PackageId(packageIdString);

                    // TODO WUL NULL CHECK for all field
                    var versionParseResult = NuGetVersion.TryParse(tools["version"].Value<string>(), out var version);

                    NuGetFramework targetframework = null;
                    if (! (tools["version"] is null))
                    {
                        targetframework = NuGetFramework.Parse(
                            tools["version"].Value<string>());
                    }

                    var toolCommandName = 
                        new ToolCommandName(tools["commands"].Value<string>());
                    
                    result.Add(new ToolManifestFindingResultIndividualTool(packageId, version, toolCommandName, targetframework));
                }
            }
            
            // Just use throw
            return ToolManifestFindingResult.ToolManifestFindingResultWithResult(result);
        }
    }
}
