// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Tool.List;

namespace Microsoft.DotNet.Tools.Tool.Common
{
    internal class ToolAppliedOption
    {
        public const string GlobalOption = "global";
        public const string LocalOption = "local";
        public const string ToolPathOption = "tool-path";
        internal static void EnsureNoConflictGlobalLocalToolPathOption(
            AppliedOption appliedOption,
            string message)
        {
            List<string> options = new List<string>();
            if (appliedOption.ValueOrDefault<bool>(GlobalOption))
            {
                options.Add(GlobalOption);
            }

            if (appliedOption.ValueOrDefault<bool>(LocalOption))
            {
                options.Add(LocalOption);
            }

            if (!String.IsNullOrWhiteSpace(appliedOption.SingleArgumentOrDefault(ToolPathOption)))
            {
                options.Add(ToolPathOption);
            }

            if (options.Count > 1)
            {
                throw new GracefulException(
                    String.Format(
                        message,
                        String.Join(" ", options)));
            }
        }
    }
}
