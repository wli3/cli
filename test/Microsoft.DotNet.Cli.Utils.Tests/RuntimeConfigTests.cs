// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Tools.Common;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Cli.Utils.Tests
{
    public class RuntimeConfigTests : TestBase
    {
        [Fact]
        void ParseBasicRuntimeConfig()
        {
            var tempPath = Path.Combine(TempRoot.Root, nameof(RuntimeConfigTests), Path.GetTempFileName());
            File.WriteAllText(tempPath, _jsonContentInvalidJson);
            var runtimeConfig = new RuntimeConfig(tempPath);
            runtimeConfig.Framework.Version.Should().Be("2.1.0");
            runtimeConfig.Framework.Name.Should().Be("Microsoft.NETCore.App");
        }

        private string _jsonContentInvalidJson =
            @"{
  ""runtimeOptions"": {
    ""tfm"": ""netcoreapp2.1"",
    ""framework"": {
      ""name"": ""Microsoft.NETCore.App"",
      ""version"": ""2.1.0""
    }
  }
}";
    }
}
