﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.DotNet.Cli.ToolManifest {
    using System;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class LocalizableStrings {
        
        private static System.Resources.ResourceManager resourceMan;
        
        private static System.Globalization.CultureInfo resourceCulture;
        
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal LocalizableStrings() {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public static System.Resources.ResourceManager ResourceManager {
            get {
                if (object.Equals(null, resourceMan)) {
                    System.Resources.ResourceManager temp = new System.Resources.ResourceManager("Microsoft.DotNet.Cli.ToolManifest.LocalizableStrings", typeof(LocalizableStrings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public static System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        public static string CannotFindAnyManifestsFileSearched {
            get {
                return ResourceManager.GetString("CannotFindAnyManifestsFileSearched", resourceCulture);
            }
        }
        
        public static string FieldCommandsIsMissing {
            get {
                return ResourceManager.GetString("FieldCommandsIsMissing", resourceCulture);
            }
        }
        
        public static string InvalidManifestFilePrefix {
            get {
                return ResourceManager.GetString("InvalidManifestFilePrefix", resourceCulture);
            }
        }
        
        public static string MissingVersion {
            get {
                return ResourceManager.GetString("MissingVersion", resourceCulture);
            }
        }
        
        public static string PackageNameAndErrors {
            get {
                return ResourceManager.GetString("PackageNameAndErrors", resourceCulture);
            }
        }
        
        public static string TargetFrameworkIsUnsupported {
            get {
                return ResourceManager.GetString("TargetFrameworkIsUnsupported", resourceCulture);
            }
        }
        
        public static string VersionIsInvalid {
            get {
                return ResourceManager.GetString("VersionIsInvalid", resourceCulture);
            }
        }
    }
}
