using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("WebApi.UnitTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace WebApi.Properties
{
    public class AssemblyInfo
    {
        internal string AssemblyVersion { get; } = "1.0.0";
    }
}
