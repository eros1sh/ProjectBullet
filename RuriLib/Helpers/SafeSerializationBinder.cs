using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;

namespace RuriLib.Helpers
{
    public class SafeSerializationBinder : DefaultSerializationBinder
    {
        private static readonly string[] AllowedAssemblyPrefixes = {
            "RuriLib", "ProjectBullet", "System.Private.CoreLib",
            "System.Collections", "mscorlib", "netstandard"
        };

        private static readonly string[] BlockedTypePatterns = {
            "System.Diagnostics.Process",
            "System.CodeDom",
            "System.Windows.Data.ObjectDataProvider",
            "System.Activities",
            "System.IdentityModel",
            "System.Web.UI",
            "System.Xaml",
            "Microsoft.VisualStudio",
            "System.Configuration.Install",
            "System.Management.Automation",
            "System.Runtime.Remoting",
            "System.ServiceModel",
        };

        public override Type BindToType(string? assemblyName, string typeName)
        {
            if (BlockedTypePatterns.Any(b => typeName.Contains(b, StringComparison.OrdinalIgnoreCase)))
                throw new JsonSerializationException(
                    $"// SECURITY FIX: Deserialization of type '{typeName}' is blocked (potential RCE gadget).");

            if (!string.IsNullOrEmpty(assemblyName) &&
                !AllowedAssemblyPrefixes.Any(p => assemblyName.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                throw new JsonSerializationException(
                    $"// SECURITY FIX: Assembly '{assemblyName}' is not in the allowed list for deserialization.");

            return base.BindToType(assemblyName, typeName);
        }
    }
}
