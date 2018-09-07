// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.InternalAbstractions;

namespace Microsoft.DotNet.ToolPackage
{
    internal struct ToolCommandName
    {
        private string Value { get; }

        public ToolCommandName(string name)
        {
            if ()
            Value = name ?? throw new ArgumentNullException(nameof(name));
        }

        public static bool HasLeadingDot(string name)
        {
            return name.StartsWith(".", StringComparison.OrdinalIgnoreCase);
        }

        public static bool HasInvalidFilenameCharacters(string name, out string invalidCharacters)
        {

        }
    }
}
