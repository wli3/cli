﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.DotNet.Cli.commands.uninstall {
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
                    System.Resources.ResourceManager temp = new System.Resources.ResourceManager("Microsoft.DotNet.Cli.commands.uninstall.LocalizableStrings", typeof(LocalizableStrings).Assembly);
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
        
        public static string PackageIdArgumentName {
            get {
                return ResourceManager.GetString("PackageIdArgumentName", resourceCulture);
            }
        }
        
        public static string PackageIdArgumentDescription {
            get {
                return ResourceManager.GetString("PackageIdArgumentDescription", resourceCulture);
            }
        }
        
        public static string SpecifyExactlyOnePackageId {
            get {
                return ResourceManager.GetString("SpecifyExactlyOnePackageId", resourceCulture);
            }
        }
        
        public static string CommandDescription {
            get {
                return ResourceManager.GetString("CommandDescription", resourceCulture);
            }
        }
        
        public static string GlobalOptionDescription {
            get {
                return ResourceManager.GetString("GlobalOptionDescription", resourceCulture);
            }
        }
        
        public static string UninstallSucceeded {
            get {
                return ResourceManager.GetString("UninstallSucceeded", resourceCulture);
            }
        }
        
        public static string ToolNotInstalled {
            get {
                return ResourceManager.GetString("ToolNotInstalled", resourceCulture);
            }
        }
        
        public static string ToolHasMultipleVersionsInstalled {
            get {
                return ResourceManager.GetString("ToolHasMultipleVersionsInstalled", resourceCulture);
            }
        }
        
        public static string FailedToUninstallTool {
            get {
                return ResourceManager.GetString("FailedToUninstallTool", resourceCulture);
            }
        }
        
        public static string UninstallToolCommandNeedGlobalOrToolPath {
            get {
                return ResourceManager.GetString("UninstallToolCommandNeedGlobalOrToolPath", resourceCulture);
            }
        }
        
        public static string ToolPathOptionName {
            get {
                return ResourceManager.GetString("ToolPathOptionName", resourceCulture);
            }
        }
        
        public static string ToolPathOptionDescription {
            get {
                return ResourceManager.GetString("ToolPathOptionDescription", resourceCulture);
            }
        }
        
        public static string UninstallToolCommandInvalidGlobalAndToolPath {
            get {
                return ResourceManager.GetString("UninstallToolCommandInvalidGlobalAndToolPath", resourceCulture);
            }
        }
        
        public static string InvalidToolPathOption {
            get {
                return ResourceManager.GetString("InvalidToolPathOption", resourceCulture);
            }
        }
        
        public static string ManifestPathOptionDescription {
            get {
                return ResourceManager.GetString("ManifestPathOptionDescription", resourceCulture);
            }
        }
        
        public static string ManifestPathOptionName {
            get {
                return ResourceManager.GetString("ManifestPathOptionName", resourceCulture);
            }
        }
        
        public static string LocalOptionDescription {
            get {
                return ResourceManager.GetString("LocalOptionDescription", resourceCulture);
            }
        }
    }
}
