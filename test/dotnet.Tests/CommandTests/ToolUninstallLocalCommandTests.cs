// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools.Tool.Install;
using Microsoft.DotNet.Tools.Tests.ComponentMocks;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.DotNet.ShellShim;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using Newtonsoft.Json;
using Xunit;
using Parser = Microsoft.DotNet.Cli.Parser;
using System.Runtime.InteropServices;
using NuGet.Versioning;
using LocalizableStrings = Microsoft.DotNet.Tools.Tool.Install.LocalizableStrings;
using Microsoft.DotNet.ToolManifest;
using NuGet.Frameworks;


namespace Microsoft.DotNet.Tests.Commands
{
    public class ToolUninstallLocalCommandTests
    {
        private readonly IFileSystem _fileSystem;
        private readonly AppliedOption _appliedCommand;
        private readonly ParseResult _parseResult;
        private readonly BufferedReporter _reporter;
        private readonly string _temporaryDirectory;
        private readonly string _manifestFilePath;
        private readonly PackageId _packageIdA = new PackageId("dotnetsay");
        private readonly ToolManifestFinder _toolManifestFinder;
        private readonly ToolManifestEditor _toolManifestEditor;

        public ToolUninstallLocalCommandTests()
        {
            _reporter = new BufferedReporter();
            _fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();
            _temporaryDirectory = _fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath;

            _manifestFilePath = Path.Combine(_temporaryDirectory, "dotnet-tools.json");
            _fileSystem.File.WriteAllText(Path.Combine(_temporaryDirectory, _manifestFilePath), _jsonContent);
            _toolManifestFinder = new ToolManifestFinder(new DirectoryPath(_temporaryDirectory), _fileSystem);
            _toolManifestEditor = new ToolManifestEditor(_fileSystem);

            _parseResult = Parser.Instance.Parse($"dotnet tool install {_packageIdA.ToString()}");
            _appliedCommand = _parseResult["dotnet"]["tool"]["install"];
        }

        [Fact]
        public void WhenRunWithPackageIdItShouldRemoveFromManifestFile()
        {
            var toolUninstallLocalCommand = new ToolUninstallLocalCommand(
                _appliedCommand, 
                _parseResult,
                _toolManifestFinder,
                _toolManifestEditor,
                _reporter);

            toolUninstallLocalCommand.Execute().Should().Be(0);
            
            var _jsonContent =
            @"{
   ""version"":1,
   ""isRoot"":true,
   ""tools"":{
      ""t-rex"":{
         ""version"":""1.0.53"",
         ""commands"":[
            ""t-rex""
         ]
      }
   }
}";
            _fileSystem.File.ReadAllText(_manifestFilePath).Should().Be(_jsonContent);
        }

        [Fact]
        public void GivenNoManifestFileItShouldThrow()
        {
            // TODO wul no check in
        }

        [Fact]
        public void WhenRunWithExplicitManifestFileItShouldRemoveFromExplicitManifestFile()
        {
            // TODO wul no check in
        }

        [Fact]
        public void WhenRunFromToolUninstallRedirectCommandWithPackageIdItShouldRemoveFromManifestFile()
        {
            // TODO wul no check in
        }

        [Fact]
        public void WhenRunWithPackageIdItShouldShowSuccessMessage()
        {
            // TODO wul no check in
        }
 
        private string _jsonContent =
            @"{
   ""version"":1,
   ""isRoot"":true,
   ""tools"":{
      ""t-rex"":{
         ""version"":""1.0.53"",
         ""commands"":[
            ""t-rex""
         ]
      },
      ""dotnetsay"":{
         ""version"":""2.1.4"",
         ""commands"":[
            ""dotnetsay""
         ]
      }
   }
}";
    }
}
