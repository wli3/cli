using System;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Microsoft.DotNet.ToolPackage.ToolConfigurationDeserialization
{
    [Serializable]
    [DebuggerStepThrough]
    [XmlType(TypeName="Command")]
    public class RepoToolManifestCommand
    {
        [XmlAttribute]
        public string PackageId { get; set; }

        [XmlAttribute]
        public string Version { get; set; }

        [XmlAttribute]
        public string Configfile { get; set; }

        [XmlAttribute]
        public string Framework { get; set; }

        [XmlAttribute]
        public string AddSource { get; set; }
    }
}
