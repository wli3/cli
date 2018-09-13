// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.EnvironmentAbstractions;

// TODO wul no checkin move it to tools
namespace Microsoft.DotNet.Tests.Commands
{
    internal class ManifestFileFinder
    {
        private IFileSystem _fileSystem;

        public ManifestFileFinder(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        
    }

    internal interface IEmumerable
    {
    }
}
