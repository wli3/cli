// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
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
using NuGet.Frameworks;
using NuGet.Versioning;
using Xunit;
using LocalizableStrings = Microsoft.DotNet.Tools.Tool.Restore.LocalizableStrings;
using Parser = Microsoft.DotNet.Cli.Parser;

namespace Microsoft.DotNet.Tests.Commands
{
    public class ToolManifestFile
    {
        private readonly IFileSystem _fileSystem;
    

        public ToolManifestFile()
        {
            _fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();
        }

        [Fact(Skip ="")]
        public void GivenManifestFileOnSameDirectoryItGetContent()
        {
            
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
    }
}
