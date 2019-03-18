// Copyright (c) .NET Foundation and contributors. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Text.Json;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Cli.Utils
{
    public class RuntimeConfig
    {
        public bool IsPortable { get; }
        internal RuntimeConfigFramework Framework { get; }

        public RuntimeConfig(string runtimeConfigPath)
        {
            using (var stream = File.OpenRead(runtimeConfigPath))
            using (JsonDocument doc = JsonDocument.Parse(stream))
            {
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("runtimeOptions", out var runtimeOptionsRoot))
                {
                    if (root.TryGetProperty("framework", out var framework))
                    {
                        var runtimeConfigFramework = new RuntimeConfigFramework();
                        foreach (var property in framework.EnumerateObject())
                        {
                            string name = null;
                            string version = null;
                            if (property.Name.Equals("name", StringComparison.OrdinalIgnoreCase))
                            {
                                name = property.Value.GetString();
                            }

                            if (property.Name.Equals("version", StringComparison.OrdinalIgnoreCase))
                            {
                                version = property.Value.GetString();
                            }

                            if (name == null || version == null)
                            {
                                Framework = null;
                            }
                            else
                            {
                                Framework = new RuntimeConfigFramework
                                {
                                    Name = name,
                                    Version = version
                                };
                            }
                        }
                    }
                    else
                    {
                        Framework = null;
                    }

                    
                }
            }

            IsPortable = Framework != null;
        }

        public static bool IsApplicationPortable(string entryAssemblyPath) 
        { 
            var runtimeConfigFile = Path.ChangeExtension(entryAssemblyPath, FileNameSuffixes.RuntimeConfigJson); 
            if (File.Exists(runtimeConfigFile)) 
            { 
                var runtimeConfig = new RuntimeConfig(runtimeConfigFile); 
                return runtimeConfig.IsPortable; 
            } 
            return false; 
        }
    } 
}
