// Copyright (c) .NET Foundation and contributors. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.IO;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Tests
{
    public class GivenProjectWithMSBuildSDk : TestBase
    {
        //private TestAssetInstance _testInstance;

        public GivenProjectWithMSBuildSDk()
        {
            //_testInstance = TestAssets.Get("VBTestApp")
            //                .CreateInstance()
            //                .WithSourceFiles();

            //new RestoreCommand()
            //    .WithWorkingDirectory(_testInstance.Root)
            //    .Execute()
            //    .Should().Pass();
        }

        [Fact]
        public void apple_sauce()
        {
            var testAppName = "TestAppSimple";
            var testInstance = TestAssets.Get(testAppName)
                .CreateInstance()
                .WithSourceFiles()
                .WithProjectChanges(project => {
                    var ns = project.Root.Name.Namespace;

                    var itemGroup = new XElement(ns + "Sdk", 
                        new XAttribute("Name", "mock.nuget.auth.msbuildsdk"),
                        new XAttribute("Version", "1.0.0"));
                    project.Root.Add(itemGroup);
                })
                .WithNuGetConfig(new RepoDirectoriesProvider().TestPackages)
                .WithRestoreFiles();
        }

    }
}
