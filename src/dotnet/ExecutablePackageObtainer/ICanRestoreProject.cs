﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ExecutablePackageObtainer
{
    internal interface ICanRestoreProject
    {
        void Restore(
            FilePath tempProjectPath,
            DirectoryPath assetJsonOutput, 
            FilePath? nugetconfig);
    }
}
