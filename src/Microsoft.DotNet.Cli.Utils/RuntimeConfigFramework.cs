// Copyright (c) .NET Foundation and contributors. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
 
using System;  
using System.Linq; 
using Newtonsoft.Json.Linq;  // TODO json
 
namespace Microsoft.DotNet.Cli.Utils
{ 
    internal class RuntimeConfigFramework
    { 
        public string Name { get; set; } 
        public string Version { get; set; }
    } 
}
