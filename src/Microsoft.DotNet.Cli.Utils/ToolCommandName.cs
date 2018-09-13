// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using NuGet.Protocol.Plugins;

namespace Microsoft.DotNet.Cli.Utils
{
    public struct ToolCommandName : IEquatable<ToolCommandName>
    {
        public bool Equals(ToolCommandName other)
        {
            return string.Equals(Value, other.Value, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is ToolCommandName name && Equals(name);
        }

        public override int GetHashCode()
        {
            return StringComparer.InvariantCultureIgnoreCase.GetHashCode(Value);
        }

        public static bool operator ==(ToolCommandName name1, ToolCommandName name2)
        {
            return name1.Equals(name2);
        }

        public static bool operator !=(ToolCommandName name1, ToolCommandName name2)
        {
            return !name1.Equals(name2);
        }

        public string Value { get; }

        public ToolCommandName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name)); 
            }

            Value = name;
        }
    }
}
