// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using Newtonsoft.Json;
using NuGet.Frameworks;
using NuGet.Versioning;
using Xunit;

namespace Microsoft.DotNet.ToolPackage.Tests
{
    internal class ToolManifestFileCreator : TestBase
    {
        public string CreateTestManifestFile()
        {
            return JsonConvert.SerializeObject(
                new LocalTools
                {
                    version = "1",
                    isRoot = true,
                    tools = new Dictionary<string, localtool>
                    {
                        {
                            "t-rex",
                            new localtool
                            {
                                version = "1.0.53", commands = new string[] {"t-rex"},
                                targetFramework = "netcoreapp2.1",
                                runtimeIdentifier = "win-x64"
                            }
                        },
                        {
                            "dotnetsay",
                            new localtool
                            {
                                version = "2.1.4", commands = new string[] {"dotnetsay"}
                            }
                        }
                    }
                });
        }

        private class LocalTools
        {
            public string version { get; set; }
            public bool isRoot { get; set; }
            public Dictionary<string, localtool> tools { get; set; }
        }

        private class localtool
        {
            public string version { get; set; }
            public string[] commands { get; set; }
            public string targetFramework { get; set; }
            public string runtimeIdentifier { get; set; }
        }
    }
}
