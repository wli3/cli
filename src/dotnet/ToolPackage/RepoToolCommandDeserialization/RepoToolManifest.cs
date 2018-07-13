using System.Diagnostics;
using System.Xml.Serialization;

namespace Microsoft.DotNet.ToolPackage.ToolConfigurationDeserialization
{
    [DebuggerStepThrough]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class RepoToolManifest
    {
        [XmlArrayItem("RepoTools", IsNullable = false)]
        public RepoToolManifestCommand[] Commands { get; set; }

        [XmlAttribute(AttributeName = "Version")]
        public int Version { get; set; }
    }
}
