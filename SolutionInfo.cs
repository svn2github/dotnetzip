using System.Reflection;
using System.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("Dino Chiesa")]
[assembly: AssemblyProduct("DotNetZip Library")]
[assembly: AssemblyCopyright("Copyright © Dino Chiesa 2006 - 2011")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]


[assembly: AssemblyVersion("1.9.1.6005")]

#if !NETCF
[assembly: AssemblyFileVersion("1.9.1.6005")]
    #if !SILVERLIGHT
// workitem 4698
[assembly: AllowPartiallyTrustedCallers]
    #endif
#endif
