using System.Reflection;
using System.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("Ionic's Zip Library (Reduced)")]
[assembly: AssemblyCompany("Microsoft")]
[assembly: AssemblyProduct("DotNetZip Library")]
[assembly: AssemblyCopyright("Copyright © Microsoft 2009")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]



#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyDescription("a library for handling zip archives. http://www.codeplex.com/DotNetZip.  This is a reduced version; it lacks SFX support. (Flavor=Debug)")]
#else
[assembly: AssemblyConfiguration("Retail")]
[assembly: AssemblyDescription("a library for handling zip archives. http://www.codeplex.com/DotNetZip.  This is a reduced version; it lacks SFX support. (Flavor=Retail)")]
#endif


// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(true)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("918818b1-7141-49b3-bbdf-858588ad19bc")]

[assembly:System.CLSCompliant(true)]

// workitem 4698
[assembly: AllowPartiallyTrustedCallers] 

[assembly: AssemblyVersion("1.8.3.31")]
[assembly: AssemblyFileVersion("1.8.3.31")]

