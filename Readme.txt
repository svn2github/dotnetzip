Mon, 03 Apr 2006  10:26

Zip Library 
---------------------------------

The Microsoft .NET Framework {v2.0 v3.0 v3.5} includes new base class
libraries supporting compression within streams - both the
Deflate and Gzip formats are supported. But the new-for-.NET2.0
System.IO.Compression namespace provides streaming compression
only - useful for communicating between cooperating parties but
not directly useful for creating compressed archives, like .zip
files. The built-in compression library does not know how to
format zip archive headers and so on.  

This is a simple class library that augments the 
System.IO.Compression.DeflateStream class, to provide handling
for Zip files.  Using this library, you can write .NET
applications that read and write zip-format files. 


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
DeflateStream class, included in the .NET Framework Class
Library, does not read or write.


This Class Library
---------------------------------

The library included here depends on the DeflateStream class,
and extends it to support reading and writing of the metadata -
the header, CRC, and other optional data - defined or required
by the zip format spec.

The key object in the class library is the ZipFile class.  The key methods on it:
      - AddItem - adds a file or a directory to a zip archive
      - AddDirectory - adds a directory to a zip archive
      - AddFile - adds a file to a zip archive
      - Extract - extract a single element from a zip file
      - Read - static method to read in an existing zipfile, for
               later extraction
      - Save - save a zipfile to disk

There are also supporting classes, called ZipEntry, and
ZipDirEntry.  Typically apps do not directly interact with these
classes.


Using the Class Library
---------------------------------

Check the examples included in this package for simple apps that
show how to read and write zip files.  The simplest way to
create a zipfile looks like this: 

      using(ZipFile zip= new ZipFile(NameOfZipFileTocreate))
      {
        zip.AddFile(filename);
	zip.Save(); 
      }


The simplest way to Extract all the entries from a zipfile looks
like this: 
      using (ZipFile zip = ZipFile.Read(NameOfExistingZipFile))
      {
        zip.ExtractAll(args[1]);
      }


There are a number of other options for using the class
library.  For example, you can read zip archives from streams,
or you can create (write) zip archives to streams.  Check the
doc for complete information. 






About Directory Paths
---------------------------------

One important note: the ZipFile.AddXxx methods add the file or
directory you specify, including the directory.  In other words,
logic like this:
    
        zip.AddFile("c:\\a\\b\\c\\Hello.doc");
	zip.Save(); 

...will produce a zip archive that contains a single file, which
is stored with the relative directory information.  When you
extract that file from the zip, either using this Zip library or
winzip or the built-in zip support in Windows, or some other
package, all those directories will be created, and the file
will be written into that directory hierarchy.  

If you don't want that directory information in your archive,
then you need to either 
 (a) copy the file or files to be compressed into the local
     directory
 (b) change the applications current directory to where the file
     resides, before adding it to the zipfile.

The latter involves a call to
System.IO.Directory.SetCurrentDirectory(), 
before you call ZipFile.AddXxx().

See the doc:
http://msdn2.microsoft.com/en-us/library/system.io.directory.setcurrentdirectory.aspx





License
--------

This software is released under the Microsoft Permissive License
of OCtober 2006.  See the License.txt file for details. 



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


This example also uses a CRC utility class, in modified form,
that was published on the internet without an explicit license.
You can find the original CRC class at:
  http://www.vbaccelerator.com/home/net/code/libraries/CRC32/Crc32_zip_CRC32_CRC32_cs.asp



Pre-requisites
---------------------------------

to run:
.NET Framework 2.0 or later

to build:
.NET Framework 2.0 SDK or later
or
Visual Studio 2005 or later



Building
---------------------------------

To build this example, 

1. extract the contents of this zip into a new directory. 

2. be sure the .NET 2.0 SDK and .NET 2.0 runtime directories
   are on your path.  These are typically

     C:\Program Files\Microsoft.NET\SDK\v2.0\bin
       and 
     c:\WINDOWS\Microsoft.NET\Framework\v2.0.50727

3. open a CMD prompt and CD to the zip\Library directory. 
  
4. nmake

5. To build the examples, cd ..\Examples and type nmake again




Limitations
---------------------------------

There are numerous limitations to this library:

 it does not support encryption, file comments, or double-byte
 chars in filenames.

 it does not support file lengths greater than 0xffffffff.

 it does not support "multi-disk archives."

 it does not do varying compression levels. 

 it can actually expand the size of previously compressed data,
 such as JPG files.

 there is no GUI tool

 and, I'm sure, many others

But it is a good basic library for reading and writing zipfiles
in .NET applications..

And yes, the zipfile that this example is shipped in, was
produced by this example library. 



See Also
---------------------------------
There is a GPL-licensed library that writes zip files, it is
called SharpZipLib and can be found at 
http://www.sharpdevelop.net/OpenSource/SharpZipLib/Default.aspx

This example library is not based on SharpZipLib.  



