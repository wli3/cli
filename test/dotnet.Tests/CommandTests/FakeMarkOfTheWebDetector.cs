﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tests.Commands
{
    internal class FakeMarkOfTheWebDetector : IMarkOfTheWebDetector
    {
        public FakeMarkOfTheWebDetector(params string[] filesHaveIt)
        {
            FilesHaveIt = filesHaveIt;
        }

        public string[] FilesHaveIt { get; }

        public bool HasMarkOfTheWeb(string filePath)
        {
            if (FilesHaveIt != null && FilesHaveIt.Any(f => f == filePath))
            {
                return true;
            }

            return false;
        }
    }
}
