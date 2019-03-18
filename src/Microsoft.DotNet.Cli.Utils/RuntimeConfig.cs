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
                            if (property.Name.Equals("name", StringComparison.OrdinalIgnoreCase))
                            {
                                runtimeConfigFramework.Name = property.Value.GetString();
                            }
                            
                            if (property.Name.Equals("version", StringComparison.OrdinalIgnoreCase))
                            {
                                runtimeConfigFramework.Version = property.Value.GetString();
                            }
                        }
                    }
                }

            }

       //     var runtimeOptionsRoot = runtimeConfigJson["runtimeOptions"];

       //     var framework = (JObject) runtimeOptionsRoot?["framework"]; 
            if (framework == null)
            {
                Framework = null;
            }
            else
            {
                var properties = framework.Properties(); 
 
                var name = properties.FirstOrDefault(p => p.Name.Equals("name", StringComparison.OrdinalIgnoreCase)); 
                var version = properties.FirstOrDefault(p => p.Name.Equals("version", StringComparison.OrdinalIgnoreCase)); 
 
                if (name == null || version == null)
                {
                    Framework = null;
                }
                else
                {
                    Framework = new RuntimeConfigFramework 
                    { 
                        Name = name.Value.ToString(), 
                        Version = version.Value.ToString() 
                    };
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

        private RuntimeConfigFramework ParseFramework(JObject runtimeConfigRoot) 
        { 
            var runtimeOptionsRoot = runtimeConfigRoot["runtimeOptions"];

            var framework = (JObject) runtimeOptionsRoot?["framework"]; 
            if (framework == null) 
            { 
                return null; 
            }

            var properties = framework.Properties(); 
 
            var name = properties.FirstOrDefault(p => p.Name.Equals("name", StringComparison.OrdinalIgnoreCase)); 
            var version = properties.FirstOrDefault(p => p.Name.Equals("version", StringComparison.OrdinalIgnoreCase)); 
 
            if (name == null || version == null) 
            { 
                return null; 
            } 
 
            return new RuntimeConfigFramework 
            { 
                Name = name.Value.ToString(), 
                Version = version.Value.ToString() 
            };
        } 
    } 
}
