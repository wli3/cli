// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NuGet.Protocol.Core.Types;
using Xunit;

namespace Microsoft.DotNet.ShellShimMaker.Tests
{
    public class ShellShimMakerTests 
    {
        [Fact]
        public void GivenAnExecutablePathItCanGenerateShimFile()
        {
            var shellShimMaker = new ShellShimMaker();
            shellShimMaker.Should().Not().BeNull();
        }
    }
}
