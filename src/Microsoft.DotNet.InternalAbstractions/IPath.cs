// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Extensions.EnvironmentAbstractions
{
    internal interface IPath
    {
        string Combine(params string[] paths);
        string GetFullPath(string path);
        bool IsPathRooted(string path);
        string GetDirectoryName(string path);
    }
}
