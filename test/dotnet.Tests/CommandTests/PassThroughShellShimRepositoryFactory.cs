// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.ShellShim;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Tests.Commands
{
    internal class PassThroughShellShimRepositoryFactory : IShellShimRepositoryFactory
    {
        private readonly IShellShimRepository _shellShimRepository;

        public PassThroughShellShimRepositoryFactory(IShellShimRepository shellShimRepository)
        {
            _shellShimRepository = shellShimRepository;
        }

        public IShellShimRepository CreateShellShimRepository(DirectoryPath? nonGlobalLocation = null)
        {
            return _shellShimRepository;
        }
    }
}
