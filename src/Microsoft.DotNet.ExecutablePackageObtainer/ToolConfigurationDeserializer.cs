// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ExecutablePackageObtainer
{
    public static class ToolConfigurationDeserializer
    {
        public static ToolConfiguration Deserialize(string pathToXml)
        {
            var serializer = new XmlSerializer(typeof(DotnetToolMetadata));

            DotnetToolMetadata dotnetToolMetadata;

            using (var fs = new FileStream(pathToXml, FileMode.Open))
            {
                var reader = XmlReader.Create(fs);
                dotnetToolMetadata = (DotnetToolMetadata)serializer.Deserialize(reader);
            }

            var commandName = dotnetToolMetadata.CommandName;
            var toolAssemblyEntryPoint = dotnetToolMetadata.ToolAssemblyEntryPoint;

            EntryPointType entryPointType;
            switch (dotnetToolMetadata.EntryPointType)
            {
                case DotnetToolMetadataEntryPointType.DotnetNetCoreAssembly:
                    entryPointType = EntryPointType.DotnetNetCoreAssembly;
                    break;
                case DotnetToolMetadataEntryPointType.NativeBinary:
                    entryPointType = EntryPointType.NativeBinary;
                    break;
                case DotnetToolMetadataEntryPointType.Script:
                    entryPointType = EntryPointType.Script;
                    break;
                default:
                    throw new Exception("TODO no checkin xml dersializtion failed, handle it outside!");
            }

            return new ToolConfiguration(commandName, toolAssemblyEntryPoint, entryPointType);
        }
    }
}
