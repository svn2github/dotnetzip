Fri, 19 Dec 2008  06:03

Zip Library 
---------------------------------

This library allows .NET applications to read, create and modify ZIP files. 

The Microsoft .NET Framework, starting with v2.0 for the desktop
Framework and v3.5 for the Compact Framework, includes new base class
libraries supporting compression within streams - both the Deflate and
Gzip formats are supported. But the classes in the System.IO.Compression 
namespace are not directly useful for creating compressed zip archives.  
The built-in compression library is not able to read or write zip archive 
headers and other meta data.

This is a simple class library that provides ZIP file support.  Using
this library, you can write .NET applications that read and write
zip-format files, including files with passwords, Unicode filenames, and
comments.  The library also supports ZIP64 and self-extracting archives.

DotNetZip works with applications running on PCs with Windows.  There is a
version of this library available for the .NET Compact Framework, too.


License
--------

This software is open source. It is released under the Microsoft Public License
of October 2006.  See the License.txt file for details. 



Dependencies
---------------------------------

Originally, this library was designed to depend upon the built-in 
System.IO.Compression.DeflateStream class for the compression.  This
proved to be less than satisfactory because the built-in compression
library did not support compression levels and also was not available on
.NET CF 2.0.

As of v1.7, the library includes a managed code version of zlib, the
library that produces RFC1950 and RFC1951 compressed streams.  Within
that version of zlib, there is also a DeflateStream class which is
similar to the built-in System.IO.Compression.DeflateStream, but more
flexible, and often more effective as well.

As a result, this library depends only on the .NET Framework v2.0, or the
.NET Compact Framework v2.0.



The Documentation
--------------------------------------------

There is a single .chm file for all of the DotNetZip library features,
including Zip and Zlib stuff.  If you only use the Zlib stuff, then you
should focus on the doc in the Ionic.Zlib namespace.  If you are
building apps for mobile devices running the Compact Framework, then
ignore the SaveSelfExtractor() pieces.

The .chm file is built using the Sandcastle Helpfile Builder tool, also
available on CodePlex at http://www.codeplex.com/SHFB .  It is built
from in-code xml documentation. 


About the Help file
--------------------------------

The .chm file contains help generated from the code.

In some cases, upon opening the .chm file for DotNetZip, the help
items tree loads, but the contents are empty. You may see an error:
"This program cannot display the webpage."  or, "Address is invalid."
If this happens, it's likely that you've encountered a problem with Windows
protection of files downloaded from less trusted locations. To work around 
this, within Windows Explorer, right-click on the CHM file, select properties, 
and Unblock it, using the button in lower part of properties window.




The Zip Format
---------------------------------
The zip format is described by PKWare, at
 http://www.pkware.com/business_and_developers/developer/popups/appnote.txt

Every valid zipfile conforms to this specification.  For
example, the spec says that for each compressed file contained
in the zip archive, the zipfile contains a byte array of
compressed data.  (The byte array is something the DeflateStream
class can produce directly.)  But the zipfile also contains
header and "directory" information - you might call this
"metadata".  In other words, the zipfile must contain a list of
all the compressed files in the archive. The zipfile also
contains CRC checksums, and can also contain comments, and other
optional attributes for each file.  These are things the
DeflateStream class - either the one included in the .NET Framework Class
Library, or the one embedded in this library - does not read or write.


This Package
---------------------------------

This package includes a managed ZLIB DLL, and a ZIP dll.  The latter
depends upon the capabilities included in the former.  

For each DLL, there is a version for the regular .NET
Framework and another for the Compact Framework. 



The Zlib Class Library
---------------------------------

The Zlib class library is packaged as Ionic.Zlib.DLL for the regular .NET
Framework and Ionic.Zlib.CF.dll for the Compact Framework.  The ZLIB
library does compression according to IETF RFC's 1950 and 1951.
See http://www.ietf.org/rfc/rfc1950.txt

The key classes are: 

  ZlibCodec - a class for Zlib (RFC1950/1951) encoding and decoding.
        This low-level class does deflation and inflation on buffers.

  DeflateStream - patterned after the DeflateStream in
        System.IO.Compression, this class supports compression
        levels and other options.


If you want to simply compress (deflate) raw block or stream data, this 
library is the thing you want.  

When building apps that do zlib stuff, you need to add a reference to
the Ionic.Zlib.dll in Visual Studio, or specify Ionic.Zlib.dll with the
/R flag on the CSC.exe or VB.exe compiler line.



The Zip Class Library
---------------------------------

The Zip class library is packaged as Ionic.Zip.DLL for the regular .NET
Framework and Ionic.Zip.CF.dll for the Compact Framework.  The Zip
library allows applications to create, read, and update zip files. 

This library uses the DeflateStream class to compress file data,
and extends it to support reading and writing of the metadata -
the header, CRC, and other optional data - defined or required
by the zip format spec.

The key object in the class library is the ZipFile class.  The key methods on it:
      - AddItem - adds a file or a directory to a zip archive
      - AddDirectory - adds a directory to a zip archive
      - AddFile - adds a file to a zip archive
      - Extract - extract a single element from a zip file
      - Read - static methods to read in an existing zipfile, for
               later extraction
      - Save - save a zipfile to disk

There is also a supporting class, called ZipEntry.  Applications
can enumerate the entries in a ZipFile, via ZipEntry.  There are
other supporting classes as well.  Typically apps do not
directly interact with these other classes.

If you want to create or read zip files, this library is the one you want.

When building apps that do zip stuff, you need to add a reference to
the Ionic.Zip.dll in Visual Studio, or specify Ionic.Zip.dll with the
/R flag on the CSC.exe or VB.exe compiler line.

NB: If your application does both Zlib and Zip stuff, you need only add
a reference to Ionic.Zip.dll.  Ionic.Zip.dll includes all the capability
in Ionic.Zlib.dll.



Using the Zip Class Library: The Basics
----------------------------------------

Check the examples included in the source package, or in the class 
reference documentation in the .CHM file, for code that illustrates
how to read and write zip files.  The simplest way to create 
a zipfile looks like this: 

      using(ZipFile zip= new ZipFile())
      {
        zip.AddFile(filename);
        zip.Save(NameOfZipFileTocreate); 
      }


The simplest way to Extract all the entries from a zipfile looks
like this: 
      using (ZipFile zip = ZipFile.Read(NameOfExistingZipFile))
      {
        zip.ExtractAll(args[1]);
      }

But you could also do something like this: 

      using (ZipFile zip = ZipFile.Read(NameOfExistingZipFile))
      {
        foreach (ZipEntry e in zip)
        {
          e.Extract();
        }
      }
      

Or in VB, it would be like this: 
     Using zip As ZipFile = ZipFile.Read(NameOfExistingZipFile)
         zip.ExtractAll
     End Using

Or this: 
     Using zip As ZipFile = ZipFile.Read(NameOfExistingZipFile)
        Dim e As ZipEntry
        For Each e In zip
            e.Extract
        Next
     End Using


There are a number of other options for using the class
library.  For example, you can read zip archives from streams,
or you can create (write) zip archives to streams, or you can extract 
into streams.  You can apply passwords for weak encryption.  You can 
specify a code page for the filenames and metadata of entries in an archive.  
You can rename entries in  archives, and you can add or remove entries from 
archives.  Check the doc for complete information. 



Namespace changes for the library
---------------------------------

With v1.7, the namespace for DotNetZip changed.  The old namespace was
Ionic.Utils.Zip, with classes like 
  Ionic.Utils.Zip.ZipFile
  Ionic.Utils.Zip.ZipEntry
  etc

The new namespace drops the "Utils" segment, and is now Ionic.Zip.
Classes are 
  Ionic.Zip.ZipFile
  Ionic.Zip.ZipEntry
  etc
  
In addition, v1.7 adds the zlib capability, so that there are classes
like:
  Ionic.Zlib.DeflateStream
  Ionic.Zlib.ZlibStream
  Ionic.Zlib.ZlibCodec




About Directory Paths
---------------------------------

One important note: the ZipFile.AddXxx methods add the file or
directory you specify, including the directory.  In other words,
logic like this:
    ZipFile zip = new ZipFile();
    zip.AddFile("c:\\a\\b\\c\\Hello.doc");
    zip.Save(); 

...will produce a zip archive that contains a single file, which
is stored with the relative directory information.  When you
extract that file from the zip, either using this Zip library or
winzip or the built-in zip support in Windows, or some other
package, all those directories will be created, and the file
will be written into that directory hierarchy.  

If you don't want that directory information in your archive,
then you need to use the overload of the AddFile() method that 
allows you to explicitly specify the directory used for the entry 
within the archive: 

    zip.AddFile("c:\\a\\b\\c\\Hello.doc", "files");
    zip.Save();
    
This will create an archive with an entry called "files\Hello.doc", 
which contains the contents of the on-disk file called 
c:\a\b\c\Hello.doc .  




The use of ILMerge
--------------------------------

This section is mostly interesting to developers who will work on the
source code of DotNetZip, to extend or re-purpose it.  If you only plan
to use DotNetZip in applications of your own, you probably don't need 
to concern yourself with this information.

Microsoft makes available a tool called ILMerge which is effectively a
managed library manager, similar to the lib tool in C toolkits.

http://www.microsoft.com/downloads/details.aspx?familyid=22914587-b4ad-4eae-87cf-b14ae6a939b0&displaylang=en 

With it, a developer can merge multiple assemblies into a single
assembly.  

DotNetZip packages two distinct libraries, and the ZIP library has a
hard dependency on the ZLIB library. Rather than require developers who
use DotNetZip to ship two DLLs with their zip-enabled applications, the
DotNetZip build uses ILMerge.

It works like this:
  The zlib library is built and signed  (Ionic.Zlib.dll)
  The "partial" zip library is built and signed (Ionic.Zip.Partial.dll)
  ILmerge is used to combine those two into a single assembly (Ionic.Zip.dll) 

In other words,  Ionic.Zip.dll is a strict superset of Ionic.Zlib.dll.  

This is true for the desktop DLL as well as the DLL for the Compact
Framework.  See the "Zip Full DLL" project and the "Zip CF Full DLL"
project - this is where the ILMerge steps are performed.

The implication for users of this library is that you should never
reference both Ionic.Zlib.dll and Ionic.Zip.dll in the same application.
If your application does both Zlib and Zip stuff, you need only add a
reference to Ionic.Zip.dll.  Ionic.Zip.dll includes all the capability
in Ionic.Zlib.dll.  You need to references only one Ionic DLL, regardless 
whether you use Zlib or Zip or both. 

Use case                                       Reference this DLL
------------------------------------------------------------------
block or stream compression                    Ionic.Zlib.dll
reading or writing Zip files                   Ionic.Zip.dll
both raw compression as well as reading 
   or writing Zip files                        Ionic.Zip.dll

raw compression on Compact Framework           Ionic.Zlib.CF.dll
reading or writing Zip files on Compact 
     Framework                                 Ionic.Zip.CF.dll
both raw compression as well as reading 
   or writing Zip files on CF                  Ionic.Zip.CF.dll




Self-Extracting Archive support
--------------------------------

The Self-Extracting Archive (SFX) support in the library allows you to 
create a self-extracting zip archive.  Essentially it is a standard EXE file
that contains boilerplate unzip code, as well as a zip file embedded as a
resource.  When the SFX runs, the application extracts the zip file resource 
and then unzips the file. 

This implies that running the SFX, unpacking from the SFX, requires the 
.NET Framework.  

There is no support for reading SFX files.  Once created, they are not readable 
by DotNetZip.  This may be added in a future release if users demand it. 

NB: Creation of SFX is not supported in the Compact Framework version of 
the library.




The Reduced ZIP library
--------------------------------

SFX support implies a large
increase in the size of the library.  Some deployments may
wish to omit the SFX support in order to get a smaller DLL. For that you can
rely on the Ionic.Zip.Reduced.dll.  It provides everything the normal
library does, except the SaveSelfExtractor() method on the ZipFile
class.

For size comparisons...

assembly              ~size   comment
-------------------------------------------------------
Ionic.Zlib.dll          77k   DeflateStream and ZlibCodec

Ionic.Zip.dll          335k   includes ZLIB and SFX

Ionic.Zip.Partial.dll  258k   includes SFX, depends on a separate Ionic.Zlib.dll
                              You should probably never reference this
                              DLL directly. It is a interim build output.
                              Included here for comparison purposes only.

Ionic.Zip.Reduced.dll  130k   includes ZLIB but not SFX

Ionic.Zlib.CF.dll       66k   DeflateStream and ZlibCodec (Compact Framework)

Ionic.Zip.CF.dll       140k   includes ZLIB but not SFX (Compact Framework)




Testing
--------------------------------------------

For those of you downloading the source, there are two source projects
in the VS Solution that contain Unit Tests: one for the zlib library and
another for the Zip library.

The zlib tests are much thinner than the zip tests at the moment. 



Examples
--------------------------------------------

The source solution also includes a number of example applications
showing how to use the DotNetZip library and all its features - creating
ZIPs, using Unicode, passwords, comments, and so on.  




Support
--------------------------------------------

There is no official support for this library.  I try to make a good
effort to monitor the discussions and work items raised on the project
portal at:
http://www.codeplex.com/DotNetZip.





About Other Intellectual Property
---------------------------------

I am no lawyer, but before using this library in your app, it
may be worth contacting PKWare for clarification on rights and
licensing.  The specification for the zip format includes a
paragraph that reads:

  PKWARE is committed to the interoperability and advancement of the
  .ZIP format.  PKWARE offers a free license for certain technological
  aspects described above under certain restrictions and conditions.
  However, the use or implementation in a product of certain technological
  aspects set forth in the current APPNOTE, including those with regard to
  strong encryption or patching, requires a license from PKWARE.  Please 
  contact PKWARE with regard to acquiring a license.

Contact pkware at:  zipformat@pkware.com 


This library also uses a CRC utility class, in modified form,
that was published on the internet without an explicit license.
You can find the original CRC class at:
  http://www.vbaccelerator.com/home/net/code/libraries/CRC32/Crc32_zip_CRC32_CRC32_cs.asp


This library uses a ZLIB implementation that is based on a conversion of
the jzlib project http://www.jcraft.com/jzlib/.  The license and
disclaimer required by the jzlib source license is included in the
relevant source files of DotNetZip, specifically in the sources for the
Zlib module.



Pre-requisites
---------------------------------

to run:
.NET Framework 2.0 or later


to build:
.NET Framework 3.5 SDK or later
or
Visual Studio 2008 or later



to run on a smart device:
  .NET Framework 2.0 or later



Building DotNetZip with the .NET SDK
-------------------------------------

To build this example, using the .NET Framework SDK v3.5,

1. extract the contents of the source zip into a new directory. 

2. be sure the .NET 2.0 SDK, .NET 3.5 runtime, and .NET 2.0 runtime 
   directories are on your path.  These are typically

     C:\Program Files\Microsoft.NET\SDK\v2.0\bin
     c:\windows\Microsoft.NET\Framework\v3.5
       and 
     c:\WINDOWS\Microsoft.NET\Framework\v2.0.50727


3. open a CMD prompt and CD to the DotNetZip directory.
  
4. msbuild 

5. to clean and rebuild, do
   msbuild /t:clean
   msbuild

6. There is a setup directory, which contains the project
   necessary to build the MSI file.  Unfortunately msbuild does
   not include support for building setup projects (vdproj). 



Building DotNetZip with Visual Studio
-------------------------------------

Of course you can also open the DotNetZip Solution within Visual
Studio 2008 and use it to build the various projects, including
the setup project for the MSI File.

To do this, just double click on the .sln file.  
Then right click on the solution, and select Build. 




Signing the assembly
-------------------------------------------------------

The binary DLL shipped in the codeplex project is signed by me, Ionic
Shade.  This provides a "strong name" for the assembly, which itself
provides some assurance as to the integrity of the library, and also
allows it to be run within restricted sites, like apps running inside
web hosters.

For more on strong names, see this article:
http://msdn.microsoft.com/en-gb/magazine/cc163583.aspx

Signing is done automatically at build time in the vs2008 project. There
is a .pfx file that holds the crypto stuff for signing the assembly, and
that pfx file is itself protected by a password. There is also an
Ionic.snk file which is referenced by the project, but which I do not
distribute.

People opening the project ask me: what's the password to this .pfx
file?  Where's the .snk file?

Here's the problem; if I give everyone the password to the PFX file or
the .snk file, then anyone can go and build a modified DotNetZip.dll,
and sign it with my key, and apply the same version number.  This means
there could be multiple distinct assemblies with the same signature.
This is obviously not good.  

Since I don't release the ability to sign DLLs with my key, 
the DLL signed with my key is guaranteed to be from me only. If
anyone wants to modify the project and party on it, they have a couple
options: 
  - sign the assembly themselves, using their own key.
  - produce a modified, unsigned assembly 

In either case it is not the same as the assembly I am shipping,
therefore it should not be signed with the same key. 

mmkay? 

As for those options above, here is some more detail:

  1. If you want a strong-named assembly, then create your own PFX file
     and .snk file and modify the appropriate projects to use those new
     files. 

  2. If you don't need a strong-named assembly, then remove all the
     signing from the various projects.

In either case, You will need to modify the "Zip Full DLL" and "Zip CF Full
DLL" projects, as well as the "Zlib" and "Zlib CF" projects.




Building the Help File
--------------------------------------------
If you want to build the helpfile, you need the SandCastle
helpfile builder.  Use the DotNetZip.shfb file with SandCastle.
You can get the builder tool at http://www.codeplex.com/SHFB



Limitations
---------------------------------

There are numerous limitations to this library:

 it does not support "multi-disk archives." or "disk spanning"

 The GUI tool for creating zips is pretty basic.

 and, I'm sure, many others

But it is a good basic library for reading and writing zipfiles
in .NET applications.

And yes, the zipfile that this example is shipped in, was
produced by this example library. 




Origins
---------------------------------

This library is mostly original code. 

There is a GPL-licensed library called SharpZipLib that writes zip
files, it can be found at
http://www.sharpdevelop.net/OpenSource/SharpZipLib/Default.aspx

This example library is not based on SharpZipLib.  

There is a Zip library as part of the Mono project.  This
library is also not based on that.

Now that the Java class library is open source, there is at least bone
open-source Java implementation for zip.  This implementation is not
based on a port of Sun's JDK code.

There is a zlib.net project from ComponentAce.com.  This library is not
based on that code. 

This library is all new code, written by me, with these exceptions:

 -  the CRC32 class - see above for credit.
 -  the zlib library - see above for credit.

