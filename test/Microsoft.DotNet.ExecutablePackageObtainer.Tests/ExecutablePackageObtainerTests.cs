// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NuGet.Protocol.Core.Types;
using Xunit;

namespace Microsoft.DotNet.ExecutablePackageObtainer.Tests
{
    public class ExecutablePackageObtainerTests // TODO the PackageObtainer should be called Executalbe Package Obtainer
    {
        [Fact]
        public void GivenSourceNameAndPasswordAndPackageNameAndVersionWhenCallItCanDownloadThePacakge()
        {
            var randomFileName = Path.GetRandomFileName();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), randomFileName); // TODO Nocheck in make it mock file system or windows only 
            var packageObtainer = new ExecutablePackageObtainer(toolsPath);
            var executablePath = packageObtainer.ObtainAndReturnExecutablePath("console.wul.test.app.1", "1.0.1");
            File.Exists(Path.Combine(executablePath)).Should().BeTrue(executablePath + " should have the executable");
        }
    }
}
