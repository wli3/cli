// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Cli.Utils
{
    public struct ToolCommandName : IEquatable<ToolCommandName>
    {
        public ToolCommandName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Value = name;
        }

        public string Value { get; }

        public override string ToString() => Value;

        public bool Equals(ToolCommandName other)
        {
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is ToolCommandName name && Equals(name);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
        }

        public static bool operator ==(ToolCommandName name1, ToolCommandName name2)
        {
            return name1.Equals(name2);
        }

        public static bool operator !=(ToolCommandName name1, ToolCommandName name2)
        {
            return !name1.Equals(name2);
        }
    }
}