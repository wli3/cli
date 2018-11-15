// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Xunit;
using Xunit.Abstractions;
using Parser = Microsoft.DotNet.Cli.Parser;

namespace Microsoft.DotNet.Tests.ParserTests
{
    public class ToolRunParserTests
    {
        private readonly ITestOutputHelper output;

        public ToolRunParserTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void ListToolParserCanGetToolCommandNameArgument()
        {
            var result = Parser.Instance.Parse("dotnet tool run dotnetsay");

            var appliedOptions = result["dotnet"]["tool"]["run"];
            var packageId = appliedOptions.Arguments.Single();

            packageId.Should().Be("dotnetsay");
        }

        [Fact]
        public void ListToolParserCanGetToolCommandNameArgumentAndCommandsArgument()
        {
            var result = Parser.Instance.Parse("dotnet tool run dotnetsay hi");

            var appliedOptions = result["dotnet"]["tool"]["run"];
            var packageId = appliedOptions.Arguments.Single();

            packageId.Should().Be("dotnetsay");
        }
    }
}
