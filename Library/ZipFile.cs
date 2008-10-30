// ZipFile.cs
//
// Copyright (c) 2006, 2007, 2008 Microsoft Corporation.  All rights reserved.
//
// This class library reads and writes zip files, according to the format
// described by pkware, at:
// http://www.pkware.com/business_and_developers/developer/popups/appnote.txt
//
// This implementation is based on the
// System.IO.Compression.DeflateStream base class in the .NET Framework
// v2.0 base class library.
//
// There are other Zip class libraries available.  For example, it is
// possible to read and write zip files within .NET via the J# runtime.
// But some people don't like to install the extra DLL.  Also, there is
// a 3rd party LGPL-based (or is it GPL?) library called SharpZipLib,
// which works, in both .NET 1.1 and .NET 2.0.  But some people don't
// like the GPL, and some people say it's complicated and slow. 
// Finally, there are commercial tools (From ComponentOne,
// XCeed, etc).  But some people don't want to incur the cost.
//
// This alternative implementation is not GPL licensed, is free of cost,
// and does not require J#. It does require .NET 2.0 (for the DeflateStream 
// class).  
// 
// This code is released under the Microsoft Public License . 
// See the License.txt for details.  
//
// Bugs:
// 1. does not do 0..9 compression levels (not supported by DeflateStream)
// 2. does only PKZIP encryption, which is weak.  No Strong Encryption (yet?)
// 3. no support for reading or writing multi-disk zip archives
// 4. no support for double-byte chars in filenames
// 5. no support for asynchronous operation
// 
// But it does read and write basic zip files, and it gets reasonable compression. 
//
// Fri, 31 Mar 2006  14:43
//


using System;
using System.IO;


namespace Ionic.Utils.Zip
{
    /// <summary>
    /// The ZipFile type represents a zip archive file.  This is the main type in the 
    /// DotNetZip class library.  This class reads and writes zip files, as defined in the format
    /// for zip described by PKWare.  The compression for this implementation is based on the
    /// System.IO.Compression.DeflateStream base class in the .NET Framework
    /// base class library, for v2.0 and later.
    /// </summary>
    public partial class ZipFile : System.Collections.Generic.IEnumerable<ZipEntry>,
    IDisposable
    {

        #region public properties

        /// <summary>
        /// This read-only property specifies the name of the zipfile to read or write. It is 
        /// set when the instance of the ZipFile type is created. When instantiating a ZipFile 
        /// to read from or write to a stream, the Name property remains null.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// A comment attached to the zip archive.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This property is read/write for the zipfile. It allows the application to
        /// specify a comment for the zipfile, or read the comment for the zipfile. 
        /// If setting the comment, changes are only made permanent when you call a
        /// <c>Save()</c> method.
        /// </para>
        /// <para>
        /// According to the zip spec, the comment is not encrypted, even if there is a password
        /// set on the zip archive. 
        /// </para>
        /// </remarks>
        public string Comment
        {
            get { return _Comment; }
            set
            {
                _Comment = value;
                _contentsChanged = true;
            }
        }

        /// <summary>
        /// When this is set, any volume name (eg C:) is trimmed 
        /// from fully-qualified pathnames on any ZipEntry, before writing the 
        /// ZipEntry into the ZipFile. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The default value is true. This setting must be true to allow 
        /// Windows Explorer to read the zip archives properly. It's also required to be 
        /// true if you want to read the generated zip files on any other non-Windows OS. 
        /// </para>
        /// 
        /// <para>
        /// The property is included for backwards compatibility only.  You'll 
        /// almost never need or want to set this to false.
        /// </para>
        /// </remarks>
        ///
        public bool TrimVolumeFromFullyQualifiedPaths
        {
            get { return _TrimVolumeFromFullyQualifiedPaths; }
            set { _TrimVolumeFromFullyQualifiedPaths = value; }
        }

        /// <summary>
        /// Indicates whether verbose output is sent to the StatusMessageWriter
        /// during <c>AddXxx()</c> and <c>ReadXxx()</c> operations. 
        /// </summary>
        /// <remarks>
        /// This is a synthetic property.  It returns true if the <see cref="StatusMessageTextWriter">StatusMessageTextWriter</see> is non-null. 
        /// </remarks>
        private bool Verbose
        {
            get { return (_StatusMessageTextWriter != null); }
            //set { _Verbose = value; }
        }


        /// <summary>
        /// Indicates whether to perform case-sensitive matching on the
        /// filename when retrieving entries in the zipfile via the string-based indexer. 
        /// The default value is false, which means DON'T do case-sensitive matching.
        /// It really makes sense to set this to TRUE only if you are not running on
        /// Windows, which has case-insensitive filenames.  In most cases you should just 
        /// leave this property alone. 
        /// </summary>
        public bool CaseSensitiveRetrieval
        {
            get { return _CaseSensitiveRetrieval; }
            set { _CaseSensitiveRetrieval = value; }
        }


        /// <summary>
        /// Indicates whether to encode entry filenames and entry comments using Unicode 
        /// (UTF-8) according to the PKWare specification, for those filenames and comments
        /// that cannot be encoded in the IBM437 character set.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The PKWare specification provides for encoding in either the IBM437 code page, or in UTF-8. 
        /// This flag selects the encoding according to that specification. 
        /// By default, this flag is false, and filenames and comments are encoded 
        /// into the zip file in the IBM437 codepage. 
        /// Setting this flag to true will specify that
        /// filenames and comments are encoded with UTF-8. 
        /// </para>
        /// <para>
        /// Zip files created with strict adherence to the PKWare specification
        /// with respect to UTF-8 encoding can contain entries with filenames containing
        /// any combination of Unicode characters, including the full 
        /// range of characters from Chinese, Latin, Hebrew, Greek, Cyrillic, and many 
        /// other alphabets. 
        /// However, because the UTF-8 portion of the PKWare specification is not broadly
        /// supported by other zip libraries and utilities, such zip files may not
        /// be readable by your favorite zip tool or archiver. In other words, interoperability
        /// will suffer if you set this flag to true. 
        /// </para>
        /// <para>
        /// In particular, Zip files created with strict adherence to the PKWare 
        /// specification with respect to UTF-8 encoding will not work well with 
        /// Explorer in Windows XP or Windows Vista, because Vista compressed folders 
        /// do not support UTF-8 in zip files.  Vista can read the zip files, but shows
        /// the filenames incorrectly.  Unpacking from Windows Vista Explorer will result in filenames
        /// that have rubbish characters in place of the high-order UTF-8 bytes.
        /// </para>
        /// <para>
        /// Also, zip files that use UTF-8 encoding will not work well 
        /// with Java applications that use the java.util.zip classes, as of 
        /// v5.0 of the Java runtime. The Java runtime does not correctly 
        /// implement the PKWare specification in this regard.
        /// </para>
        /// <para>
        /// As a result, we have the curious situation that "correct" 
        /// behavior by the DotNetZip library during zip creation will result 
        /// in zip files that are not able to be read by various other tools.
        /// </para>
        /// <para>
        /// The DotNetZip library can read and write zip files 
        /// with UTF8-encoded entries, according to the PKware spec.  If you use DotNetZip for both 
        /// creating and reading the zip file, and you use UTF-8, there will be no loss of information 
        /// in the filenames. For example, using a self-extractor created by this
        /// library will allow you to unpack files correctly with no loss of 
        /// information in the filenames. 
        /// </para>
        /// <para>
        /// Encoding filenames and comments using the IBM437 codepage, the default
        /// behavior, will cause loss of information on some filenames,
        /// but the resulting zipfile will
        /// be more interoperable with other utilities. As an example of the 
        /// loss of information, the o-tilde character will be down-coded to plain o. 
        /// Likewise, the O with a stroke through it, used in Danish and Norwegian,
        /// will be down-coded to plain o. Chinese characters cannot be represented
        /// in codepage IBM437; when using the default encoding, Chinese characters in 
        /// filenames will be represented as ?.  
        /// </para>
        /// <para>
        /// The loss of information associated to the use of the IBM437 encoding can lead to
        /// runtime errors. For example, using IBM437, any sequence of 4 Chinese characters will
        /// be encoded as ????.  If your application creates a ZipFile, then adds two files, each
        /// with names of four Chinese characters each, this will result in a duplicate filename
        /// exception.  In the case where you add a single file with a name containing four
        /// Chinese characters, calling Extract() on the entry that has question marks in the
        /// filename will result in an exception, because the question mark is not legal for use
        /// within filenames on Windows.  These are just a few examples of the problems associated
        /// to loss of information.
        /// </para>
        /// <para>
        /// This flag is independent of the encoding of the content within the 
        /// entries in the zip file.  
        /// </para>
        /// <para>
        /// Rather than specify the encoding in a binary fashion using this flag, an application
        /// can specify an arbitrary encoding via the <c>Encoding</c>property.  Setting 
        /// the encoding explicitly when creating zip archives will result in non-compliant 
        /// zip files that, curiously, are fairly interoperable.  The challenge is, the PKWare specification
        /// does not provide for a way to specify that an entry in a zip archive uses a code page that is
	/// neither IBM437 nor UTF-8.   Therefore 
        /// if you set the encoding explicitly when creating a zip archive, you must take care upon 
        /// reading the zip archive to use the same code page.  If you get it wrong, the behavior is 
        /// undefined and may result in incorrect filenames, exceptions, and acne.  
        /// </para>
        /// <seealso cref="Encoding">Encoding</seealso>
        /// </remarks>
        public bool UseUnicode
        {
            get
            {
                return _encoding == System.Text.Encoding.GetEncoding("UTF-8");
            }
            set
            {
                _encoding = (value) ? System.Text.Encoding.GetEncoding("UTF-8") : DefaultEncoding;
            }
        }

        /// <summary>
        /// Set the text encoding to use when writing new entries to the ZipFile, or get the text encoding
	/// that was used when reading the entries from the ZipFile. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// In its AppNote.txt document, PKWare describes how to specify in the zip entry header
        /// that a filename or comment containing non-ANSI characters is encoded with UTF-8.  But, some 
        /// archivers do not follow the specification, and instead encode super-ANSI characters using the 
        /// system default code page.  For example, WinRAR when run on a machine in Shanghai may encode 
        /// filenames with the Big-5 Chinese (950) code page.  This behavior is contrary to the Zip specification, but it 
        /// occurs anyway.
        /// </para>
        /// <para>
        /// When using DotNetZip to write zip archives that will be read by one of these other archivers, 
        /// set this property to specify the code page to use when encoding filenames and comments into the zip file.
        /// </para>
        /// <para>
        /// The default encoding is codepage IBM437.  Be aware that a zip file created after you've explicitly 
        /// specified the code page will not be compliant to the PKWare specification, and may not be readable 
        /// by compliant archivers.  On the other hand, many (most?) archivers are non-compliant and can read zip files 
        /// created in arbitrary code pages.  The trick is to use the proper codepage when reading the zip.
        /// </para>
        /// <para>
        /// When using DotNetZip to read zip archives that have been created by a different tool or library, and the zip
        /// archive uses an arbitrary code page, you must specify the encoding to use before or when the zipfile is READ.  
        /// This means you must use a ZipFile.Read() method that allows you to specify a System.Text.Encoding parameter. 
	/// Setting the Encoding property after your application has read in the zip archive will not 
	/// affect the entry names of entries that have already been read in, and is probably not what you want.  
        /// </para>
        /// <para>
        /// When using an arbitrary, non-UTF8 code page for encoding, there is no standard way for the 
        /// creator application - whether DotNetZip, WinRar, or something else - to specify in the zip file 
        /// which codepage has been used. As a result, readers of zip files are not
        /// able to inspect the zip file and determine the codepage that was used for the entries contained within it. 
        /// It is left to the application to determine the necessary codepage when reading zipfiles encoded this way.  
        /// If you use an incorrect codepage when reading a zipfile, you will get entries with filenames
        /// that are incorrect, and they may even contain characters that are not legal for use within filenames in
        ///  Windows. Extracting entries with illegal characters in the filenames will lead to exceptions. Caveat Emptor.
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.DefaultEncoding">DefaultEncoding</seealso>.
        public System.Text.Encoding Encoding
        {
            get
            {
                return _encoding;
            }
            set
            {
                _encoding = value;
            }
        }

        /// <summary>
        /// The default text encoding used in zip archives.  It is numeric 437, also known as IBM437. 
        /// </summary>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.Encoding">Encoding</seealso>

        public readonly static System.Text.Encoding DefaultEncoding = System.Text.Encoding.GetEncoding("IBM437");


        /// <summary>
        /// Gets or sets the <c>TextWriter</c> to which status messages are delivered 
        /// for the instance. If the TextWriter is set to a non-null value, then 
        /// verbose output is sent to the <c>TextWriter</c> during <c>Add</c><c>, Read</c><c>, Save</c> and
        /// <c>Extract</c> operations.  
        /// </summary>
        ///
        /// <example>
        /// <para>
        /// In this example, a console application instantiates a ZipFile, then sets
        /// the StatusMessageTextWriter to Console.Out.  At that point, all verbose
        /// status messages for that ZipFile are sent to the console. 
        /// </para>
        ///
        /// <code lang="C#">
        /// using (ZipFile zip= new ZipFile(FilePath))
        /// {
        ///   zip.StatusMessageTextWriter= System.Console.Out;
        ///   // messages are sent to the console during extraction
        ///   zip.ExtractAll();
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As new ZipFile(FilePath)
        ///   zip.StatusMessageTextWriter= System.Console.Out
        ///   'Status Messages will be sent to the console during extraction
        ///   zip.ExtractAll()
        /// End Using
        /// </code>
        /// </example>
        public System.IO.TextWriter StatusMessageTextWriter
        {
            get { return _StatusMessageTextWriter; }
            set { _StatusMessageTextWriter = value; }
        }

        /// <summary>
        /// Gets or sets the flag that indicates whether the ZipFile should use
        /// compression for subsequently added entries in the ZipFile instance.
        /// </summary>
        ///
        /// <remarks>
        /// <para> 
        /// When saving an entry into a zip archive, the DotNetZip first compresses
        /// the file, then compares the size of the pre-compressed data with the size of the
        /// post-compressed data. For files that are already compressed, like MP3's or JPGs,
        /// the deflate algorithm can actually expand the size of the data.  In this case,
        /// the DotNetZip library uses the pre-compressed data and simply stores the file
        /// data into the archive. 
        /// </para> 
        ///
        /// <para>
        /// The DotNetZip library does this optimization automatically.  To avoid the
        /// unnecessary processing and comparison, the application can explicitly request
        /// that Compression not be used, by setting this flag.  The default value is false.
        /// </para> 
        ///
        /// <para>
        /// Do not construe setting this flag to false as "Force Compression".  Setting it
        /// to false merely does NOT force No compression.  Think about it a little bit:
        /// There's a difference.  If you want to force the use fo deflate algorithm when
        /// storing each entry into the zip archive, define a <see
        /// cref="WillReadTwiceOnInflation"/> callback, which always returns false.  This is
        /// probably the wrong thing to do, but you could do it.  Forcing the use of the
        /// Deflate algorithm when storing an entry does not guarantee that the data size
        /// will get smaller. It could increase, as described above.  But if you want to be
        /// pig-headed about it, go ahead.
        /// </para>
        ///
        /// <para>
        /// Changes to this flag apply to all entries subsequently added to the archive. 
        /// The application can also set the CompressionMethod
        /// property on each ZipEntry, for more granular control of this capability.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.CompressionMethod"/>
        ///
        /// <example>
        /// This example shows how to specify that Compression will not be used when adding files 
        /// to the zip archive. None of the files added to the archive in this way will use
        /// compression.
        /// <code>
        /// using (ZipFile zip = new ZipFile(ZipFileToCreate))
        /// {
        ///   zip.ForceNoCompression = true;
        ///   zip.AddDirectory(@"c:\temp\Foo");
        ///   zip.Comment = "All files in this archive will be uncompressed.";
        ///   zip.Save();
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As New ZipFile(ZipFileToCreate)
        ///   zip.ForceNoCompression = true
        ///   zip.AddDirectory("c:\temp\Foo")
        ///   zip.Comment = "All files in this archive will be uncompressed."
        ///   zip.Save()
        /// End Using
        /// </code>
        ///
        /// </example>
        public bool ForceNoCompression
        {
            get { return _ForceNoCompression; }
            set { _ForceNoCompression = value; }
        }


        /// <summary>
        /// Gets or sets the name for the folder to store the temporary file
        /// this library writes when saving the zip archive. 
        /// </summary>
        ///
        /// <remarks>
        /// The calling application should have write and delete rights on that folder.  By
        /// default, the temp file folder is the directory referred to by the TEMP
        /// environment variable.  If that variable is not set, this value defaults to the
        /// current working directory.  But for some scenarios, such as ASP.NET
        /// applications, the application may wish to explicitly override this,
        /// with this public property. This property is used only when calling one of the
        /// <c>Save()</c> methods, or the <c>SaveSelfExtractor()</c> method.
        /// </remarks>
        ///
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown upon setting the property if the directory does not exist. 
        /// </exception>
        ///
        public String TempFileFolder
        {
            get
            {
                // first time through only (default value)
                if (_TempFileFolder == null)
                {
                    if (Environment.GetEnvironmentVariable("TEMP") != null)
                        _TempFileFolder = Environment.GetEnvironmentVariable("TEMP");
                    else
                        _TempFileFolder = ".";
                }
                return _TempFileFolder;
            }

            set
            {
                if (value == null)
                    throw new ArgumentException("You may not set the TempFileFolder to a null value.");


                if (!System.IO.Directory.Exists(value))
                    throw new System.IO.FileNotFoundException(String.Format("That directory ({0}) does not exist.", value));

                _TempFileFolder = value;
            }
        }

        /// <summary>
        /// Sets the password to be used for any entry subsequently added 
        /// to the ZipFile, using one of the AddFile, AddDirectory, or AddItem methods; 
        /// or any entry subsequently extracted from the ZipFile using one of the Extract methods on the ZipFile class.  
        /// This password is applied to the entries, not
        /// to the zip archive itself. 
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        /// Though the password is set on the ZipFile object, the password
        /// actually does not apply to the archive as a whole.  Instead, it
        /// applies to individual entries that are added to the archive, from
        /// that point in time onward.  The "directory" of the archive - in other
        /// words the list of files - is not encrypted with the password, The
        /// list of filenames in the archive is in clear text.  but the contents
        /// of the individual files are encrypted.
        /// </para>
        /// 
        /// <para>
        /// If you set the password on the zip archive, and then add a set of files to the 
        /// archive, then each entry is encrypted with that password.  You may also want 
        /// to change the password between adding different entries. If you set the 
        /// password, add an entry, then set the password to null, and add another entry,
        /// the first entry is encrypted and the second is not.  Furshtay?
        /// </para>
        /// 
        /// <para>
        /// If you read in an existing ZipFile, then set the Password property, then call
        /// one of the ZipFile.Extract() overloads, the entry is extracted using the Password that is 
        /// specified on the ZipFile instance. If you have not set the Password property, then
        /// the password is null, and the entry is extracted with no password.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <example>
        /// <para>
        /// In this example, three files are added to a Zip archive. The ReadMe.txt file
        /// will be placed in the root of the archive, and will not be encrypted. 
        /// The .png file will be included into the zip, encrypted with the "123456!" password.
        /// The pdf file will be included, encrypted with "!Secret1" as the password.
        /// </para>
        /// <code>
        ///    try
        ///    {
        ///      using (ZipFile zip = new ZipFile("test3.zip",System.Console.Out))
        ///      {
        ///        zip.AddFile("ReadMe.txt");
        ///        zip.Password= "123456!";
        ///        zip.AddFile("7440-N49th.png");
        ///        zip.Password= "!Secret1";
        ///        zip.AddFile("2005_Annual_Report.pdf");
        ///
        ///        zip.Save();
        ///      }
        ///    }
        ///    catch (System.Exception ex1)
        ///    {
        ///      System.Console.Error.WriteLine("exception: {0}", ex1);
        ///    }
        /// </code>
        ///
        /// <code lang="VB">
        ///  Try 
        ///    Using zip As New ZipFile("test2.zip", System.Console.Out)
        ///      zip.AddFile("c:\datafiles\ReadMe.txt", "")
        ///      zip.Password = "123456!"
        ///      zip.AddFile("c:\photos\personal\7440-N49th.png", "images")
        ///      zip.Password= "!Secret1";
        ///      zip.AddFile("c:\Desktop\2005_Annual_Report.pdf", "files\documents")
        ///      zip.Save
        ///    End Using
        ///  Catch ex1 As System.Exception
        ///    System.Console.Error.WriteLine("exception: {0}", ex1)
        ///  End Try
        /// </code>
        ///
        /// </example>
        /// 
        public String Password
        {
            set
            {
                _Password = value;
            }
        }



        /// <summary>
        /// A callback that allows the application to specify whether multiple reads of the
        /// stream should be performed, in the case that a compression operation actually
        /// inflates the size of the file data.  
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// In some cases, applying the Deflate compression algorithm in DeflateStream can
        /// result an increase in the size of the data.  This "inflation" can happen with
        /// previously compressed files, such as a zip, jpg, png, mp3, and so on.  In a few
        /// tests, inflation on zip files can be as large as 60%!  Inflation can also happen
        /// with very small files.  In these cases, by default, the DotNetZip library
        /// discards the compressed bytes, and stores the uncompressed file data into the
        /// zip archive.  This is an optimization where smaller size is preferred over
        /// longer run times.
        /// </para>
        ///
        /// <para>
        /// The application can specify that compression is not even tried, by setting the
        /// ForceNoCompression flag.  In this case, the compress-and-check-sizes process as
        /// decribed above, is not done.
        /// </para>
        ///
        /// <para>
        /// In some cases, neither choice is optimal.  The application wants compression,
        /// but in some cases also wants to avoid reading the stream more than once.  This
        /// may happen when the stream is very large, or when the read is very expensive, or
        /// when the difference between the compressed and uncompressed sizes is not
        /// significant.
        /// </para>
        ///
        /// <para>
        /// To satisfy these applications, this delegate allows the DotNetZip library to ask
        /// the application to for approval for re-reading the stream, in the case where
        /// inflation occurs.  The callback is invoked only in the case of inflation; that
        /// is to say when the uncompressed stream is smaller than the compressed stream.
        /// </para>
        ///
        /// <para>
        /// As with other properties (like Password and ForceNoCompression), setting the
        /// corresponding delegate on the ZipFile class itself will set it on all ZipEntry
        /// items that are subsequently added to the ZipFile instance.
        /// </para>
        ///
        /// </remarks>
        /// <example>
        /// <para>
        /// In this example, the application callback checks to see if the difference
        /// between the compressed and uncompressed data is greater than 25%.  If it is,
        /// then the callback returns true, and the application tells the library to re-read
        /// the stream.  If not, then the callback returns false, and the library just keeps
        /// the "inflated" file data.
        /// </para>
        ///
        /// <code>
        ///
        /// public bool ReadTwiceCallback(int uncompressed, int compressed, string filename)
        /// {
        ///     return ((uncompressed * 1.0/compressed) > 1.25);
        /// }
        /// 
        /// public void CreateTheZip()
        /// {
        ///     using (ZipFile zip = new ZipFile())
        ///     {
        ///         zip2.WillReadTwiceOnInflation = ReadTwiceCallback;
        ///         zip2.AddFile(filename1);
        ///         zip2.AddFile(filename2);
        ///         zip2.Save(ZipFileToCreate);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="Ionic.Utils.Zip.ReReadApprovalCallback"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.WillReadTwiceOnInflation"/>
        public ReReadApprovalCallback WillReadTwiceOnInflation
        {
            get;
            set;
        }

        internal System.IO.Stream ReadStream
        {
            get
            {
                if (_readstream == null)

                    if (_name != null)
                    {
                        try
                        {
                            _readstream = System.IO.File.OpenRead(_name);
                            _ReadStreamIsOurs = true;
                        }
                        catch (System.IO.IOException ioe)
                        {
                            throw new ZipException("Error opening the file", ioe);
                        }
                    }

                return _readstream;
            }
            //set
            //{
            //    if (value != null)
            //        throw new ArgumentException("Cannot set ReadStream explicitly to a non-null value.", "ReadStream");
            //    _readstream = null;
            //}
        }



	// called by ZipEntry in ZipEntry.Extract(), when there is no stream set for the ZipEntry.
        internal void Reset()
        {
            if (_JustSaved)
            {
                // read in the just-saved zip archive
                ZipFile x = new ZipFile();
                x._name = this._name;
                x.Encoding = this.Encoding;
                ReadIntoInstance(x);
                // copy the contents of the entries.
                // cannot just replace the entries - the app may be holding them
                foreach (ZipEntry e1 in x)
                {
                    foreach (ZipEntry e2 in this)
                    {
                        if (e1.FileName == e2.FileName)
                        {
                            e2.CopyMetaData(e1);
                        }
                    }
                }
                _JustSaved = false;
            }
        }


        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new ZipFile instance, using the specified ZipFileName for the filename. 
        /// The ZipFileName may be fully qualified.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Applications can use this constructor to create a new ZipFile for writing, 
        /// or to slurp in an existing zip archive for read and write purposes.  
        /// </para>
        /// 
        /// <para>
        /// To create a new zip archive, an application should call this constructor,
        /// passing the name of a file that does not exist.  Then the application can
        /// add directories or files to the ZipFile via <c>AddDirectory()</c>,
        /// <c>AddFile()</c>, <c>AddItem()</c> and then write the zip archive to the
        /// disk by calling <c>Save()</c>. The zip file is not actually written to
        /// the disk until the application calls <c>ZipFile.Save()</c>.  At that point
        /// the new zip file with the given name is created. 
        /// </para>
        /// 
        /// <para>
        /// To read an existing zip archive, the application should call this constructor,
        /// passing the name of a valid zip file that does exist.  The file is then read into
        /// the <c>ZipFile</c> instance.  The app can then enumerate the entries or can modify
        /// the zip file, for example adding entries, removing entries, changing comments, and
        /// so on.  When reading an existing zip archive, the application may wish to
        /// explicitly specify that it is reading an existing zip file by using
        /// <c>ZipFile.Read()</c>.  On the other hand, this parameterized constructor allows
        /// applications to use the same code to add items to a zip archive, regardless of
        /// whether the zip file exists.
        /// </para>
        /// 
        /// <para>
        /// To encrypt the data for the  files added to the ZipFile instance, set the Password
        /// property after creating the ZipFile instance.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <exception cref="Ionic.Utils.Zip.ZipException">
        /// Thrown if zipFileName refers to an existing file that is not a valid zip file. 
        /// </exception>
        ///
        /// <example>
        /// This example shows how to create a zipfile, and add a few files into it. 
        /// <code>
        /// using (ZipFile zip = new ZipFile(args[0]))
        /// { 
        ///   // Store all files found in the top level directory, into the zip archive.
        ///   String[] filenames = System.IO.Directory.GetFiles(args[1]);
        ///   foreach (String filename in filenames)
        ///   {
        ///     Console.WriteLine("Adding {0}...", filename);
        ///     zip.AddFile(filename);
        ///   }  
        ///   zip.Save();
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Dim ZipToCreate As String = "ex1.zip"
        /// Dim DirectoryToZip As String = "c:\temp"
        /// Using zip As ZipFile = New ZipFile(ZipToCreate)
        ///     Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        ///     Dim filename As String
        ///     For Each filename In filenames
        ///         Console.WriteLine("Adding {0}...", filename)
        ///         zip.AddFile(filename)
        ///     Next
        ///     zip.Save
        /// End Using
        /// </code>
        /// </example>
        ///
        /// <param name="zipFileName">The filename to use for the new zip archive.</param>
        ///
        public ZipFile(string zipFileName)
        {
            try
            {
                InitFile(zipFileName, null);
            }
            catch (Exception e1)
            {
                throw new ZipException(String.Format("{0} is not a valid zip file", zipFileName), e1);
            }
        }


        /// <summary>
        /// Create a zip file, without specifying a target filename or stream to save to. 
        /// </summary>
        /// 
        /// <remarks>
        /// If you do eventually call <c>Save()</c>, you will need to have specified
        /// a zip filename at some point. Either as a parameter to <c>Save()</c> or 
        /// on the ZipFile object itself.
        /// </remarks>
        /// 
        /// <example>
        /// This example creates a Zip archive called Backup.zip, containing all the files
        /// in the directory DirectoryToZip. Files within subdirectories are not zipped up.
        /// <code>
        /// using (ZipFile zip = new ZipFile())
        /// { 
        ///   // Store all files found in the top level directory, into the zip archive.
        ///   // note: this code does not recurse subdirectories!
        ///   String[] filenames = System.IO.Directory.GetFiles(DirectoryToZip);
        ///   foreach (String filename in filenames)
        ///   {
        ///     Console.WriteLine("Adding {0}...", filename);
        ///     zip.AddFile(filename);
        ///   }  
        ///   zip.Save("Backup.zip");
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As New ZipFile
        ///     ' Store all files found in the top level directory, into the zip archive.
        ///     ' note: this code does not recurse subdirectories!
        ///     Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        ///     Dim filename As String
        ///     For Each filename In filenames
        ///         Console.WriteLine("Adding {0}...", filename)
        ///         zip.AddFile(filename)
        ///     Next
        ///     zip.Save("Backup.zip")
        /// End Using
        /// </code>
        /// </example>
        public ZipFile()
        {
            InitFile(null, null);
        }


        /// <summary>
        /// Creates a new ZipFile instance, using the specified ZipFileName for the filename. 
        /// The ZipFileName may be fully qualified.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// Applications can use this constructor to create a new ZipFile for writing, 
        /// or to slurp in an existing zip archive for read and write purposes.  
        /// </para>
        ///
        /// <para>
        /// To create a new zip archive, an application should call this constructor,
        /// passing the name of a file that does not exist.  Then the application can
        /// add directories or files to the ZipFile via <c>AddDirectory()</c>,
        /// <c>AddFile()</c>, <c>AddItem()</c> and then write the zip archive to the
        /// disk by calling <c>Save()</c>. The zip file is not actually written to
        /// the disk until the application calls <c>ZipFile.Save()</c>.  At that point
        /// the new zip file with the given name is created. 
        /// </para>
        /// 
        /// <para>
        /// To read an existing zip archive, the application should call this constructor,
        /// passing the name of a valid zip file that does exist.  The file is then read into
        /// the <c>ZipFile</c> instance.  The app can then enumerate the entries or can modify
        /// the zip file, for example adding entries, removing entries, changing comments, and
        /// so on.  When reading an existing zip archive, the application may wish to
        /// explicitly specify that it is reading an existing zip file by using
        /// <c>ZipFile.Read()</c>.  On the other hand, this parameterized constructor allows
        /// applications to use the same code to add items to a zip archive, regardless of
        /// whether the zip file exists.
        /// </para>
        ///
        /// <para>
        /// This version of the constructor allows the caller to pass in a TextWriter, to which
        /// verbose messages will be written during extraction or creation of the zip archive.
        /// A console application may wish to pass System.Console.Out to get messages on the
        /// Console. A graphical or headless application may wish to capture the messages in a
        /// different <c>TextWriter</c>, for example, a <c>StringWriter</c>.
        /// </para>
        /// 
        /// <para>
        /// To encrypt the data for the  files added to the ZipFile instance, set the Password
        /// property after creating the ZipFile instance.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <exception cref="Ionic.Utils.Zip.ZipException">
        /// Thrown if zipFileName refers to an existing file that is not a valid zip file. 
        /// </exception>
        ///
        /// <example>
        /// <code>
        /// using (ZipFile zip = new ZipFile("Backup.zip", System.Console.Out))
        /// { 
        ///   // Store all files found in the top level directory, into the zip archive.
        ///   // note: this code does not recurse subdirectories!
        ///   String[] filenames = System.IO.Directory.GetFiles(DirectoryToZip);
        ///   foreach (String filename in filenames)
        ///   {
        ///     Console.WriteLine("Adding {0}...", filename);
        ///     zip.AddFile(filename);
        ///   }  
        ///   zip.Save();
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        /// Using zip As New ZipFile("Backup.zip", System.Console.Out)
        ///     ' Store all files found in the top level directory, into the zip archive.
        ///     ' note: this code does not recurse subdirectories!
        ///     Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
        ///     Dim filename As String
        ///     For Each filename In filenames
        ///         Console.WriteLine("Adding {0}...", filename)
        ///         zip.AddFile(filename)
        ///     Next
        ///     zip.Save
        /// End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="zipFileName">The filename to use for the new zip archive.</param>
        /// <param name="statusMessageWriter">A TextWriter to use for writing verbose status messages.</param>
        public ZipFile(string zipFileName, System.IO.TextWriter statusMessageWriter)
        {
            try
            {
                InitFile(zipFileName, statusMessageWriter);
            }
            catch (Exception e1)
            {
                throw new ZipException(String.Format("{0} is not a valid zip file", zipFileName), e1);
            }
        }


        /// <summary>
        /// Constructor to create an instance of ZipFile that writes Zip archives to a <c>System.IO.Stream</c>.
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// Applications can use this constructor to create an instance of ZipFile 
        /// for writing to a stream. This is useful when zipping up content, but for any 
        /// reason it is not desirable to create a zip file in the filesystem itself. 
        /// </para>
        ///
        /// <para>
        /// Typically an application writing a zip archive in this manner will create and
        /// open a stream, then call this constructor, passing in the stream.  Then the app will add 
        /// directories or files to the ZipFile via <c>AddDirectory</c> or <c>AddFile</c> or <c>AddItem</c>.  The app
        /// will then write the zip archive to the memory stream by calling <c>Save()</c>. 
        /// </para>
        ///
        /// <para>
        /// The compressed (zipped) data is not actually written to the stream until the
        /// application calls <c>ZipFile.Save</c>.  This means the file data must be
        /// available at the time the <c>Save</c> method is invoked. 
        /// </para>
        ///
        /// <para>
        /// When using a filesystem file for the Zip output, it is possible to call
        /// <c>Save</c> multiple times on the ZipFile instance. With each call the zip content
        /// is written to the output file. When saving to a <c>Stream</c>, as you would do when you use this constructor, after the initial
        /// call to <c>Save</c>, additional calls to <c>Save</c> will throw. This is because the
        /// stream is assumed to be a write-only stream, and after the initial <c>Save</c>, it
        /// is not possible to seek backwards and "unwrite" the zip file data.
        /// </para>
        ///
        /// <para>
        /// Calling Save() on a ZipFile that has been created with this constructor will not
        /// result in the creation of a temporary zipfile in the filesystem.  
        /// </para>
        ///
        /// </remarks>
        /// 
        /// <exception cref="System.ArgumentException">
        /// Thrown if the stream is not writable.  Seriously, think about it, dude. 
        /// You need a writable stream if you're going to extract zip content to it. 
        /// </exception>
        ///
        /// <example>
        /// <code>
        ///    byte[] buffer= new byte[100000]; // 100k max
        ///    System.IO.MemoryStream ms= new System.IO.MemoryStream(buffer);
        ///
        ///    // write to the memory stream:
        ///    try
        ///    {
        ///      using (ZipFile zip = new ZipFile(ms,System.Console.Out))
        ///      {
        ///        zip.Comment= "This is the comment attached to the archive.";
        ///        zip.AddItem(directoryName);  // and recurse
        ///        zip.Save(); // write all the data to the stream and close it
        ///      }
        ///    }
        ///    catch (System.Exception ex1)
        ///    {
        ///      System.Console.Error.WriteLine("exception: " + ex1);
        ///    }
        /// </code>
        /// <code lang="VB">
        ///   Dim buffer As Byte() = New Byte(100000) {}
        ///   Dim ms As New System.IO.MemoryStream(buffer)
        ///   ' write to the memory stream:
        ///   Try 
        ///       Using zip As ZipFile = New ZipFile(ms, Console.Out)
        ///           zip.Comment = "This is the comment attached to the archive."
        ///           ' Add directory and recurse
        ///           zip.AddItem(NameOfDirectoryToZip)
        ///           ' now, write all the data to the stream and close it:
        ///           zip.Save
        ///       End Using
        ///   Catch ex1 As Exception
        ///       Console.Error.WriteLine("exception: {0}", ex1.ToString())
        ///   End Try
        /// </code>
        /// </example>
        ///
        /// <param name="outputStream">The <c>System.IO.Stream</c> to write to. It must be writable.</param>
        public ZipFile(System.IO.Stream outputStream)
        {
            if (!outputStream.CanWrite)
                throw new ArgumentException("The outputStream must be a writable stream.");

            // At various times during writing of the archive, we retrieve the position in the 
            // stream.  But, the Response.OutputStream in an ASP.NET page doesn't allow this.
            // So, we wrap the stream with a counting stream, so that we can retrieve the count
            // of bytes written at any particular moment. 

            _writestream = new CountingStream(outputStream);
            _entries = new System.Collections.Generic.List<ZipEntry>();
        }

        /// <summary>
        /// Constructor to create an instance of ZipFile that writes Zip archives to a stream.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>Applications can use this constructor to create an instance of ZipFile 
        /// for writing to a stream. This is useful when zipping up content, but for any 
        /// reason it is not desirable to create a zip file in the filesystem itself. 
        /// </para>
        /// <para>Typically an application writing a zip archive in this manner will create and
        /// open a stream, then call this constructor, passing in the stream.  Then the app will 
        /// add directories or files to the ZipFile via AddDirectory or AddFile or AddItem.  The 
        /// app will then write the zip archive to the memory stream by calling <c>Save</c>. The 
        /// compressed (zipped) data is not actually written to the stream until the application 
        /// calls <c>ZipFile.Save</c> .
        /// </para>
        /// <para>
        /// This version of the constructor allows the caller to pass in a TextWriter, to which  
        /// verbose messages will be written during creation of the zip archive.  A console 
        /// application may wish to pass System.Console.Out to get messages on the Console. 
        /// A graphical or headless application may wish to capture the messages in a different 
        /// TextWriter. 
        /// </para>
        /// </remarks>
        ///
        /// <exception cref="System.ArgumentException">
        /// Thrown if the stream is not writable. 
        /// You need to specify a writable stream if you're going to extract zip content to it. 
        /// </exception>
        ///
        /// <param name="outputStream">The outputStream to write to. It must be writable.</param>
        /// <param name="statusMessageWriter">A TextWriter to use for writing verbose status messages.</param>
        public ZipFile(System.IO.Stream outputStream, System.IO.TextWriter statusMessageWriter)
        {

            if (!outputStream.CanWrite)
                throw new ArgumentException("The outputStream must be a writable stream.");

            // At various times during writing of the archive, we retrieve the position in the 
            // stream.  But, the Response.OutputStream in an ASP.NET page doesn't allow this.
            // So, we wrap the stream with a counting stream, so that we can retrieve the count
            // of bytes written at any particular moment. 

            _writestream = new CountingStream(outputStream);
            _entries = new System.Collections.Generic.List<ZipEntry>();
            _StatusMessageTextWriter = statusMessageWriter;
        }


	/// <summary>
	/// Returns the version number on the assembly.
	/// </summary>
	/// <remarks>
	/// This property is exposed as a convenience.  Callers
	/// could also get the version value by retrieving  GetName().Version 
	/// on the System.Reflection.Assembly object pointing to the
	/// DotNetZip assembly. But sometimes it is not clear which
	/// assembly is being loaded.  This property makes it clear. 
	/// </remarks>
	public static System.Version LibraryVersion
	{
	    get 
	    {
		return
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
	    }
	}

        private void InitFile(string zipFileName, System.IO.TextWriter statusMessageWriter)
        {
            // create a new zipfile
            _name = zipFileName;
            _StatusMessageTextWriter = statusMessageWriter;
            _contentsChanged = true;

            if (System.IO.File.Exists(_name))
            {
                ReadIntoInstance(this);
                this._fileAlreadyExists = true;
            }
            else
                _entries = new System.Collections.Generic.List<ZipEntry>();
            return;
        }
        #endregion

        #region Adding Entries

        /// <summary>
        /// Adds an item, either a file or a directory, to a zip file archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// If adding a directory, the add is recursive on all files and subdirectories 
        /// contained within it. 
        /// </para>
        /// <para>
        /// The name of the item may be a relative path or a fully-qualified path.
        /// The item added by this call to the ZipFile is not written to the zip file
        /// archive until the application calls Save() on the ZipFile. 
        /// </para>
        /// <para>
        /// The directory name used for the file within the archive is the same as
        /// the directory name (potentially a relative path) specified in the FileOrDirectoryName.
        /// </para>
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddFile(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddDirectory(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateItem(string)"/>
        ///
        /// <overloads>This method has two overloads.</overloads>
        /// <param name="fileOrDirectoryName">the name of the file or directory to add.</param>
        /// 
        public void AddItem(string fileOrDirectoryName)
        {
            AddItem(fileOrDirectoryName, null);
        }


        /// <summary>
        /// Adds an item, either a file or a directory, to a zip file archive, 
        /// explicitly specifying the directory path to be used in the archive. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// If adding a directory, the add is recursive on all files and subdirectories 
        /// contained within it. 
        /// </para>
        /// <para>
        /// The name of the item may be a relative path or a fully-qualified path.
        /// The item added by this call to the ZipFile is not written to the zip file
        /// archive until the application calls Save() on the ZipFile. 
        /// </para>
        /// 
        /// <para>
        /// This version of the method allows the caller to explicitly specify the 
        /// directory path to be used in the archive, which would override the 
        /// "natural" path of the filesystem file.
        /// </para>
        /// 
        /// <para>
        /// Encryption will be used on the file data if the Password
        /// has been set on the ZipFile object, prior to calling this method.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown if the file or directory passed in does not exist. 
        /// </exception>
        ///
        /// <param name="fileOrDirectoryName">the name of the file or directory to add.</param>
        /// <param name="directoryPathInArchive">
        /// The name of the directory path to use within the zip archive. 
        /// This path need not refer to an extant directory in the current filesystem.
        /// If the files within the zip are later extracted, this is the path used for the extracted file. 
        /// Passing null (nothing in VB)) will use the path on the FileOrDirectoryName.  Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddFile(string, string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddDirectory(string, string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateItem(string, string)"/>
        ///
        /// <example>
        /// This example shows how to zip up a set of files into a flat hierarchy, 
        /// regardless of where in the filesystem the files originated.
        /// <code>
        /// String[] itemnames= { 
        ///   "c:\\temp\\Readme.txt",
        ///   "MyProposal.docx",
        ///   "SupportFiles",  // a directory
        ///   "images\\Image1.jpg"
        /// };
        ///
        /// try
        /// {
        ///   using (ZipFile zip = new ZipFile(ZipToCreate,System.Console.Out))
        ///   {
        ///     for (int i = 1; i &lt; itemnames.Length; i++)
        ///     {
        ///       // will add Files or Dirs, recurses and flattens subdirectories
        ///       zip.AddItem(itemnames[i],"flat"); 
        ///     }
        ///     zip.Save();
        ///   }
        /// }
        /// catch (System.Exception ex1)
        /// {
        ///   System.Console.Error.WriteLine("exception: {0}", ex1);
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        ///   Dim itemnames As String() = _
        ///     New String() { "c:\temp\Readme.txt", _
        ///                    "MyProposal.docx", _
        ///                    "SupportFiles", _
        ///                    "images\Image1.jpg" }
        ///   Try 
        ///       Using zip As New ZipFile(ZipToCreate, Console.Out)
        ///           Dim i As Integer
        ///           For i = 1 To itemnames.Length - 1
        ///               ' will add Files or Dirs, recursing and flattening subdirectories.
        ///               zip.AddItem(itemnames(i), "flat")
        ///           Next i
        ///           zip.Save
        ///       End Using
        ///   Catch ex1 As Exception
        ///       Console.Error.WriteLine("exception: {0}", ex1.ToString())
        ///   End Try
        /// </code>
        /// </example>
        public void AddItem(String fileOrDirectoryName, String directoryPathInArchive)
        {
            if (System.IO.File.Exists(fileOrDirectoryName))
                AddFile(fileOrDirectoryName, directoryPathInArchive);
            else if (System.IO.Directory.Exists(fileOrDirectoryName))
                AddDirectory(fileOrDirectoryName, directoryPathInArchive);

            else
                throw new System.IO.FileNotFoundException(String.Format("That file or directory ({0}) does not exist!", fileOrDirectoryName));
        }

        /// <summary>
        /// Adds a File to a Zip file archive. 
        /// </summary>
        /// <remarks>
        /// The file added by this call to the ZipFile is not written to the zip file
        /// archive until the application calls Save() on the ZipFile. 
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// In this example, three files are added to a Zip archive. The ReadMe.txt file
        /// will be placed in the root of the archive. The .png file will be placed in a folder 
        /// within the zip called photos\personal.  The pdf file will be included into a
        /// folder within the zip called Desktop.
        /// </para>
        /// <code>
        ///    try
        ///    {
        ///      using (ZipFile zip = new ZipFile("test2.zip",System.Console.Out))
        ///      {
        ///        zip.AddFile("c:\\photos\\personal\\7440-N49th.png");
        ///        zip.AddFile("c:\\Desktop\\2005_Annual_Report.pdf");
        ///        zip.AddFile("ReadMe.txt");
        ///
        ///        zip.Save();
        ///      }
        ///    }
        ///    catch (System.Exception ex1)
        ///    {
        ///      System.Console.Error.WriteLine("exception: " + ex1);
        ///    }
        /// </code>
        /// 
        /// <code lang="VB">
        ///  Try 
        ///       Using zip As ZipFile = New ZipFile("test2.zip", Console.Out)
        ///           zip.AddFile("c:\photos\personal\7440-N49th.png")
        ///           zip.AddFile("c:\Desktop\2005_Annual_Report.pdf")
        ///           zip.AddFile("ReadMe.txt")
        ///           zip.Save
        ///       End Using
        ///   Catch ex1 As Exception
        ///       Console.Error.WriteLine("exception: {0}", ex1.ToString)
        ///   End Try
        /// </code>
        /// </example>
        /// 
        /// <overloads>This method has two overloads.</overloads>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddItem(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddDirectory(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateFile(string)"/>
        ///
        /// <param name="fileName">
        /// The name of the file to add. It should refer to a file in the filesystem.  
        /// The name of the file may be a relative path or a fully-qualified path. 
        /// </param>
        /// <returns>The ZipEntry corresponding to the File added.</returns>
        public ZipEntry AddFile(string fileName)
        {
            return AddFile(fileName, null);
        }



        /// <summary>
        /// Adds a File to a Zip file archive, potentially overriding the path to be used
        /// within the zip archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The file added by this call to the ZipFile is not written to the zip file
        /// archive until the application calls Save() on the ZipFile. 
        /// </para>
        /// 
        /// <para>
        /// This version of the method allows the caller to explicitly specify the 
        /// directory path to be used in the archive. 
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// In this example, three files are added to a Zip archive. The ReadMe.txt file
        /// will be placed in the root of the archive. The .png file will be placed in a folder 
        /// within the zip called images.  The pdf file will be included into a
        /// folder within the zip called files\documents, and will be encrypted with the 
        /// given password.
        /// </para>
        /// <code>
        /// try
        /// {
        ///   using (ZipFile zip = new ZipFile("Archive.zip",System.Console.Out))
        ///   {
        ///     zip.AddFile("c:\\datafiles\\ReadMe.txt", "");
        ///     zip.AddFile("c:\\photos\\personal\\7440-N49th.png", "images");
        ///     zip.Password = "EncryptMe!";
        ///     zip.AddFile("c:\\Desktop\\2005_Annual_Report.pdf", "files\\documents");
        ///     zip.Save();
        ///   }
        /// }
        /// catch (System.Exception ex1)
        /// {
        ///   System.Console.Error.WriteLine("exception: {0}", ex1);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        ///   Try 
        ///       Using zip As ZipFile = New ZipFile("Archive.zip", Console.Out)
        ///           zip.AddFile("c:\datafiles\ReadMe.txt", "")
        ///           zip.AddFile("c:\photos\personal\7440-N49th.png", "images")
        ///           zip.Password = "EncryptMe!"
        ///           zip.AddFile("c:\Desktop\2005_Annual_Report.pdf", "files\documents")
        ///           zip.Save
        ///       End Using
        ///   Catch ex1 As Exception
        ///       Console.Error.WriteLine("exception: {0}", ex1)
        ///   End Try
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddItem(string,string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddDirectory(string, string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateFile(string,string)"/>
        ///
        /// <param name="fileName">
        /// The name of the file to add.  The name of the file may be a relative path or 
        /// a fully-qualified path.
        /// </param>
        ///
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the FileName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the files within the zip are later extracted, this is the path used for the extracted file. 
        /// Passing null (nothing in VB) will use the path on the FileName, if any.  Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        ///
        /// <returns>The ZipEntry corresponding to the file added.</returns>
        public ZipEntry AddFile(string fileName, String directoryPathInArchive)
        {
            string nameInArchive = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
            ZipEntry ze = ZipEntry.Create(fileName, nameInArchive);
            ze.TrimVolumeFromFullyQualifiedPaths = TrimVolumeFromFullyQualifiedPaths;
            ze.ForceNoCompression = ForceNoCompression;
            ze.WillReadTwiceOnInflation = WillReadTwiceOnInflation;
            //ze.UseUtf8Encoding = UseUnicode;
            ze.Encoding = Encoding;
            ze._Source = EntrySource.Filesystem;
            ze.Password = _Password;
            if (Verbose) StatusMessageTextWriter.WriteLine("adding {0}...", fileName);
            InsureUniqueEntry(ze);
            _entries.Add(ze);
            _contentsChanged = true;
            return ze;
        }



        /// <summary>
        /// Adds or Updates a File in a Zip file archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method adds a file to a zip archive, or, if the file already exists in the zip archive, 
        /// this method Updates the content of that given filename in the zip archive.
        /// </para>
        ///
        /// <para>
        /// Upon success, there is no way for the application to learn whether the file was added or updated. 
        /// </para>
        ///
        /// </remarks>
        ///
        /// <example>
        /// This example shows how to Update an existing entry in a zipfile. The first call to 
        /// UpdateFile adds the file to the newly-created zip archive.  The second 
        /// call to UpdateFile updates the content for that file in the zip archive.
        /// <code>
        /// using (ZipFile zip1 = new ZipFile())
        /// {
        ///   // UpdateFile might more accurately be called "AddOrUpdateFile"
        ///   zip1.UpdateFile("MyDocuments\\Readme.txt", "");
        ///   zip1.UpdateFile("CustomerList.csv", "");
        ///   zip1.Comment = "This zip archive has been created.";
        ///   zip1.Save("Content.zip");
        /// }
        /// 
        /// using (ZipFile zip2 = ZipFile.Read("Content.zip"))
        /// {
        ///   zip2.UpdateFile("Updates\\Readme.txt", "");
        ///   zip2.Comment = "This zip archive has been updated: The Readme.txt file has been changed.";
        ///   zip2.Save();
        /// }
        ///
        /// </code>
        /// <code lang="VB">
        ///   Using zip1 As New ZipFile
        ///       ' UpdateFile might more accurately be called "AddOrUpdateFile"
        ///       zip1.UpdateFile("MyDocuments\Readme.txt", "")
        ///       zip1.UpdateFile("CustomerList.csv", "")
        ///       zip1.Comment = "This zip archive has been created."
        ///       zip1.Save("Content.zip")
        ///   End Using
        ///
        ///   Using zip2 As ZipFile = ZipFile.Read("Content.zip")
        ///       zip2.UpdateFile("Updates\Readme.txt", "")
        ///       zip2.Comment = "This zip archive has been updated: The Readme.txt file has been changed."
        ///       zip2.Save
        ///   End Using
        /// </code>
        /// </example>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddFile(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateDirectory(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateItem(string)"/>
        ///
        /// <param name="fileName">
        /// The name of the file to add or update. It should refer to a file in the filesystem.  
        /// The name of the file may be a relative path or a fully-qualified path. 
        /// </param>
        ///
        /// <returns>The ZipEntry corresponding to the File that was added or updated.</returns>
        public ZipEntry UpdateFile(string fileName)
        {
            return UpdateFile(fileName, null);
        }



        /// <summary>
        /// Adds or Updates a File in a Zip file archive.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method adds a file to a zip archive, or, if the file already exists in the zip archive, 
        /// this method Updates the content of that given filename in the zip archive.
        /// </para>
        /// 
        /// <para>
        /// This version of the method allows the caller to explicitly specify the 
        /// directory path to be used in the archive.  The entry to be added or updated is found by 
        /// using the specified directory path, combined with the basename of the specified 
        /// filename. 
        /// </para>
        /// 
        /// <para>
        /// Upon success, there is no way for the application to learn whether the file was added or updated. 
        /// </para>
        /// </remarks>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddFile(string,string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateDirectory(string,string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateItem(string,string)"/>
        ///
        /// <param name="fileName">
        /// The name of the file to add or update. It should refer to a file in the filesystem.  
        /// The name of the file may be a relative path or a fully-qualified path. 
        /// </param>
        ///
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the FileName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the files within the zip are later extracted, this is the path used for the extracted file. 
        /// Passing null (nothing in VB) will use the path on the FileName, if any.  Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        ///
        /// <returns>The ZipEntry corresponding to the File that was added or updated.</returns>
        public ZipEntry UpdateFile(string fileName, String directoryPathInArchive)
        {
            // ideally this would all be transactional!
            var key = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
            if (this[key] != null)
                this.RemoveEntry(key);
            return this.AddFile(fileName, directoryPathInArchive);
        }





        /// <summary>
        /// Add or Update a Directory in a zip archive.
        /// If the directory has already been added to the archive,
        /// its contents are updated.  If not, then the directory is added.
        /// </summary>
        ///
        /// <remarks>
        /// If the specified directory does not exist in the archive, then this method is equivalent to
        /// calling AddDirectory().  If the specified directory already exists in the archive, then this 
        /// method updates any existing entries, and adds any new entries. Any entries that are in the 
        /// zip archive but not in the specified directory, are left alone.  In other words, the contents of 
        /// the zip file is a union of the previous contents and the new files.
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateFile(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddDirectory(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateItem(string)"/>
        ///
        /// <param name="directoryName">The path to the directory to be added to the zip archive, 
        /// or updated in the zip archive.</param>
        public void UpdateDirectory(string directoryName)
        {
            UpdateDirectory(directoryName, null);
        }


        /// <summary>
        /// Add or Update a directory in the zip archive at the specified root directory in the archive.  
        /// If the directory has already been added to the archive,
        /// its contents are updated.  If not, then the directory is added.
        /// </summary>
        ///
        /// <remarks>
        /// If the specified directory does not exist in the archive, then this method is equivalent to
        /// calling AddDirectory().  If the specified directory already exists in the archive, then this 
        /// method updates any existing entries, and adds any new entries. Any entries that are in the 
        /// zip archive but not in the specified directory, are left alone.  In other words, the contents of 
        /// the zip file is a union of the previous contents and the new files.
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateFile(string,string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddDirectory(string,string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateItem(string,string)"/>
        ///
        /// <param name="directoryName">The path to the directory to be added to the zip archive, 
        /// or updated in the zip archive.</param>
        ///
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the ItemName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the files within the zip are later extracted, this is the path used for the extracted file. 
        /// Passing null (nothing in VB) will use the path on the FileName, if any.  Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        /// 
        public void UpdateDirectory(string directoryName, String directoryPathInArchive)
        {
            // ideally this would be transactional!
            //xxx
            //var key = ZipEntry.NameInArchive(directoryName, directoryPathInArchive);
            //if (this[key] != null)
            //    this.RemoveEntry(key);
            ////this.AddDirectory(DirectoryName, DirectoryPathInArchive);
            this.AddOrUpdateDirectoryImpl(directoryName, directoryPathInArchive, AddOrUpdateAction.AddOrUpdate);
        }





        /// <summary>
        /// Add or Update a File or Directory in the zip archive. This
        /// is useful when the application is not sure or does not care
        /// if the entries in the existing zip archive already exist.
        /// </summary>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddItem(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateFile(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateDirectory(string)"/>
        ///
        /// <param name="itemName">the path to the file or directory to be added or updated.</param>
        public void UpdateItem(string itemName)
        {
            UpdateItem(itemName, null);
        }


        /// <summary>
        /// Add or Update a File or Directory.  This is useful when the application is not sure or does not 
        /// care if the entries in the existing zip archive already exist.  
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This version of the method allows the caller to explicitly specify the 
        /// directory path to be used for the item being added to the archive.  The entry or entries 
        /// that are added or updated will use the specified <c>DirectoryPathInArchive</c>. Extracting
        /// the entry from the archive will result in a file stored in that directory path. 
        /// </para>
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddItem(string, string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateFile(string, string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateDirectory(string, string)"/>
        ///
        /// <param name="itemName">The path for the File or Directory to be added or updated.</param>
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the ItemName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the files within the zip are later extracted, this is the path used for the extracted file. 
        /// Passing null (nothing in VB) will use the path on the FileName, if any.  Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        public void UpdateItem(string itemName, string directoryPathInArchive)
        {
            if (System.IO.File.Exists(itemName))
                UpdateFile(itemName, directoryPathInArchive);

            else if (System.IO.Directory.Exists(itemName))
                UpdateDirectory(itemName, directoryPathInArchive);

            else
                throw new System.IO.FileNotFoundException(String.Format("That file or directory ({0}) does not exist!", itemName));
        }


        /// <summary>
        /// Uses the given stream as input to create an entry in the ZipFile, with the 
        /// given FileName and given Directory Path.  
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The stream must remain open and readable at least through the call to 
        /// <c>ZipFile.Save</c>.
        /// </para>
        /// <para>
        /// Encryption will be used on the stream data when saving the 
        /// ZipFile, if the Password is set on the ZipFile object prior to calling
        /// this method.</para>
        /// </remarks>
        ///
        /// <example>
        /// <code lang="C#">
        /// String ZipToCreate = "Content.zip";
        /// String FileNameInArchive = "Content-From-Stream.bin";
        /// using (ZipFile zip = new ZipFile(ZipToCreate))
        /// {
        ///   ZipEntry entry= zip.AddFileStream(FileNameInArchive, "basedirectory", StreamToRead);
        ///   entry.Comment = "The content for entry in the zip file was obtained from a stream";
        ///   zip.AddFile("Readme.txt");
        ///   zip.Save();
        /// }
        /// 
        /// </code>
        /// <code lang="VB">
        /// Dim ZipToCreate As String = "Content.zip"
        /// Dim FileNameInArchive As String = "Content-From-Stream.bin"
        /// Using zip As ZipFile = New ZipFile(ZipToCreate)
        ///   Dim entry as ZipEntry = zip.AddFileStream(FileNameInArchive, "basedirectory", Me.StreamToRead)
        ///   entry.Comment = "The content for entry in the zip file was obtained from a stream"
        ///   zip.AddFile("Readme.txt")
        ///   zip.Save
        /// End Using
        /// </code>
        /// </example>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateFileStream(string, string, System.IO.Stream)"/>
        ///
        /// <param name="fileName">FileName which is shown in the ZIP File</param>
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the ItemName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the files within the zip are later extracted, this is the path used for the extracted file. 
        /// Passing null (nothing in VB) will use the path on the FileName, if any.  Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        /// <param name="stream">the input stream from which to grab content for the file</param>
        /// <returns>The ZipEntry added.</returns>
        public ZipEntry AddFileStream(string fileName, string directoryPathInArchive, System.IO.Stream stream)
        {
            string n = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
            ZipEntry ze = ZipEntry.Create(fileName, n, stream);
            ze.TrimVolumeFromFullyQualifiedPaths = TrimVolumeFromFullyQualifiedPaths;
            ze.ForceNoCompression = ForceNoCompression;
            ze.WillReadTwiceOnInflation = WillReadTwiceOnInflation;
            ze.Encoding = Encoding;
            ze._Source = EntrySource.Stream;
            ze.Password = _Password;
            if (Verbose) StatusMessageTextWriter.WriteLine("adding {0}...", fileName);
            InsureUniqueEntry(ze);
            _entries.Add(ze);
            _contentsChanged = true;
            return ze;
        }



        /// <summary>
        /// Adds an entry into the zip archive using the given filename and directory path within the archive,
        /// and the given content for the file.  No file is created in the filesystem.  
        /// </summary>
        /// <param name="content">The content of the file, should it be extracted from the zip.</param>
        /// <param name="fileName">The filename to use within the archive.</param>
        /// <param name="directoryPathInArchive">
        /// Specifies a driectory path to use to override any path in the ItemName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the files within the zip are later extracted, this is the path used for the extracted file. 
        /// Passing null (nothing in VB) will use the path on the FileName, if any.  Passing the empty string ("")
        /// will insert the item at the root path within the archive. 
        /// </param>
        /// <returns>The ZipEntry added.</returns>
        /// 
        /// <example>
        /// This example shows how to add an entry to the zipfile, using a string as content for that entry. 
        /// <code lang="C#">
        /// string Content = "This string will be the content of the Readme.txt file in the zip archive.";
        /// using (ZipFile zip1 = new ZipFile())
        /// {
        ///   zip1.AddFile("MyDocuments\\Resume.doc", "files");
        ///   zip1.AddFileFromString("Readme.txt", "", Content); 
        ///   zip1.Comment = "This zip file was created at " + System.DateTime.Now.ToString("G");
        ///   zip1.Save("Content.zip");
        /// }
        /// 
        /// </code>
        /// <code lang="VB">
        /// Public Sub Run()
        ///   Dim Content As String = "This string will be the content of the Readme.txt file in the zip archive."
        ///   Using zip1 As ZipFile = New ZipFile
        ///     zip1.AddFileFromString("Readme.txt", "", Content)
        ///     zip1.AddFile("MyDocuments\Resume.doc", "files")
        ///     zip1.Comment = ("This zip file was created at " &amp; DateTime.Now.ToString("G"))
        ///     zip1.Save("Content.zip")
        ///   End Using
        /// End Sub
        /// </code>
        /// </example>
        public ZipEntry AddFileFromString(string fileName, string directoryPathInArchive, string content)
        {
            System.IO.MemoryStream ms = SharedUtilities.StringToMemoryStream(content);
            return AddFileStream(fileName, directoryPathInArchive, ms);
        }


        /// <summary>
        /// Updates the given entry in the zipFile, using the given stream as input, and the
        /// given FileName and given Directory Path.  Encryption will be used on the 
        /// stream data if the Password is set on the ZipFile object, prior to calling
        /// this method.
        /// </summary>
        ///
        /// <remarks>
        /// The stream must remain open and readable at least through the call to 
        /// <c>ZipFile.Save</c>.
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddFileStream(string, string, System.IO.Stream)"/>
        ///
        /// <param name="fileName">FileName which is shown in the ZIP File</param>
        /// <param name="directoryPathInArchive">The root path to be used in the ZIP archive, 
        /// for the entry added from the stream.</param>
        /// <param name="stream">The Input Stream to read file data from.</param>
        /// <returns>The ZipEntry added.</returns>
        public ZipEntry UpdateFileStream(string fileName, string directoryPathInArchive, System.IO.Stream stream)
        {
            var key = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
            if (this[key] != null)
                this.RemoveEntry(key);

            return AddFileStream(fileName, directoryPathInArchive, stream);
        }



        private void InsureUniqueEntry(ZipEntry ze1)
        {
            foreach (ZipEntry ze2 in _entries)
            {
                if (SharedUtilities.TrimVolumeAndSwapSlashes(ze1.FileName) == ze2.FileName)
                    throw new ArgumentException(String.Format("The entry '{0}' already exists in the zip archive.", ze1.FileName));
            }
        }

        /// <summary>
        /// Adds the contents of a filesystem directory to a Zip file archive. 
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// The name of the directory may be 
        /// a relative path or a fully-qualified path. Any files within the named 
        /// directory are added to the archive.  Any subdirectories within the named
        /// directory are also added to the archive, recursively. 
        /// </para>
        /// 
        /// <para>
        /// Top-level entries in the named directory will appear as top-level 
        /// entries in the zip archive.  Entries in subdirectories in the named 
        /// directory will result in entries in subdirectories in the zip archive.
        /// </para>
        /// 
        /// <para>
        /// If you want the entries to appear in a containing directory in the zip
        /// archive itself, then you should call the AddDirectory() overload that allows
        /// you to explicitly specify a containing directory.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddItem(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddFile(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateDirectory(string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddDirectory(string, string)"/>
        ///
        /// <overloads>This method has 2 overloads.</overloads>
        /// 
        /// <param name="directoryName">The name of the directory to add.</param>
        public void AddDirectory(string directoryName)
        {
            AddDirectory(directoryName, null);
        }


        /// <summary>
        /// Adds the contents of a filesystem directory to a Zip file archive, 
        /// overriding the path to be used for entries in the archive. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The name of the directory may be a relative path or a fully-qualified
        /// path. The add operation is recursive, so that any files or subdirectories
        /// within the name directory are also added to the archive.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// <para>
        /// In this code, calling the ZipUp() method with a value of "c:\temp" for the
        /// directory parameter will result in a zip file structure in which all entries
        /// are contained in a toplevel "temp" directory.
        /// </para>
        ///
        /// <code lang="C#">
        /// public void ZipUp(string targetZip, string directory)
        /// {
        ///   using (var zip = new ZipFile())
        ///   {
        ///     zip.AddDirectory(directory, System.IO.Path.GetFileName(directory));
        ///     zip.Save(targetZip);
        ///   }
        /// }
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddItem(string, string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddFile(string, string)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.UpdateDirectory(string, string)"/>
        ///
        /// <param name="directoryName">The name of the directory to add.</param>
        /// 
        /// <param name="directoryPathInArchive">
        /// Specifies a directory path to use to override any path in the DirectoryName.
        /// This path may, or may not, correspond to a real directory in the current filesystem.
        /// If the zip is later extracted, this is the path used for the extracted file or directory. 
        /// Passing null (nothing in VB) or the empty string ("")
        /// will insert the items at the root path within the archive. 
        /// </param>
        /// 
        public void AddDirectory(string directoryName, string directoryPathInArchive)
        {
            AddOrUpdateDirectoryImpl(directoryName, directoryPathInArchive, AddOrUpdateAction.AddOnly);
        }


        /// <summary>
        /// Creates a directory in the zip archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// 
        /// <para>
        /// Use this when you want to create a directory in the archive but there is no
        /// corresponding filesystem representation for that directory.
        /// </para>
        ///
        /// <para>
        /// You will probably not need to do this in your code. One of the only times
        /// you will want to do this is if you want an empty directory in the zip
        /// archive.  If you add a file to a zip archive that is stored within a
        /// multi-level directory, all of the directory tree is implicitly created in
        /// the zip archive.  
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="directoryNameInArchive">
        /// The name of the directory to create in the archive.
        /// </param>
        public ZipEntry AddDirectoryByName(string directoryNameInArchive)
        {
            // add the directory itself.
            ZipEntry baseDir = ZipEntry.Create(directoryNameInArchive, directoryNameInArchive);
            baseDir.TrimVolumeFromFullyQualifiedPaths = TrimVolumeFromFullyQualifiedPaths;
            baseDir._Source = EntrySource.Filesystem;
            baseDir.MarkAsDirectory();
            InsureUniqueEntry(baseDir);
            _entries.Add(baseDir);
            _contentsChanged = true;
            return baseDir;
        }



        private void AddOrUpdateDirectoryImpl(string directoryName, string rootDirectoryPathInArchive, AddOrUpdateAction action)
        {
            if (rootDirectoryPathInArchive == null)
            {
                rootDirectoryPathInArchive = "";
                //System.IO.Path.GetDirectoryName(SharedUtilities.TrimVolumeAndSwapSlashes(directoryName));
                //SharedUtilities.TrimVolumeAndSwapSlashes(directoryName);
            }

            AddOrUpdateDirectoryImpl(directoryName, rootDirectoryPathInArchive, action, 0);
        }



        private void AddOrUpdateDirectoryImpl(string directoryName, string rootDirectoryPathInArchive, AddOrUpdateAction action, int level)
        {
            if (Verbose) StatusMessageTextWriter.WriteLine("{0} {1}...",
                               (action == AddOrUpdateAction.AddOnly) ? "adding" : "Adding or updating", directoryName);

            string dirForEntries = rootDirectoryPathInArchive;

            if (level > 0)
            {
                int f = directoryName.Length;
                for (int i = level; i > 0; i--)
                    f = directoryName.LastIndexOfAny("/\\".ToCharArray(), f - 1, f - 1);

                dirForEntries = directoryName.Substring(f + 1);
                dirForEntries = System.IO.Path.Combine(rootDirectoryPathInArchive, dirForEntries);
            }

            // if not top level, or if the root is non-empty, then explicitly add the directory
            if (level > 0 || rootDirectoryPathInArchive != "")
            {
                ZipEntry baseDir = ZipEntry.Create(directoryName, dirForEntries);
                baseDir.Encoding = this.Encoding;  // workitem 6410
                baseDir.TrimVolumeFromFullyQualifiedPaths = TrimVolumeFromFullyQualifiedPaths;
                baseDir._Source = EntrySource.Filesystem;
                baseDir.MarkAsDirectory();

                // Previously, we used to test for the existence of the directory and 
                // throw if it exists.  But that seems silly. We will still throw 
                // if a file exists and the action is AddOnly.  But for a directory, 
                // it does not matter if it already exists.  So no throw. 

                //if (action == AddOrUpdateAction.AddOnly)
                //    InsureUniqueEntry(baseDir);
                //else
                //{
                //    // For updates, remove the old entry before adding the new. 
                //    ZipEntry e = this[baseDir.FileName];
                //    if (e != null)
                //        RemoveEntry(e);
                //}


                // check for uniqueness:
                ZipEntry e = this[baseDir.FileName];
                if (e == null)
                {
                    _entries.Add(baseDir);
                    _contentsChanged = true;
                }
                dirForEntries = baseDir.FileName;
            }

            String[] filenames = System.IO.Directory.GetFiles(directoryName);

            // add the files: 
            foreach (String filename in filenames)
            {
                if (action == AddOrUpdateAction.AddOnly)
                    AddFile(filename, dirForEntries);
                else
                    UpdateFile(filename, dirForEntries);
            }

            // add the subdirectories:
            String[] dirnames = System.IO.Directory.GetDirectories(directoryName);
            foreach (String dir in dirnames)
            {
                AddOrUpdateDirectoryImpl(dir, rootDirectoryPathInArchive, action, level + 1);
            }
            _contentsChanged = true;
        }

        #endregion

        #region Saving

        /// <summary>
        /// Saves the Zip archive, using the name given when the ZipFile was instantiated. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// The zip file is written to storage only when the caller calls <c>Save</c>.  
        /// The Save operation writes the zip content to a temporary file. 
        /// Then, if the zip file already exists (for example when adding an item to a zip archive)
        /// this method will replace the existing zip file with this temporary file.
        /// If the zip file does not already exist, the temporary file is renamed 
        /// to the desired name.  
        /// </para>
        ///
        /// <para>
        /// When using a filesystem file for the Zip output, it is possible to call
        /// <c>Save</c> multiple times on the ZipFile instance. With each call the zip content
        /// is written to the output file. When saving to a <c>Stream</c>, after the initial
        /// call to <c>Save</c>, additional calls to <c>Save</c> will throw. This is because the
        /// stream is assumed to be a write-only stream, and after the initial <c>Save</c>, it
        /// is not possible to seek backwards and "unwrite" the zip file data.
        /// </para>
        ///
        /// <para>
        /// Data for entries that have been added to the <c>ZipFile</c> instance is written
        /// to the output when the <c>Save</c> method is called. This means that the input
        /// streams for those entries must be available at the time the application
        /// calls <c>Save</c>.  If, for example, the application adds entries with
        /// <c>AddFileStream</c> using a dynamically-allocated <c>MemoryStream</c>,
        /// the memory stream must not have been disposed before the call to <c>Save</c>.
        /// </para>
        ///
        /// <para>
        /// When using the zip library within an ASP.NET application, you may wish to set the
        /// <c>TempFileFolder</c> property on the <c>ZipFile</c> instance before calling Save().
        /// </para>
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.AddFileStream(String, String, System.IO.Stream)"/>
        ///
        /// <exception cref="Ionic.Utils.Zip.BadStateException">
        /// Thrown if you haven't specified a location or stream for saving the zip,
        /// either in the constructor or by setting the Name property. 
        /// </exception>
        ///
        public void Save()
        {
            try
            {
                _saveOperationCanceled = false;
                OnSaveStarted();

                if (WriteStream == null)
                    throw new BadStateException("You haven't specified where to save the zip.");
                // check if modified, before saving. 
                if (!_contentsChanged) return;

                if (Verbose) StatusMessageTextWriter.WriteLine("Saving....");

                // write an entry in the zip for each file
                int n = 0;
                foreach (ZipEntry e in _entries)
                {
                    e.Write(WriteStream);
                    e._zipfile = this;
                    n++;
                    OnSaveProgress(n, e.FileName);
                    if (_saveOperationCanceled)
                        break;
                }

                if (_saveOperationCanceled)
                    return;

                WriteCentralDirectoryStructure(WriteStream);

                // do the rename as necessary
                if ((_temporaryFileName != null) && (_name != null))
                {
                    // _temporaryFileName may remain null if we are writing to a stream
                    // only close the stream if there is a file behind it. 
                    WriteStream.Close();
                    WriteStream.Dispose();
                    WriteStream = null;

                    if (_saveOperationCanceled)
                        return;

                    if ((_fileAlreadyExists) && (this._readstream != null))
                    {
                        // This means we opened and read a zip file. 
                        // If we are now saving to the same file, we need to close the
                        // orig file, first.
                        this._readstream.Close();
                        this._readstream = null;
                    }

                    if (_fileAlreadyExists)
                    {
                        // We do not just call File.Replace() here because 
                        // there is a possibility that the TEMP volume is different 
                        // that the volume for the final file (c:\ vs d:\).
                        // So we need to do a Delete+Move pair. 
                        //
                        // Ideally this would be transactional. 
                        // 
                        // It's possible that the delete succeeds and the move fails.  
                        // in that case, we're hosed, and we'll throw.
                        //
                        // Could make this more complicated by moving (renaming) the first file, then
                        // moving the second, then deleting the first file. But the
                        // error handling and unwrap logic just gets more complicated.
                        //
                        // Better to just keep it simple. 
                        System.IO.File.Delete(_name);
                        System.IO.File.Move(_temporaryFileName, _name);
                    }
                    else
                        System.IO.File.Move(_temporaryFileName, _name);

                    _fileAlreadyExists = true;
                }

                OnSaveCompleted();
                _JustSaved = true;

            }

                // workitem 5043
            finally
            {
                CleanupAfterSaveOperation();
            }

            return;
        }


        private void RemoveTempFile()
        {
            try
            {
                if (System.IO.File.Exists(_temporaryFileName))
                {
                    System.IO.File.Delete(_temporaryFileName);
                }
            }
            catch (Exception ex1)
            {
                StatusMessageTextWriter
                .WriteLine("ZipFile::Save: could not delete temp file: {0}.", ex1.Message);
            }
        }


        private void CleanupAfterSaveOperation()
        {
            if ((_temporaryFileName != null) && (_name != null))
            {
                // only close the stream if there is a file behind it. 
                if (_writestream != null)
                {
                    try { _writestream.Close(); }
                    catch { }
                    try { _writestream.Dispose(); }
                    catch { }
                }
                _writestream = null;
                RemoveTempFile();
            }
        }


        /// <summary>
        /// Save the file to a new zipfile, with the given name. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This is handy when reading a zip archive from a stream 
        /// and you want to modify the archive (add a file, change a 
        /// comment, etc) and then save it to a file. 
        /// </para>
        /// <para>
        /// It also works if you create a new ZipFile for writing to a 
        /// stream, and then you also want to write it to a filesystem file. 
        /// In that case, call the Save() method, and then also call this method with
        /// a filename. 
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="System.ArgumentException">
        /// Thrown if you specify a directory for the filename.
        /// </exception>
        ///
        /// <param name="zipFileName">
        /// The name of the zip archive to save to. Existing files will 
        /// be overwritten with great prejudice.
        /// </param>
        public void Save(string zipFileName)
        {
            // Check for the case where we are re-saving a zip archive 
            // that was originally instantiated with a stream.  In that case, 
            // the _name will be null. If so, we set _writestream to null, 
            // which insures that we'll cons up a new WriteStream (with a filesystem
            // file backing it) in the Save() method.
            if (_name == null)
                _writestream = null;

            _name = zipFileName;
            if (Directory.Exists(_name))
                throw new ZipException("Bad Directory", new System.ArgumentException("That name specifies an existing directory. Please specify a filename.", "zipFileName"));
            _contentsChanged = true;
            _fileAlreadyExists = File.Exists(_name);
            Save();
        }


        private void WriteCentralDirectoryStructure(Stream s)
        {
            // The Central Directory Structure.
            // We need to keep track of the start and Finish of the Central Directory Structure. 
            var output = s as CountingStream;
            long Start = (output != null) ? output.BytesWritten : s.Position;

            foreach (ZipEntry e in _entries)
            {
                e.WriteCentralDirectoryEntry(s);  // this writes a ZipDirEntry corresponding to the ZipEntry
            }
            //long Finish = WriteStream.Length; // Position;  ditto - for ASP.NET
            long Finish = (output != null) ? output.BytesWritten : s.Position;

            // now, the footer
            WriteCentralDirectoryFooter(s, Start, Finish);
        }


        private void WriteCentralDirectoryFooter(Stream s, long StartOfCentralDirectory, long EndOfCentralDirectory)
        {
            int bufferLength = 22;
            if (Comment != null) bufferLength += Comment.Length;
            byte[] bytes = new byte[bufferLength];

            int i = 0;
            // signature
            bytes[i++] = (byte)(ZipConstants.EndOfCentralDirectorySignature & 0x000000FF);
            bytes[i++] = (byte)((ZipConstants.EndOfCentralDirectorySignature & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((ZipConstants.EndOfCentralDirectorySignature & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((ZipConstants.EndOfCentralDirectorySignature & 0xFF000000) >> 24);

            // number of this disk
            bytes[i++] = 0;
            bytes[i++] = 0;

            // number of the disk with the start of the central directory
            bytes[i++] = 0;
            bytes[i++] = 0;

            // total number of entries in the central dir on this disk
            bytes[i++] = (byte)(_entries.Count & 0x00FF);
            bytes[i++] = (byte)((_entries.Count & 0xFF00) >> 8);

            // total number of entries in the central directory
            bytes[i++] = (byte)(_entries.Count & 0x00FF);
            bytes[i++] = (byte)((_entries.Count & 0xFF00) >> 8);

            // size of the central directory
            Int32 SizeOfCentralDirectory = (Int32)(EndOfCentralDirectory - StartOfCentralDirectory);
            bytes[i++] = (byte)(SizeOfCentralDirectory & 0x000000FF);
            bytes[i++] = (byte)((SizeOfCentralDirectory & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((SizeOfCentralDirectory & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((SizeOfCentralDirectory & 0xFF000000) >> 24);

            // offset of the start of the central directory 
            Int32 StartOffset = (Int32)StartOfCentralDirectory;  // cast down from Long
            bytes[i++] = (byte)(StartOffset & 0x000000FF);
            bytes[i++] = (byte)((StartOffset & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((StartOffset & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((StartOffset & 0xFF000000) >> 24);

            // zip archive comment 
            if ((Comment == null) || (Comment.Length == 0))
            {
                // no comment!
                bytes[i++] = (byte)0;
                bytes[i++] = (byte)0;
            }
            else
            {
#if NO_UTF8		
                Int16 commentLength = (Int16)Comment.Length;
                // the size of our buffer defines the max length of the comment we can write
                if (commentLength + i + 2 > bytes.Length) commentLength = (Int16)(bytes.Length - i - 2);
                bytes[i++] = (byte)(commentLength & 0x00FF);
                bytes[i++] = (byte)((commentLength & 0xFF00) >> 8);

                char[] c = Comment.ToCharArray();

                int j = 0;
                // now actually write the comment itself into the byte buffer
                for (j = 0; (j < commentLength) && (i + j < bytes.Length); j++)
                {
                    bytes[i + j] = System.BitConverter.GetBytes(c[j])[0];
                }
                i += j;
#else

                byte[] block = SharedUtilities.StringToByteArray(Comment, Encoding);
                Int16 commentLength = (Int16)block.Length;
                // the size of our buffer defines the max length of the comment we can write
                if (commentLength + i + 2 > bytes.Length) commentLength = (Int16)(bytes.Length - i - 2);
                bytes[i++] = (byte)(commentLength & 0x00FF);
                bytes[i++] = (byte)((commentLength & 0xFF00) >> 8);

                if (commentLength != 0)
                {
                    int j = 0;
                    // now actually write the comment itself into the byte buffer
                    for (j = 0; (j < commentLength) && (i + j < bytes.Length); j++)
                    {
                        bytes[i + j] = block[j];
                    }
                    i += j;
                }

#endif

            }

            s.Write(bytes, 0, i);
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired when the save starts.
        /// </summary>
        public event EventHandler<SaveEventArgs> SaveStarted;

        /// <summary>
        /// Fired after each entry has been written to the archive.
        /// </summary>
        /// <example>
        /// <code lang="C#">
        /// public static void SaveProgress(object sender, SaveProgressEventArgs e)
        /// {
        ///   Console.WriteLine("{0} ({1}/{2})", e.NameOfLatestEntry, e.EntriesSaved, e.EntriesTotal);
        /// }
        /// 
        /// public static ZipUp(string targetZip, string directory)
        /// {
        ///   using (var zip = new ZipFile()) {
        ///     zip.SaveProgress += SaveProgress; 
        ///     zip.AddDirectory(directory);
        ///     zip.Save(targetZip);
        ///   }
        /// }
        ///
        /// </code>
        /// <code lang="VB">
        /// Public Sub ZipUp(ByVal targetZip As String, ByVal directory As String)
        /// 	Try 
        /// 	    Using zip As ZipFile = New ZipFile
        /// 		AddHandler zip.SaveProgress, AddressOf MySaveProgress
        /// 		zip.AddDirectory(directory)
        /// 		zip.Save(targetZip)
        /// 	    End Using
        /// 	Catch ex1 As Exception
        /// 	    Console.Error.WriteLine(("exception: " &amp; ex1.ToString))
        /// 	End Try
        /// End Sub
        /// 
        /// Public Sub MySaveProgress(ByVal sender As Object, ByVal e As SaveProgressEventArgs)
        ///     Console.WriteLine("{0} ({1}/{2})", e.NameOfLatestEntry, e.EntriesSaved, e.EntriesTotal)
        /// End Sub
        /// </code>
        /// </example>
        public event EventHandler<SaveProgressEventArgs> SaveProgress;

        /// <summary>
        /// Fired after the save completes.
        /// </summary>
        public event EventHandler<SaveEventArgs> SaveCompleted;


        /// <summary>
        /// Fires the <see cref="SaveProgress"/> method.
        /// </summary>
        private void OnSaveProgress(int current, string currentEntryName)
        {
            lock (LOCK)
            {
                if (SaveProgress != null)
                {
                    var e = new SaveProgressEventArgs(_entries.Count, current, currentEntryName);
                    SaveProgress(this, e);
                    if (e.Cancel)
                        _saveOperationCanceled = true;
                }
            }
        }


        private void OnSaveCompleted()
        {
            lock (LOCK)
            {
                if (SaveCompleted != null)
                {
                    var e = new SaveEventArgs((_name != null) ? _name : "(stream)");
                    SaveCompleted(this, e);
                }
            }
        }


        private void OnSaveStarted()
        {
            lock (LOCK)
            {
                if (SaveStarted != null)
                {
                    var e = new SaveEventArgs((_name != null) ? _name : "(stream)");
                    SaveStarted(this, e);
                }
            }
        }



        /// <summary>
        /// Fired when the ExtractAll starts, before the first entry is extracted.
        /// </summary>
        public event EventHandler<ExtractEventArgs> ExtractStarted;

        /// <summary>
        /// Fired after each entry has been extracted from the archive, during the 
        /// execution of the ExtractAll method.
        /// </summary>
        /// <example>
        /// <code>
        /// public static void ExtractProgress(object sender, ExtractProgressEventArgs e)
        /// {
        ///   Console.WriteLine("{0} ({1}/{2})", e.NameOfLatestEntry, e.EntriesExtractd, e.EntriesTotal);
        /// }
        /// 
        /// public static ExtractZip(string zipToExtract, string directory)
        /// {
        ///   using (var zip = new ZipFile(zipToExtract)) {
        ///     zip.ExtractProgress += ExtractProgress; 
        ///     zip.ExtractAll(directory, true);
        ///   }
        /// }
        ///
        /// </code>
        /// </example>
        public event EventHandler<ExtractProgressEventArgs> ExtractProgress;

        /// <summary>
        /// Fired after the extract completes.
        /// </summary>
        public event EventHandler<ExtractEventArgs> ExtractCompleted;


        /// <summary>
        /// Fires the <see cref="ExtractProgress"/> method.
        /// </summary>
        private void OnExtractProgress(int current, string currentEntryName, string path, bool overwrite)
        {
            lock (LOCK)
            {
                if (ExtractProgress != null)
                {
                    var e = new ExtractProgressEventArgs(_entries.Count, current, currentEntryName, path, overwrite);
                    ExtractProgress(this, e);
                    if (e.Cancel)
                        _extractOperationCanceled = true;
                }
            }
        }


        private void OnExtractCompleted(string path, bool wantOverwrite)
        {
            lock (LOCK)
            {
                if (ExtractCompleted != null)
                {
                    var e = new ExtractEventArgs((_name != null) ? _name : "(stream)",
                         path,
                         wantOverwrite);
                    ExtractCompleted(this, e);
                }
            }
        }


        private void OnExtractStarted(string path, bool wantOverwrite)
        {
            lock (LOCK)
            {
                if (ExtractStarted != null)
                {
                    var e = new ExtractEventArgs((_name != null) ? _name : "(stream)",
                         path,
                         wantOverwrite);
                    ExtractStarted(this, e);
                }
            }
        }


        #endregion

        #region Reading Zip Files

        /// <summary>
        /// Checks the given file to see if it appears to be a valid zip file.
        /// </summary>
        /// <remarks>
        /// This method opens the file, and reads in the zip header, as well as the
        /// zip directory structure.  If everything succeeds, then the method
        /// returns true.  If anything fails - for example if an incorrect signature
        /// is found, the the method returns false.  This method also returns false
        /// (no exception) for a file that does not exist.  Because this method does
        /// not actually read in the zip content, decrypt, and check CRCs, it is
        /// possible for this method to return true in the case the zip file is
        /// corrupted.
        /// </remarks>
        /// <param name="fileName">The file to check.</param>
        /// <returns>true if the file appears to be a zip file.</returns>
        public static bool IsZipFile(string fileName)
        {
            bool result = false;
            try
            {
                // if no exception, then ... it is a zip file.
                using (ZipFile zf = ZipFile.Read(fileName, null, System.Text.Encoding.GetEncoding("IBM437"))) { }
                result = true;
            }
            catch (ZipException) { }
            return result;
        }


        /// <summary>
        /// Reads a zip file archive and returns the instance.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The stream is read using the default <c>System.Text.Encoding</c>, which is the <c>IBM437</c> codepage.  
        /// </para>
        /// </remarks>
        /// <exception cref="System.Exception">
        /// Thrown if the ZipFile cannot be read. The implementation of this 
        /// method relies on <c>System.IO.File.OpenRead</c>, which can throw
        /// a variety of exceptions, including specific exceptions if a file
        /// is not found, an unauthorized access exception, exceptions for
        /// poorly formatted filenames, and so on. 
        /// </exception>
        /// 
        /// <param name="zipFileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <overloads>This method has a bunch of interesting overloads. They are all static (Shared in VB)</overloads>
        ///
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string zipFileName)
        {
            return ZipFile.Read(zipFileName, null, DefaultEncoding);
        }


        /// <summary>
        /// Reads a zip file archive using the specified text encoding, and returns the instance.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This version of the method allows the caller to pass in a <c>TextWriter</c>.  
        /// The ZipFile is read in using the default IBM437 encoding for entries where UTF-8 
        /// encoding is not explicitly specified.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// <code lang="C#">
        /// var sw = new System.IO.StringWriter();
        /// using (ZipFile zip =  ZipFile.Read("PackedDocuments.zip", sw))
        /// {
        ///   var Threshold = new DateTime(2007,7,4);
        ///   // We cannot remove the entry from the list, within the context of 
        ///   // an enumeration of said list.
        ///   // So we add the doomed entry to a list to be removed later.
        ///   // pass 1: mark the entries for removal
        ///   var MarkedEntries = new System.Collections.Generic.List&lt;ZipEntry&gt;();
        ///   foreach (ZipEntry e in zip)
        ///   {
        ///     if (e.LastModified &lt; Threshold)
        ///       MarkedEntries.Add(e);
        ///   }
        ///   // pass 2: actually remove the entry. 
        ///   foreach (ZipEntry zombie in MarkedEntries)
        ///      zip.RemoveEntry(zombie);
        ///   zip.Comment = "This archive has been updated.";
        ///   zip.Save();
        /// }
        /// // can now use contents of sw, eg store in an audit log
        /// </code>
        ///
        /// <code lang="VB">
        ///   Dim sw As New System.IO.StringWriter
        ///   Using zip As ZipFile = ZipFile.Read("PackedDocuments.zip", sw)
        ///       Dim Threshold As New DateTime(2007, 7, 4)
        ///       ' We cannot remove the entry from the list, within the context of 
        ///       ' an enumeration of said list.
        ///       ' So we add the doomed entry to a list to be removed later.
        ///       ' pass 1: mark the entries for removal
        ///       Dim MarkedEntries As New System.Collections.Generic.List(Of ZipEntry)
        ///       Dim e As ZipEntry
        ///       For Each e In zip
        ///           If (e.LastModified &lt; Threshold) Then
        ///               MarkedEntries.Add(e)
        ///           End If
        ///       Next
        ///       ' pass 2: actually remove the entry. 
        ///       Dim zombie As ZipEntry
        ///       For Each zombie In MarkedEntries
        ///           zip.RemoveEntry(zombie)
        ///       Next
        ///       zip.Comment = "This archive has been updated."
        ///       zip.Save
        ///   End Using
        ///   ' can now use contents of sw, eg store in an audit log
        /// </code>
        /// </example>
        /// 
        /// <exception cref="System.Exception">
        /// Thrown if the zipfile cannot be read. The implementation of this 
        /// method relies on <c>System.IO.File.OpenRead</c>, which can throw
        /// a variety of exceptions, including specific exceptions if a file
        /// is not found, an unauthorized access exception, exceptions for
        /// poorly formatted filenames, and so on. 
        /// </exception>
        /// 
        /// <param name="zipFileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to use for writing verbose status messages during operations
        /// on the zip archive.  A console application may wish to pass <c>System.Console.Out</c> to get 
        /// messages on the Console. A graphical or headless application may wish to capture the messages 
        /// in a different <c>TextWriter</c>, such as a <c>System.IO.StringWriter</c>. 
        /// </param>
        /// 
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string zipFileName, System.IO.TextWriter statusMessageWriter)
        {
            return ZipFile.Read(zipFileName, statusMessageWriter, DefaultEncoding);
        }

        /// <summary>
        /// Reads a zip file archive using the specified text encoding, and returns the instance.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This version of the method allows the caller to pass in an <c>Encoding</c>.  
        /// The ZipFile is read in using the specified encoding for entries where UTF-8
        /// encoding is not explicitly specified.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// This example shows how to read a zip file using the Big-5 Chinese code page (950), 
        /// and extract each entry in the zip file.
        /// <code lang="C#">
        /// using (ZipFile zip = ZipFile.Read(ZipToExtract,
        ///                                   System.Text.Encoding.GetEncoding(950)))
        /// {
        ///   foreach (ZipEntry e in zip)
        ///   {
        ///      e.Extract(extractDirectory);
        ///   }
        /// }
        /// </code>
        /// </example>
	///
        /// <exception cref="System.Exception">
        /// Thrown if the zipfile cannot be read. The implementation of this 
        /// method relies on <c>System.IO.File.OpenRead</c>, which can throw
        /// a variety of exceptions, including specific exceptions if a file
        /// is not found, an unauthorized access exception, exceptions for
        /// poorly formatted filenames, and so on. 
        /// </exception>
        /// 
        /// <param name="zipFileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="encoding">
        /// The <c>System.Text.Encoding</c> to use when reading in the zip archive. Be careful specifying the
        /// encoding.  If the value you use here is not the same as the Encoding used when the zip archive was 
        /// created (possibly by a different archiver) you will get unexpected results and possibly exceptions. 
        /// </param>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.Encoding">Encoding</seealso>.
	///
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string zipFileName, System.Text.Encoding encoding)
        {
            return ZipFile.Read(zipFileName, null, encoding);
        }


        /// <summary>
        /// Reads a zip file archive using the specified text encoding, and returns the instance.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This version of the method allows the caller to pass in a <c>TextWriter</c> and an <c>Encoding</c>.  
        /// The ZipFile is read in using the specified encoding for entries where UTF-8
        /// encoding is not explicitly specified.
        /// </para>
        /// </remarks>
        /// 
        /// 
        /// <example>
        /// This example shows how to read a zip file using the Big-5 Chinese code page (950), 
        /// and extract each entry in the zip file, while sending status messages out to the Console. 
        /// <code lang="C#">
        /// using (ZipFile zip = ZipFile.Read(ZipToExtract,
        ///                                   System.Console.Out,
        ///                                   System.Text.Encoding.GetEncoding(950)))
        /// {
        ///   foreach (ZipEntry e in zip)
        ///   {
        ///      e.Extract(extractDirectory);
        ///   }
        /// }
        /// </code>
        /// </example>
	///
        /// <exception cref="System.Exception">
        /// Thrown if the zipfile cannot be read. The implementation of this 
        /// method relies on <c>System.IO.File.OpenRead</c>, which can throw
        /// a variety of exceptions, including specific exceptions if a file
        /// is not found, an unauthorized access exception, exceptions for
        /// poorly formatted filenames, and so on. 
        /// </exception>
        /// 
        /// <param name="zipFileName">
        /// The name of the zip archive to open.  
        /// This can be a fully-qualified or relative pathname.
        /// </param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to use for writing verbose status messages during operations
        /// on the zip archive.  A console application may wish to pass <c>System.Console.Out</c> to get 
        /// messages on the Console. A graphical or headless application may wish to capture the messages 
        /// in a different <c>TextWriter</c>, such as a <c>System.IO.StringWriter</c>. 
        /// </param>
        /// 
        /// <param name="encoding">
        /// The <c>System.Text.Encoding</c> to use when reading in the zip archive. Be careful specifying the
        /// encoding.  If the value you use here is not the same as the Encoding used when the zip archive was 
        /// created (possibly by a different archiver) you will get unexpected results and possibly exceptions.  
        /// </param>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.Encoding">Encoding</seealso>.
	///
        /// <returns>The instance read from the zip archive.</returns>
        /// 
        public static ZipFile Read(string zipFileName, System.IO.TextWriter statusMessageWriter, System.Text.Encoding encoding)
        {
            ZipFile zf = new ZipFile();
            zf.Encoding = encoding;
            zf._StatusMessageTextWriter = statusMessageWriter;
            zf._name = zipFileName;

            try
            {
                ReadIntoInstance(zf);
                zf._fileAlreadyExists = true;
            }
            catch (Exception e1)
            {
                throw new ZipException(String.Format("{0} is not a valid zip file", zipFileName), e1);
            }
            return zf;
        }

        /// <summary>
        /// Reads a zip archive from a stream.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// This is useful when when the zip archive content is available from 
        /// an already-open stream. The stream must be open and readable when calling this
        /// method.  The stream is left open when the reading is completed. 
        /// </para>
        /// <para>
        /// The stream is read using the default <c>System.Text.Encoding</c>, which is the <c>IBM437</c> codepage.  
        /// </para>
        /// </remarks>
        ///
        /// <example>
        /// This example shows how to Read zip content from a stream, and extract
        /// one entry into a different stream. In this example, the filename
        /// "NameOfEntryInArchive.doc", refers only to the name of the entry
        /// within the zip archive.  A file by that name is not created in the
        /// filesystem.  The I/O is done strictly with the given streams.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(InputStream))
        /// {
        ///   zip.Extract("NameOfEntryInArchive.doc", OutputStream);
        /// }
        /// </code>
        /// <code lang="VB">
        /// Using zip as ZipFile = ZipFile.Read(InputStream)
        ///   zip.Extract("NameOfEntryInArchive.doc", OutputStream)
        /// End Using
        /// </code>
        /// </example>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(System.IO.Stream zipStream)
        {
            return Read(zipStream, null, DefaultEncoding);
        }

        /// <summary>
        /// Reads a zip archive from a stream, using the specified TextWriter for status messages.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method is useful when when the zip archive content is available from 
        /// an already-open stream. The stream must be open and readable when calling this
        /// method.  The stream is left open when the reading is completed. 
        /// </para>
        /// 
        /// <para>
        /// The stream is read using the default <c>System.Text.Encoding</c>, which is the <c>IBM437</c> codepage.  
        /// For more information on the encoding, see the <see cref="Ionic.Utils.Zip.ZipFile.Encoding">Encoding</see> property.
        /// </para>
        /// </remarks>
        /// 
        /// 
        /// <exception cref="Ionic.Utils.Zip.ZipException">
        /// Thrown if zipStream is null.
        /// In this case, the inner exception is an ArgumentException.
        /// </exception>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        /// 
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written during operations on the ZipFile.  
        /// For example, in a console application, System.Console.Out works, and will get a message for each entry added to the ZipFile. 
        /// If the TextWriter is null, no verbose messages are written. 
        /// </param>
        /// 
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(System.IO.Stream zipStream, System.IO.TextWriter statusMessageWriter)
        {
            return Read(zipStream, statusMessageWriter, DefaultEncoding);
        }

        /// <summary>
        /// Reads a zip archive from a stream, using the specified encoding.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method is useful when when the zip archive content is available from 
        /// an already-open stream. The stream must be open and readable when calling this
        /// method.  The stream is left open when the reading is completed. 
        /// </para>
        /// </remarks>
        ///
        /// <exception cref="Ionic.Utils.Zip.ZipException">
        /// Thrown if zipStream is null.
        /// In this case, the inner exception is an ArgumentException.
        /// </exception>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        /// 
        /// <param name="encoding">
        /// The text encoding to use when reading entries that do not have the UTF-8 encoding bit
	/// set.  Be careful specifying the encoding.  If the value you use here is not the same
	/// as the Encoding used when the zip archive was created (possibly by a different
	/// archiver) you will get unexpected results and possibly exceptions.  See the <see
	/// cref="Ionic.Utils.Zip.ZipFile.Encoding">Encoding</see> property for more information.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(System.IO.Stream zipStream, System.Text.Encoding encoding)
        {
            return Read(zipStream, null, encoding);
        }

        /// <summary>
        /// Reads a zip archive from a stream, using the specified text Encoding and the specified TextWriter for status messages.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is useful when when the zip archive content is available from 
        /// an already-open stream. The stream must be open and readable when calling this
        /// method.  The stream is left open when the reading is completed. 
        /// </para>
        /// </remarks>
        ///
        /// <exception cref="Ionic.Utils.Zip.ZipException">
        /// Thrown if zipStream is null.
        /// In this case, the inner exception is an ArgumentException.
        /// </exception>
        ///
        /// <param name="zipStream">the stream containing the zip data.</param>
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written during operations on the ZipFile.  
        /// For example, in a console application, System.Console.Out works, and will get a message for each entry added to the ZipFile. 
        /// If the TextWriter is null, no verbose messages are written. 
        /// </param>
	///
        /// <param name="encoding">
        /// The text encoding to use when reading entries that do not have the UTF-8 encoding bit
	/// set.  Be careful specifying the encoding.  If the value you use here is not the same
	/// as the Encoding used when the zip archive was created (possibly by a different
	/// archiver) you will get unexpected results and possibly exceptions.  See the <see
	/// cref="Ionic.Utils.Zip.ZipFile.Encoding">Encoding</see> property for more information.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile</returns>
        public static ZipFile Read(System.IO.Stream zipStream, System.IO.TextWriter statusMessageWriter, System.Text.Encoding encoding)
        {
            if (zipStream == null)
                throw new ZipException("Cannot read.", new ArgumentException("The stream must be non-null", "zipStream"));

            ZipFile zf = new ZipFile();
            zf._encoding = encoding;
            zf._StatusMessageTextWriter = statusMessageWriter;
            zf._readstream = zipStream;
            zf._ReadStreamIsOurs = false;
            ReadIntoInstance(zf);
            return zf;
        }

        /// <summary>
        /// Reads a zip archive from a byte array.
        /// </summary>
        /// 
        /// <remarks>
        /// This is useful when the data for the zipfile is contained in a byte array, 
        /// for example, downloaded from an FTP server without being saved to a
        /// filesystem. 
        /// </remarks>
        /// 
        /// <param name="buffer">
        /// The byte array containing the zip data.  
        /// (I don't know why, but sometimes the compiled helpfuile (.chm) indicates a 2d 
        /// array when it is just one-dimensional.  This is a one-dimensional array.)
        /// </param>
        /// 
        /// <returns>an instance of ZipFile. The name on the ZipFile will be null (nothing in VB)). </returns>
        public static ZipFile Read(byte[] buffer)
        {
            return Read(buffer, null, DefaultEncoding);
        }


        /// <summary>
        /// Reads a zip archive from a byte array, using the given StatusMessageWriter.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method is useful when the data for the zipfile is contained in a byte array, for
        /// example when retrieving the data from a database or other non-filesystem store.  
        /// The default Text Encoding (IBM437) is used to read the zipfile data.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="buffer">the byte array containing the zip data.</param>
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written during operations on the ZipFile.  
        /// For example, in a console application, System.Console.Out works, and will get a message for each entry added to the ZipFile. 
        /// If the TextWriter is null, no verbose messages are written. 
        /// </param>
        /// 
        /// <returns>an instance of ZipFile. The name is set to null.</returns>
        /// 
        public static ZipFile Read(byte[] buffer, System.IO.TextWriter statusMessageWriter)
        {
            return Read(buffer, statusMessageWriter, DefaultEncoding);
        }


        /// <summary>
        /// Reads a zip archive from a byte array, using the given StatusMessageWriter and text Encoding.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method is useful when the data for the zipfile is contained in a byte array, for
        /// example when retrieving the data from a database or other non-filesystem store.  
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <param name="buffer">the byte array containing the zip data.</param>
        /// <param name="statusMessageWriter">
        /// The <c>System.IO.TextWriter</c> to which verbose status messages are written during operations on the ZipFile.  
        /// For example, in a console application, System.Console.Out works, and will get a message for each entry added to the ZipFile. 
        /// If the TextWriter is null, no verbose messages are written. 
        /// </param>
        /// 
        /// <param name="encoding">
        /// The text encoding to use when reading entries that do not have the UTF-8 encoding bit
	/// set.  Be careful specifying the encoding.  If the value you use here is not the same
	/// as the Encoding used when the zip archive was created (possibly by a different
	/// archiver) you will get unexpected results and possibly exceptions.  See the <see
	/// cref="Ionic.Utils.Zip.ZipFile.Encoding">Encoding</see> property for more information.
        /// </param>
        /// 
        /// <returns>an instance of ZipFile. The name is set to null.</returns>
        /// 
        public static ZipFile Read(byte[] buffer, System.IO.TextWriter statusMessageWriter, System.Text.Encoding encoding)
        {
            ZipFile zf = new ZipFile();
            zf._StatusMessageTextWriter = statusMessageWriter;
            zf._encoding = encoding;
            zf._readstream = new System.IO.MemoryStream(buffer);
            zf._ReadStreamIsOurs = true;
            ReadIntoInstance(zf);
            return zf;
        }


        private static void ReadIntoInstance(ZipFile zf)
        {
            try
            {
                zf._entries = new System.Collections.Generic.List<ZipEntry>();
                ZipEntry e;
                if (zf.Verbose)
                    if (zf.Name == null)
                        zf.StatusMessageTextWriter.WriteLine("Reading zip from stream...");
                    else
                        zf.StatusMessageTextWriter.WriteLine("Reading zip {0}...", zf.Name);

                while ((e = ZipEntry.Read(zf.ReadStream, zf.Encoding)) != null)
                {
                    if (zf.Verbose)
                        zf.StatusMessageTextWriter.WriteLine("  {0}", e.FileName);

                    zf._entries.Add(e);
                }

                // read the zipfile's central directory structure here.
                zf._direntries = new System.Collections.Generic.List<ZipDirEntry>();

                ZipDirEntry de;
                while ((de = ZipDirEntry.Read(zf.ReadStream, zf.Encoding)) != null)
                {
                    zf._direntries.Add(de);
                    // Housekeeping: Since ZipFile exposes ZipEntry elements in the enumerator, 
                    // we need to copy the comment that we grab from the ZipDirEntry
                    // into the ZipEntry, so the application can access the comment. 
                    // Also since ZipEntry is used to Write zip files, we need to copy the 
                    // file attributes to the ZipEntry as appropriate. 
                    foreach (ZipEntry e1 in zf._entries)
                    {
                        if (e1.FileName == de.FileName)
                        {
                            e1.Comment = de.Comment;
                            if (de.IsDirectory) e1.MarkAsDirectory();
                            break;
                        }
                    }
                }

                ReadCentralDirectoryFooter(zf);

                if (zf.Verbose && !String.IsNullOrEmpty(zf.Comment))
                    zf.StatusMessageTextWriter.WriteLine("Zip file Comment: {0}", zf.Comment);

                // when finished slurping in the zip, close the read stream
                //zf.ReadStream.Close();
            }
            catch (Exception e1)
            {
                if (zf._ReadStreamIsOurs && zf._readstream != null)
                {
                    try
                    {
                        zf._readstream.Close();
                        zf._readstream.Dispose();
                        zf._readstream = null;
                    }
                    finally { }
                }

                throw new Ionic.Utils.Zip.ZipException("Exception while reading", e1);
            }
        }


        private static void ReadCentralDirectoryFooter(ZipFile zf)
        {
            System.IO.Stream s = zf.ReadStream;
            int signature = Ionic.Utils.Zip.SharedUtilities.ReadSignature(s);

            // Throw if this is not a signature for "end of central directory record"
            // This is a sanity check.
            if (signature != ZipConstants.EndOfCentralDirectorySignature)
            {
                s.Seek(-4, System.IO.SeekOrigin.Current);
                throw new BadReadException(String.Format("  ZipFile::Read(): Bad signature ({0:X8}) at position 0x{1:X8}", signature, s.Position));
            }

            // read a bunch of throwaway metadata for supporting multi-disk archives (throwback!)
            // read the comment here
            byte[] block = new byte[16];
            zf.ReadStream.Read(block, 0, block.Length); // discard result

            ReadZipFileComment(zf);
        }

        private static void ReadZipFileComment(ZipFile zf)
        {
            // read the comment here
            byte[] block = new byte[2];
            zf.ReadStream.Read(block, 0, block.Length);

            Int16 commentLength = (short)(block[0] + block[1] * 256);
            if (commentLength > 0)
            {
                block = new byte[commentLength];
                zf.ReadStream.Read(block, 0, block.Length);
                // workitem 6415
                // use UTF if the caller hasn't already set a non-default encoding
                if (Ionic.Utils.Zip.SharedUtilities.HighBytes(block) && zf._encoding == System.Text.Encoding.GetEncoding("ibm437"))
                    zf._encoding = System.Text.Encoding.UTF8;
                zf.Comment = Ionic.Utils.Zip.SharedUtilities.StringFromBuffer(block, block.Length, zf._encoding);
            }
        }


        /// <summary>
        /// Generic IEnumerator support, for use of a ZipFile in a foreach construct.  
        /// </summary>
        ///
        /// <remarks>
        /// You probably do not want to call <c>GetEnumerator</c> explicitly. Instead 
        /// it is implicitly called when you use a <c>foreach</c> loop in C#, or a 
        /// <c>For Each</c> loop in VB.
        /// </remarks>
        ///
        /// <example>
        /// This example reads a zipfile of a given name, then enumerates the 
        /// entries in that zip file, and displays the information about each 
        /// entry on the Console.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(zipfile))
        /// {
        ///   bool header = true;
        ///   foreach (ZipEntry e in zip)
        ///   {
        ///     if (header)
        ///     {
        ///        System.Console.WriteLine("Zipfile: {0}", zip.Name);
        ///        System.Console.WriteLine("Version Needed: 0x{0:X2}", e.VersionNeeded);
        ///        System.Console.WriteLine("BitField: 0x{0:X2}", e.BitField);
        ///        System.Console.WriteLine("Compression Method: 0x{0:X2}", e.CompressionMethod);
        ///        System.Console.WriteLine("\n{1,-22} {2,-6} {3,4}   {4,-8}  {0}",
        ///                     "Filename", "Modified", "Size", "Ratio", "Packed");
        ///        System.Console.WriteLine(new System.String('-', 72));
        ///        header = false;
        ///     }
        ///
        ///     System.Console.WriteLine("{1,-22} {2,-6} {3,4:F0}%   {4,-8}  {0}",
        ///                 e.FileName,
        ///                 e.LastModified.ToString("yyyy-MM-dd HH:mm:ss"),
        ///                 e.UncompressedSize,
        ///                 e.CompressionRatio,
        ///                 e.CompressedSize);
        ///
        ///     e.Extract();
        ///   }
        /// }
        /// </code>
        ///
        /// <code lang="VB">
        ///   Dim ZipFileToExtract As String = "c:\foo.zip"
        ///   Using zip As ZipFile = ZipFile.Read(ZipFileToExtract)
        ///       Dim header As Boolean = True
        ///       Dim e As ZipEntry
        ///       For Each e In zip
        ///           If header Then
        ///               Console.WriteLine("Zipfile: {0}", zip.Name)
        ///               Console.WriteLine("Version Needed: 0x{0:X2}", e.VersionNeeded)
        ///               Console.WriteLine("BitField: 0x{0:X2}", e.BitField)
        ///               Console.WriteLine("Compression Method: 0x{0:X2}", e.CompressionMethod)
        ///               Console.WriteLine(ChrW(10) &amp; "{1,-22} {2,-6} {3,4}   {4,-8}  {0}", _
        ///                 "Filename", "Modified", "Size", "Ratio", "Packed" )
        ///               Console.WriteLine(New String("-"c, 72))
        ///               header = False
        ///           End If
        ///           Console.WriteLine("{1,-22} {2,-6} {3,4:F0}%   {4,-8}  {0}", _
        ///             e.FileName, _
        ///             e.LastModified.ToString("yyyy-MM-dd HH:mm:ss"), _
        ///             e.UncompressedSize, _
        ///             e.CompressionRatio, _
        ///             e.CompressedSize )
        ///           e.Extract
        ///       Next
        ///   End Using
        /// </code>
        /// </example>
        /// 
        /// <returns>A generic enumerator suitable for use  within a foreach loop.</returns>
        public System.Collections.Generic.IEnumerator<ZipEntry> GetEnumerator()
        {
            foreach (ZipEntry e in _entries)
                yield return e;
        }

        /// <summary>
        /// IEnumerator support, for use of a ZipFile in a foreach construct.  
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Extracts all of the items in the zip archive, to the specified path in the filesystem.
        /// The path can be relative or fully-qualified. 
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        /// If an extraction of a file from the zip archive would overwrite an existing file
        /// in the filesystem, the file will not be overwritten and an exception will be
        /// thrown. To avoid this, use the overload that allows you to specify that you want
        /// to overwrite existing files.
        /// </para>
        ///
        /// <para>
        /// This method will send verbose output messages to the StatusMessageTextWriter, if it 
        /// is set on the ZipFile instance. 
        /// </para>
        ///
        /// <para>
        /// You may also wish to take advantage of the <c>ExtractStarted</c>,
        /// <c>ExtractProgress</c>, and <c>ExtractCompleted</c> events.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.ExtractAll(string, bool)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.ExtractStarted"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.ExtractProgress"/>
        ///
        /// <param name="path">The path to which the contents of the zip archive should be extracted.</param>
        public void ExtractAll(string path)
        {
            ExtractAll(path, false);
        }

        /// <summary>
        /// Extracts all of the items in the zip archive, to the specified path in the filesystem,  
        /// optionally overwriting any existing files. The path can be relative or fully-qualified. 
        /// </summary>
        ///
        /// <remarks>
        /// This method will send verbose output messages to the StatusMessageTextWriter, if it 
        /// is set on the ZipFile instance. 
        /// </remarks>
        ///
        /// <example>
        /// This example extracts all the entries in a zip archive file, 
        /// to the specified target directory.  It overwrites any existing files.
        /// It also handles exceptions that may be thrown, such as unauthorized 
        /// access exceptions.
        /// <code>
        /// String TargetDirectory= "c:\\temp\\unpack";
        /// try 
        /// {
        ///   using(ZipFile zip= ZipFile.Read(ZipFileToExtract))
        ///   {
        ///     zip.ExtractAll(TargetDirectory, true);
        ///   }
        /// }
        /// catch (System.Exception ex1)
        /// {
        ///   System.Console.Error.WriteLine("exception: {0}", ex1);
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        ///   Dim TargetDirectory As String = "c:\temp\unpack"
        ///   Try 
        ///       Using zip As ZipFile = ZipFile.Read(ZipFileToExtract)
        ///           zip.ExtractAll(TargetDirectory, True)
        ///       End Using
        ///   Catch ex1 As Exception
        ///       Console.Error.WriteLine("exception: {0}", ex1.ToString)
        ///   End Try
        /// </code>
        /// </example>
        /// 
        /// <param name="path">the path to which the contents of the zipfile are extracted.</param>
        /// <param name="wantOverwrite">true to overwrite any existing files on extraction</param>
        public void ExtractAll(string path, bool wantOverwrite)
        {
            bool header = Verbose;

            OnExtractStarted(path, wantOverwrite);

            int n = 0;
            foreach (ZipEntry e in _entries)
            {
                if (header)
                {
                    StatusMessageTextWriter.WriteLine("\n{1,-22} {2,-8} {3,4}   {4,-8}  {0}",
                              "Name", "Modified", "Size", "Ratio", "Packed");
                    StatusMessageTextWriter.WriteLine(new System.String('-', 72));
                    header = false;
                }
                if (Verbose)
                {
                    StatusMessageTextWriter.WriteLine("{1,-22} {2,-8} {3,4:F0}%   {4,-8} {0}",
                              e.FileName,
                              e.LastModified.ToString("yyyy-MM-dd HH:mm:ss"),
                              e.UncompressedSize,
                              e.CompressionRatio,
                              e.CompressedSize);
                    if (!String.IsNullOrEmpty(e.Comment))
                        StatusMessageTextWriter.WriteLine("  Comment: {0}", e.Comment);
                }
                e.Password = _Password;  // this may be null
                e.Extract(path, wantOverwrite);
                n++;
                OnExtractProgress(n, e.FileName, path, wantOverwrite);
                if (_extractOperationCanceled)
                    break;

            }
            OnExtractCompleted(path, wantOverwrite);
        }


        /// <summary>
        /// Extract a single item from the archive.  The file, including any relative
        /// qualifying path, is created at the current working directory.  
        /// </summary>
        /// 
        /// <remarks>
        /// Calling this method, the entry is extracted using the Password that is 
        /// specified on the ZipFile instance. If you have not set the Password property, then
        /// the password is null, and the entry is extracted with no password.
        /// </remarks>
        /// 
        /// <param name="fileName">
        /// The file to extract. It must be the exact filename, including the path
        /// contained in the archive, if any. The filename match is not case-sensitive by
        /// default; you can use the <c>CaseSensitiveRetrieval</c> property to change
        /// this behavior.
        /// </param>
        public void Extract(string fileName)
        {
            ZipEntry e = this[fileName];
            e.Password = _Password; // possibly null
            e.Extract();
        }

        /// <summary>
        /// Extract a single item from the archive.  The file, including any relative
        /// qualifying path, is created at the current working directory.  
        /// </summary>
        ///
        /// <remarks>
        /// Calling this method, the entry is extracted using the Password that is 
        /// specified on the ZipFile instance. If you have not set the Password property, then
        /// the password is null, and the entry is extracted with no password.
        /// </remarks>
        /// 
        /// <param name="fileName">
        /// the file to extract. It must be the exact filename, including the path
        /// contained in the archive, if any. The filename match is not case-sensitive by
        /// default; you can use the <c>CaseSensitiveRetrieval</c> property to change
        /// this behavior.
        /// </param>
        /// <param name="directoryName">the directory into which to extract. It should exist.</param>
        public void Extract(string fileName, string directoryName)
        {
            ZipEntry e = this[fileName];
            e.Password = _Password; // possibly null
            e.Extract(directoryName);
        }


        /// <summary>
        /// Extract a single item from the archive, potentially overwriting  
        /// any existing file in the filesystem by the same name. The file, including any relative 
        /// qualifying path, is created at the current working directory.  
        /// </summary>
        /// 
        /// <remarks>
        /// Calling this method, the entry is extracted using the Password that is 
        /// specified on the ZipFile instance. If you have not set the Password property, then
        /// the password is null, and the entry is extracted with no password.
        /// </remarks>
        /// 
        /// <param name="fileName">
        /// The file to extract. It must be the exact filename, including the path contained in the 
        /// archive, if any. The filename match is not case-sensitive by default; you can use the <c>CaseSensitiveRetrieval</c> 
        /// property to change this behavior.
        /// The pathname can use forward-slashes or backward slashes.
        /// </param>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.CaseSensitiveRetrieval"/>
        ///
        /// <param name="wantOverwrite">True if the caller wants to overwrite any existing files by the given name.</param>
        public void Extract(string fileName, bool wantOverwrite)
        {
            ZipEntry e = this[fileName];
            e.Password = _Password; // possibly null
            e.Extract(wantOverwrite);
        }


        /// <summary>
        /// Extract a single item from the archive, into the specified directory, potentially overwriting  
        /// any existing file in the filesystem by the same name. The file, including any relative 
        /// qualifying path, is created in the specified directory.  
        /// </summary>
        /// 
        /// <remarks>
        /// Calling this method, the entry is extracted using the Password that is 
        /// specified on the ZipFile instance. If you have not set the Password property, then
        /// the password is null, and the entry is extracted with no password.
        /// </remarks>
        /// 
        /// <param name="fileName">
        /// The file to extract. It must be the exact filename, including the path contained in the archive, 
        /// if any. The filename match is not case-sensitive by default; you can use the <c>CaseSensitiveRetrieval</c> 
        /// property to change this behavior. The pathname can use forward-slashes or backward slashes.
        /// </param>
        /// <param name="directoryName">the directory into which to extract. It should exist.</param>
        /// <param name="wantOverwrite">True if the caller wants to overwrite any existing files by the given name.</param>
        public void Extract(string fileName, string directoryName, bool wantOverwrite)
        {
            ZipEntry e = this[fileName];
            e.Password = _Password; // possibly null
            e.Extract(directoryName, wantOverwrite);
        }




        /// <summary>
        /// Extract a single specified file from the archive, to the given stream.  This is 
        /// useful when extracting to Console.Out or to a memory stream, for example. 
        /// </summary>
        /// 
        /// <remarks>
        /// Calling this method, the entry is extracted using the Password that is 
        /// specified on the ZipFile instance. If you have not set the Password property, then
        /// the password is null, and the entry is extracted with no password.
        /// </remarks>
        /// 
        /// <exception cref="Ionic.Utils.Zip.ZipException">
        /// Thrown if the outputStream is not writable, or if the filename is 
        /// null or empty. The inner exception is an ArgumentException in each case.
        /// </exception>
        ///
        /// <param name="fileName">
        /// the file to extract. It should include pathnames used in the archive, if any.
        /// The filename match is not case-sensitive by default; you can use the
        /// <c>CaseSensitiveRetrieval</c> property to change this behavior.The
        /// application can specify pathnames using forward-slashes or backward slashes.
        /// </param>
        ///
        /// <param name="outputStream">
        /// the stream to which the extacted, decompressed file data is written. 
        /// The stream must be writable.
        /// </param>
        public void Extract(string fileName, System.IO.Stream outputStream)
        {
            if (outputStream == null || !outputStream.CanWrite)
                throw new ZipException("Cannot extract.", new ArgumentException("The OutputStream must be a writable stream.", "outputStream"));

            if (String.IsNullOrEmpty(fileName))
                throw new ZipException("Cannot extract.", new ArgumentException("The file name must be neither null nor empty.", "fileName"));

            ZipEntry e = this[fileName];
            e.Password = _Password; // possibly null
            e.Extract(outputStream);
        }


        /// <summary>
        /// This is an integer indexer into the Zip archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This property is read-write. But don't get too excited: When setting the value, the
        /// only legal value is null. If you assign a non-null value
        /// (non Nothing in VB), the setter will throw an exception.
        /// </para>
        /// <para>
        /// Setting the value to null is equivalent to calling <c>ZipFile.RemoveEntry</c> 
        /// with the filename for the given entry.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="System.ArgumentException">
        /// Thrown if the caller attempts to assign a non-null value to the indexer, 
        /// or if the caller uses an out-of-range index value.
        /// </exception>
        ///
        /// <param name="ix">
        /// The index value.
        /// </param>
        /// 
        /// <returns>
        /// The ZipEntry within the Zip archive at the specified index. If the 
        /// entry does not exist in the archive, this indexer throws.
        /// </returns>
        /// 
        public ZipEntry this[int ix]
        {
            // workitem 6402
            get
            {
#if NONONO
                if (_JustSaved)
                {
                    // work item 5593
                    // read in the just-saved zip archive                    
                    ReadIntoInstance(this);
                    _JustSaved = false;
                }
#endif

                return _entries[ix];
            }

            set
            {
                if (value != null)
                    throw new ArgumentException("You may not set this to a non-null ZipEntry value.");
                RemoveEntry(_entries[ix]);
            }
        }


        /// <summary>
        /// This is a name-based indexer into the Zip archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// Retrieval by the string-based indexer is done on a case-insensitive basis, 
        /// by default.  Set the <see cref="CaseSensitiveRetrieval"/> property to use case-sensitive 
        /// comparisons. 
        /// </para>
        /// <para>
        /// This property is read-write. When setting the value, the
        /// only legal value is null. If you assign a non-null value
        /// (non Nothing in VB), the setter will throw an exception.
        /// </para>
        /// <para>
        /// Setting the value to null is equivalent to calling <c>ZipFile.RemoveEntry</c> 
        /// with the filename.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// This example extracts only the entries in a zip file that are .txt files.
        /// <code>
        /// using (ZipFile zip = ZipFile.Read("PackedDocuments.zip"))
        /// {
        ///   foreach (string s1 in zip.EntryFilenames)
        ///   {
        ///     if (s1.EndsWith(".txt"))
        ///       zip[s1].Extract("textfiles");
        ///   }
        /// }
        /// </code>
        /// <code lang="VB">
        ///   Using zip As ZipFile = ZipFile.Read("PackedDocuments.zip")
        ///       Dim s1 As String
        ///       For Each s1 In zip.EntryFilenames
        ///           If s1.EndsWith(".txt") Then
        ///               zip(s1).Extract("textfiles")
        ///           End If
        ///       Next
        ///   End Using
        /// </code>
        /// </example>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.RemoveEntry(string)"/>
        ///
        /// <exception cref="System.ArgumentException">
        /// Thrown if the caller attempts to assign a non-null value to the indexer.
        /// </exception>
        ///
        /// <param name="fileName">
        /// The name of the file, including any directory path, to retrieve from the zip. 
        /// The filename match is not case-sensitive by default; you can use the
        /// <c>CaseSensitiveRetrieval</c> property to change this behavior. The
        /// pathname can use forward-slashes or backward slashes.
        /// </param>
        /// 
        /// <returns>
        /// The ZipEntry within the Zip archive, given by the specified filename. If the named
        /// entry does not exist in the archive, this indexer returns null.
        /// </returns>
        /// 
        public ZipEntry this[String fileName]
        {
            get
            {
#if NONONO
                if (_JustSaved)
                {
                    // work item 5593
                    // read in the just-saved zip archive                    
                    ReadIntoInstance(this);
                    _JustSaved = false;
                }
#endif
                foreach (ZipEntry e in _entries)
                {
                    if (this.CaseSensitiveRetrieval)
                    {
                        // check for the file match with a case-sensitive comparison.
                        if (e.FileName == fileName) return e;
                        // also check for equivalence
                        if (fileName.Replace("\\", "/") == e.FileName) return e;
                        if (e.FileName.Replace("\\", "/") == fileName) return e;
                    }
                    else
                    {
                        // check for the file match in a case-insensitive manner.
                        if (String.Compare(e.FileName, fileName, StringComparison.CurrentCultureIgnoreCase) == 0) return e;
                        // also check for equivalence
                        if (String.Compare(fileName.Replace("\\", "/"), e.FileName, StringComparison.CurrentCultureIgnoreCase) == 0) return e;
                        if (String.Compare(e.FileName.Replace("\\", "/"), fileName, StringComparison.CurrentCultureIgnoreCase) == 0) return e;
                    }

                }
                return null;
            }

            set
            {
                if (value != null)
                    throw new ArgumentException("You may not set this to a non-null ZipEntry value.");
                RemoveEntry(fileName);
            }
        }

        /// <summary>
        /// The list of filenames for the entries contained within the zip archive.  The 
        /// filenames use forward slashes in pathnames. 
        /// </summary>
        ///
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.this[string]"/>
        ///
        /// <example>
        /// This example shows one way to test if a filename is already contained within 
        /// a zip archive.
        /// <code>
        /// String ZipFileToRead= "PackedDocuments.zip";
        /// string Candidate = "DatedMaterial.xps";
        /// using (ZipFile zip = new ZipFile(ZipFileToRead))
        /// {
        ///   if (zip.EntryFilenames.Contains(Candidate))
        ///     Console.WriteLine("The file '{0}' exists in the zip archive '{1}'",
        ///                       Candidate,
        ///                       ZipFileName);
        ///   else
        ///     Console.WriteLine("The file, '{0}', does not exist in the zip archive '{1}'",
        ///                       Candidate,
        ///                       ZipFileName);
        ///   Console.WriteLine();
        /// }
        /// </code>
        /// <code lang="VB">
        ///   Dim ZipFileToRead As String = "PackedDocuments.zip"
        ///   Dim Candidate As String = "DatedMaterial.xps"
        ///   Using zip As New ZipFile(ZipFileToRead)
        ///       If zip.EntryFilenames.Contains(Candidate) Then
        ///           Console.WriteLine("The file '{0}' exists in the zip archive '{1}'", _
        ///                       Candidate, _
        ///                       ZipFileName)
        ///       Else
        ///         Console.WriteLine("The file, '{0}', does not exist in the zip archive '{1}'", _
        ///                       Candidate, _
        ///                       ZipFileName)
        ///       End If
        ///       Console.WriteLine
        ///   End Using
        /// </code>
        /// </example>
        ///
        /// <returns>
        /// The list of strings for the filenames contained within the Zip archive.
        /// </returns>
        /// 
        public System.Collections.ObjectModel.ReadOnlyCollection<string> EntryFileNames
        {
            get
            {
                var foo = _entries.ConvertAll((e) => { return e.FileName; });
                return foo.AsReadOnly();
            }
        }



        /// <summary>
        /// Removes the given ZipEntry from the zip archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// After calling <c>RemoveEntry</c>, the application must call <c>Save</c> to make the 
        /// changes permanent.  
        /// </para>
        /// </remarks>
        ///
        /// <exception cref="System.ArgumentException">
        /// Thrown if the specified ZipEntry does not exist in the ZipFile.
        /// </exception>
        ///
        /// <example>
        /// In this example, all entries in the zip archive dating from before December 31st, 2007, are
        /// removed from the archive.
        /// <code>
        /// System.DateTime Threshold = new System.DateTime(2007,12,31);
        /// using (ZipFile zip = new ZipFile(ZipFileToRead))
        /// {
        ///   var EntriesToRemove = new System.Collections.Generic.List&lt;ZipEntry&gt;();
        ///   foreach (ZipEntry e in zip)
        ///   {
        ///     if (e.LastModified &lt; Threshold)
        ///     {
        ///       // We cannot remove the entry from the list, within the context of 
        ///       // an enumeration of said list.
        ///       // So we add the doomed entry to a list to be removed later.
        ///       EntriesToRemove.Add(e);
        ///     }
        ///   }
        ///   
        ///   // actually remove the doomed entries. 
        ///   foreach (ZipEntry zombie in EntriesToRemove)
        ///     zip.RemoveEntry(zombie);
        ///   
        ///   zip.Comment= String.Format("This zip archive was updated at {0}.", 
        ///                              System.DateTime.Now.ToString("G"));
        ///   zip.Save();
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        ///   Dim ZipFileToRead As String = "c:\foo.zip"
        ///   Dim Threshold As New DateTime(2007, 12, 31)
        ///   Using zip As ZipFile = New ZipFile(ZipFileToRead)
        ///       Dim EntriesToRemove As New System.Collections.Generic.List(Of ZipEntry)
        ///       Dim e As ZipEntry
        ///       For Each e In zip
        ///           If (e.LastModified &lt; Threshold) Then
        ///               ' We cannot remove the entry from the list, within the context of 
        ///               ' an enumeration of said list.
        ///               ' So we add the doomed entry to a list to be removed later.
        ///               EntriesToRemove.Add(e)
        ///           End If
        ///       Next
        ///   
        ///       ' actually remove the doomed entries. 
        ///       Dim zombie As ZipEntry
        ///       For Each zombie In EntriesToRemove
        ///           zip.RemoveEntry(zombie)
        ///       Next
        ///       zip.Comment = String.Format("This zip archive was updated at {0}.", DateTime.Now.ToString("G"))
        ///       zip.Save
        ///   End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="entry">
        /// The ZipEntry to remove from the zip. 
        /// </param>
        /// 
        public void RemoveEntry(ZipEntry entry)
        {
            if (!_entries.Contains(entry))
                throw new ArgumentException("The entry you specified does not exist in the zip archive.");

#if NONONO	    
            if (_JustSaved)
            {
		// In this case the _direntries struct is not filled. 
		// 
                // work item 5593
                // read in the just-saved zip archive 
		ZipFile x = new ZipFile();
                ReadIntoInstance(x);

                _JustSaved = false;
            }
#endif

            _entries.Remove(entry);

            if (_direntries != null)
            {
                bool FoundAndRemovedDirEntry = false;
                foreach (ZipDirEntry de1 in _direntries)
                {
                    if (entry.FileName == de1.FileName)
                    {
                        _direntries.Remove(de1);
                        FoundAndRemovedDirEntry = true;
                        break;
                    }
                }

                if (!FoundAndRemovedDirEntry)
                    throw new BadStateException("The entry to be removed was not found in the directory.");
            }

            _contentsChanged = true;
        }




        /// <summary>
        /// Removes the ZipEntry with the given filename from the zip archive.  
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// After calling <c>RemoveEntry</c>, the application must call <c>Save</c> to make the changes permanent.  
        /// </para>
        ///
        /// </remarks>
        ///
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the ZipFile is not updatable. 
        /// </exception>
        ///
        /// <exception cref="System.ArgumentException">
        /// Thrown if a ZipEntry with the specified filename does not exist in the ZipFile.
        /// </exception>
        ///
        /// <example>
        /// This example shows one way to remove an entry with a given filename from an 
        /// existing zip archive.
        /// <code>
        /// String ZipFileToRead= "PackedDocuments.zip";
        /// string Candidate = "DatedMaterial.xps";
        /// using (ZipFile zip = new ZipFile(ZipFileToRead))
        /// {
        ///   if (zip.EntryFilenames.Contains(Candidate))
        ///   {
        ///     zip.RemoveEntry(Candidate);
        ///     zip.Comment= String.Format("The file '{0}' has been removed from this archive.", 
        ///                                Candidate);
        ///     zip.Save();
        ///   }
        /// }
        /// </code>
        /// <code lang="VB">
        ///   Dim ZipFileToRead As String = "PackedDocuments.zip"
        ///   Dim Candidate As String = "DatedMaterial.xps"
        ///   Using zip As ZipFile = New ZipFile(ZipFileToRead)
        ///       If zip.EntryFilenames.Contains(Candidate) Then
        ///           zip.RemoveEntry(Candidate)
        ///           zip.Comment = String.Format("The file '{0}' has been removed from this archive.", Candidate)
        ///           zip.Save
        ///       End If
        ///   End Using
        /// </code>
        /// </example>
        /// 
        /// <param name="fileName">
        /// The name of the file, including any directory path, to remove from the zip. 
        /// The filename match is not case-sensitive by default; you can use the
        /// <c>CaseSensitiveRetrieval</c> property to change this behavior. The
        /// pathname can use forward-slashes or backward slashes.
        /// </param>
        /// 
        public void RemoveEntry(String fileName)
        {
            string modifiedName = ZipEntry.NameInArchive(fileName, null);
            ZipEntry e = this[modifiedName];
            if (e == null)
                throw new ArgumentException("The entry you specified was not found in the zip archive.");

            RemoveEntry(e);
        }


        #endregion

        #region Destructors and Disposers

        /// <summary>
        /// This is the class Destructor, which gets called implicitly when the instance is destroyed.  
        /// Because the ZipFile type implements IDisposable, this method calls Dispose(false).  
        /// </summary>
        ~ZipFile()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            Dispose(false);
        }

        /// <summary>
        /// Handles closing of the read and write streams associated
        /// to the ZipFile, if necessary.  The Dispose() method is generally 
        /// employed implicitly, via a using() {} statement. 
        /// </summary>
        /// <example>
        /// <code>
        /// using (ZipFile zip = ZipFile.Read(zipfile))
        /// {
        ///   foreach (ZipEntry e in zip)
        ///   {
        ///     if (WantThisEntry(e.FileName)) 
        ///       zip.Extract(e.FileName, Console.OpenStandardOutput());
        ///   }
        /// } // Dispose() is called implicitly here.
        /// </code>
        /// </example>
        public void Dispose()
        {
            // dispose of the managed and unmanaged resources
            Dispose(true);

            // tell the GC that the Finalize process no longer needs
            // to be run for this object.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The Dispose() method.  It disposes any managed resources, 
        /// if the flag is set, then marks the instance disposed.
        /// This method is typically not called from application code.
        /// </summary>
        /// <param name="disposeManagedResources">indicates whether the method should dispose streams or not.</param>
        protected virtual void Dispose(bool disposeManagedResources)
        {
            if (!this._disposed)
            {
                if (disposeManagedResources)
                {
                    // dispose managed resources
                    if (_ReadStreamIsOurs)
                    {
                        if (_readstream != null)
                        {
                            _readstream.Dispose();
                            _readstream = null;
                        }
                    }
                    // only dispose the writestream if there is a backing file 
                    //(_temporaryFileName is not null)
                    if ((_temporaryFileName != null) && (_name != null))
                        if (_writestream != null)
                        {
                            _writestream.Dispose();
                            _writestream = null;
                        }
                }
                this._disposed = true;
            }
        }
        #endregion

        #region private properties

        private System.IO.Stream WriteStream
        {
            get
            {
                if (_writestream == null)
                {
                    if (_name != null)
                    {
                        _temporaryFileName = (TempFileFolder != ".") ?
                System.IO.Path.Combine(TempFileFolder, System.IO.Path.GetRandomFileName())
                : System.IO.Path.GetRandomFileName();
                        _writestream = new System.IO.FileStream(_temporaryFileName, System.IO.FileMode.CreateNew);
                    }
                }
                return _writestream;
            }
            set
            {
                if (value != null)
                    throw new ZipException("Whoa!", new ArgumentException("Cannot set the stream to a non-null value.", "value"));
                _writestream = null;
            }
        }
        #endregion

        #region private fields
        private System.IO.TextWriter _StatusMessageTextWriter;
        private bool _CaseSensitiveRetrieval;
        private System.IO.Stream _readstream;
        private System.IO.Stream _writestream;
        private bool _disposed;
        private System.Collections.Generic.List<ZipEntry> _entries;
        private System.Collections.Generic.List<ZipDirEntry> _direntries;
        private bool _TrimVolumeFromFullyQualifiedPaths = true;
        private bool _ForceNoCompression;
        private string _name;
        private string _Comment;
        private string _Password;
        private bool _fileAlreadyExists;
        private string _temporaryFileName;
        private bool _contentsChanged;
        private String _TempFileFolder;
        private bool _ReadStreamIsOurs = true;
        private object LOCK = new object();
        private bool _saveOperationCanceled;
        private bool _extractOperationCanceled;
        private bool _JustSaved;
        private System.Text.Encoding _encoding = System.Text.Encoding.GetEncoding("IBM437"); // default = IBM437
        #endregion
    }


    enum AddOrUpdateAction
    {
        AddOnly = 0,
        AddOrUpdate
    }
}


// Example usage: 
// 1. Extracting all files from a Zip file: 
//
//     try 
//     {
//       using(ZipFile zip= ZipFile.Read(ZipFile))
//       {
//         zip.ExtractAll(TargetDirectory, true);
//       }
//     }
//     catch (System.Exception ex1)
//     {
//       System.Console.Error.WriteLine("exception: " + ex1);
//     }
//
// 2. Extracting files from a zip individually:
//
//     try 
//     {
//       using(ZipFile zip= ZipFile.Read(ZipFile)) 
//       {
//         foreach (ZipEntry e in zip) 
//         {
//           e.Extract(TargetDirectory);
//         }
//       }
//     }
//     catch (System.Exception ex1)
//     {
//       System.Console.Error.WriteLine("exception: " + ex1);
//     }
//
// 3. Creating a zip archive: 
//
//     try 
//     {
//       using(ZipFile zip= new ZipFile(NewZipFile)) 
//       {
//
//         String[] filenames= System.IO.Directory.GetFiles(Directory); 
//         foreach (String filename in filenames) 
//         {
//           zip.Add(filename);
//         }
//
//         zip.Save(); 
//       }
//
//     }
//     catch (System.Exception ex1)
//     {
//       System.Console.Error.WriteLine("exception: " + ex1);
//     }
//
//
// ==================================================================
//
//
//
// Information on the ZIP format:
//
// From
// http://www.pkware.com/documents/casestudies/APPNOTE.TXT
//
//  Overall .ZIP file format:
//
//     [local file header 1]
//     [file data 1]
//     [data descriptor 1]  ** sometimes
//     . 
//     .
//     .
//     [local file header n]
//     [file data n]
//     [data descriptor n]   ** sometimes
//     [archive decryption header] 
//     [archive extra data record] 
//     [central directory]
//     [zip64 end of central directory record]
//     [zip64 end of central directory locator] 
//     [end of central directory record]
//
// Local File Header format:
//         local file header signature ... 4 bytes  (0x04034b50)
//         version needed to extract ..... 2 bytes
//         general purpose bit field ..... 2 bytes
//         compression method ............ 2 bytes
//         last mod file time ............ 2 bytes
//         last mod file date............. 2 bytes
//         crc-32 ........................ 4 bytes
//         compressed size................ 4 bytes
//         uncompressed size.............. 4 bytes
//         file name length............... 2 bytes
//         extra field length ............ 2 bytes
//         file name                       varies
//         extra field                     varies
//
//
// Data descriptor:  (used only when bit 3 of the general purpose bitfield is set)
//         (although, I have found zip files where bit 3 is not set, yet this descriptor is present!)
//         local file header signature     4 bytes  (0x08074b50)  ** sometimes!!! Not always
//         crc-32                          4 bytes
//         compressed size                 4 bytes
//         uncompressed size               4 bytes
//
//
//   Central directory structure:
//
//       [file header 1]
//       .
//       .
//       . 
//       [file header n]
//       [digital signature] 
//
//
//       File header:  (This is a ZipDirEntry)
//         central file header signature   4 bytes  (0x02014b50)
//         version made by                 2 bytes
//         version needed to extract       2 bytes
//         general purpose bit flag        2 bytes
//         compression method              2 bytes
//         last mod file time              2 bytes
//         last mod file date              2 bytes
//         crc-32                          4 bytes
//         compressed size                 4 bytes
//         uncompressed size               4 bytes
//         file name length                2 bytes
//         extra field length              2 bytes
//         file comment length             2 bytes
//         disk number start               2 bytes
//         internal file attributes **     2 bytes
//         external file attributes ***    4 bytes
//         relative offset of local header 4 bytes
//         file name (variable size)
//         extra field (variable size)
//         file comment (variable size)
//
// ** The internal file attributes, near as I can tell, 
// uses 0x01 for a file and a 0x00 for a directory. 
//
// ***The external file attributes follows the MS-DOS file attribute byte, described here:
// at http://support.microsoft.com/kb/q125019/
// 0x0010 => directory
// 0x0020 => file 
//
//
// End of central directory record:
//
//         end of central dir signature    4 bytes  (0x06054b50)
//         number of this disk             2 bytes
//         number of the disk with the
//         start of the central directory  2 bytes
//         total number of entries in the
//         central directory on this disk  2 bytes
//         total number of entries in
//         the central directory           2 bytes
//         size of the central directory   4 bytes
//         offset of start of central
//         directory with respect to
//         the starting disk number        4 bytes
//         .ZIP file comment length        2 bytes
//         .ZIP file comment       (variable size)
//
// date and time are packed values, as MSDOS did them
// time: bits 0-4 : seconds (divided by 2)
//            5-10: minute
//            11-15: hour
// date  bits 0-4 : day
//            5-8: month
//            9-15 year (since 1980)
//
// see http://msdn.microsoft.com/en-us/library/ms724274(VS.85).aspx

