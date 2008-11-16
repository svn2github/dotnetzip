// ZipEntry.cs
//
// Copyright (c) 2006, 2007, 2008 Microsoft Corporation.  All rights reserved.
//
// Part of an implementation of a zipfile class library. 
// See the file ZipFile.cs for the license and for further information.
//
// Created: Tue, 27 Mar 2007  15:30
// 

using System;
using System.IO;
using System.IO.Compression;
using RE = System.Text.RegularExpressions;

namespace Ionic.Utils.Zip
{
    /// <summary>
    /// An enum that provides the various encryption algorithms supported by this library.
    /// </summary>
    public enum EncryptionAlgorithm
    {
        /// <summary>
        /// No encryption at all.
        /// </summary>
        None = 0,

        /// <summary>
        /// Traditional or Classic pkzip encryption.
        /// </summary>
        PkzipWeak,
        //AES128, AES192, AES256, etc  // not implemented (yet?)
    }

    /// <summary>
    /// An enum that specifies the source of the ZipEntry. 
    /// </summary>
    internal enum EntrySource
    {
        /// <summary>
        /// Default value.  Invalid on a bonafide ZipEntry.
        /// </summary>
        None = 0,

        /// <summary>
        /// Entry was instantiated by Adding an entry from the filesystem.
        /// </summary>
        Filesystem,

        /// <summary>
        /// Entry was instantiated by reading a zipfile.
        /// </summary>
        Zipfile,

        /// <summary>
        /// Entry was instantiated via a stream or string.
        /// </summary>
        Stream,
    }


    /// <summary>
    /// Represents a single entry in a ZipFile. Typically, applications
    /// get a ZipEntry by enumerating the entries within a ZipFile,
    /// or by adding an entry to a ZipFile.  
    /// </summary>
    public class ZipEntry
    {
        private ZipEntry() { }

        /// <summary>
        /// The time and date at which the file indicated by the ZipEntry was last modified. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The DotNetZip library sets the LastModified value for an entry, equal to the 
        /// Last Modified time of the file in the filesystem.  If an entry is added from a stream, 
        /// in which case no Last Modified attribute is available, the library uses 
        /// <c>System.DateTime.Now</c> for this value, for the given entry. 
        /// </para>
        ///
        /// <para>
        /// It is also possible to set the LastModified value on an entry, to an arbitrary
        /// value.  Be aware that because of the way the PKZip specification describes how
        /// times are stored in the zip file, the full precision of the
        /// <c>System.DateTime</c> datatype is not stored in LastModified when saving zip
        /// files.  For more information on how times are formatted, see the PKZip
        /// specification.
        /// </para>
        ///
        /// <para>
        /// The last modified time of the file created upon a call to <c>ZipEntry.Extract()</c> 
        /// may be adjusted during extraction to compensate
        /// for differences in how the .NET Base Class Library deals
        /// with daylight saving time (DST) versus how the Windows
        /// filesystem deals with daylight saving time. 
        /// See http://blogs.msdn.com/oldnewthing/archive/2003/10/24/55413.aspx for more context.
        /// </para>
        /// <para>
        /// In a nutshell: Daylight savings time rules change regularly.  In
        /// 2007, for example, the inception week of DST changed.  In 1977,
        /// DST was in place all year round. In 1945, likewise.  And so on.
        /// Win32 does not attempt to guess which time zone rules were in
        /// effect at the time in question.  It will render a time as
        /// "standard time" and allow the app to change to DST as necessary.
        ///  .NET makes a different choice.
        /// </para>
        /// <para>
        /// Compare the output of FileInfo.LastWriteTime.ToString("f") with
        /// what you see in the property sheet for a file that was last
        /// written to on the other side of the DST transition. For example,
        /// suppose the file was last modified on October 17, 2003, during DST but
        /// DST is not currently in effect. Explorer's file properties
        /// reports Thursday, October 17, 2003, 8:45:38 AM, but .NETs
        /// FileInfo reports Thursday, October 17, 2003, 9:45 AM.
        /// </para>
        /// <para>
        /// Win32 says, "Thursday, October 17, 2002 8:45:38 AM PST". Note:
        /// Pacific STANDARD Time. Even though October 17 of that year
        /// occurred during Pacific Daylight Time, Win32 displays the time as
        /// standard time because that's what time it is NOW.
        /// </para>
        /// <para>
        /// .NET BCL assumes that the current DST rules were in place at the
        /// time in question.  So, .NET says, "Well, if the rules in effect
        /// now were also in effect on October 17, 2003, then that would be
        /// daylight time" so it displays "Thursday, October 17, 2003, 9:45
        /// AM PDT" - daylight time.
        /// </para>
        /// <para>
        /// So .NET gives a value which is more intuitively correct, but is
        /// also potentially incorrect, and which is not invertible. Win32
        /// gives a value which is intuitively incorrect, but is strictly
        /// correct.
        /// </para>
        /// <para>
        /// Because of this funkiness, this library adds one hour to the LastModified time
        /// on the extracted file, if necessary.  That is to say, if the time in question
        /// had occurred in what the .NET Base Class Library assumed to be DST (an
        /// assumption that may be wrong given the constantly changing DST rules).
        /// </para>
        /// </remarks>
        ///
        public DateTime LastModified
        {
            get { return _LastModified; }
            set
            {
                _LastModified = value;
                //SetLastModDateTimeWithAdjustment(this);
            }
        }

        /// <summary>
        /// When this is set, this class trims the volume (eg C:\) from any
        /// fully-qualified pathname on the ZipEntry, before writing the ZipEntry into
        /// the ZipFile. This flag affects only zip creation. By default, this flag is TRUE,
        /// which means volume names will not be included in the filenames on entries in
        /// the archive.  Your best bet is to just leave this alone.
        /// </summary>
        public bool TrimVolumeFromFullyQualifiedPaths
        {
            get { return _TrimVolumeFromFullyQualifiedPaths; }
            set { _TrimVolumeFromFullyQualifiedPaths = value; }
        }

        /// <summary>
        /// When this is set, the entry is not compressed when written to 
        /// the archive.  For example, the application might want to set flag to True
        /// this when zipping up JPG or MP3 files, which are already compressed.
        /// </summary>
        public bool ForceNoCompression
        {
            get { return _ForceNoCompression; }
            set { _ForceNoCompression = value; }
        }


        /// <summary>
        /// The name of the filesystem file, referred to by the ZipEntry. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This may be different than the path used in the archive itself. What I mean is, 
        /// if you call Zip.AddFile("fooo.txt"", AlternativeDirectory), then the 
        /// path used in the zip entry will be different than this path.  This path is 
        /// used to locate the thing-to-be-zipped on disk. 
        /// </para>
        /// <para>
        /// If the entry is being added from a stream, then this is null (Nothing in VB).
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="FileName"/>
        public string LocalFileName
        {
            get { return _LocalFileName; }
        }

        /// <summary>
        /// The name of the file contained in the ZipEntry. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// When writing a zip, this path has backslashes replaced with 
        /// forward slashes, according to the zip spec, for compatibility
        /// with Unix(tm) and ... get this.... Amiga!
        /// </para>
        ///
        /// <para>
        /// This is the name of the entry in the ZipFile itself.  This name may be different
        /// than the name of the filesystem file used to create the entry. In fact, there
        /// may be no filesystem file at all, if the entry is created from a stream or a string.
        /// </para>
        /// </remarks>
        /// <seealso cref="LocalFileName"/>
        public string FileName
        {
            get { return _FileNameInArchive; }
        }

        /// <summary>
        /// The version of the zip engine needed to read the ZipEntry.  This is usually 0x14. 
        /// (Decimal 20).
        /// </summary>
        public Int16 VersionNeeded
        {
            get { return _VersionNeeded; }
        }

        /// <summary>
        /// The comment attached to the ZipEntry. 
        /// </summary>
        public string Comment
        {
            get { return _Comment; }
            set { _Comment = value; }
        }

        /// <summary>
        /// The bitfield as defined in the zip spec. In the current implementation, the
        /// only thing this library potentially writes to the general purpose
        /// Bitfield is an encryption indicators.
        /// </summary>
        /// <code>
        /// bit  0 - set if encryption is used.
        /// b. 1-2 - set to determine whether normal, max, fast deflation.  
        ///          This library always leaves these bits unset when writing (indicating 
        ///          "normal" deflation").
        ///
        /// bit  3 - indicates crc32, compressed and uncompressed sizes are zero in
        ///          local header.  We always leave this as zero on writing, but can read
        ///          a zip with it nonzero. 
        ///
        /// bit  4 - reserved for "enhanced deflating". This library doesn't do enhanced deflating.
        /// bit  5 - set to indicate the zip is compressed patched data.  This library doesn't do that.
        /// bit  6 - set if strong encryption is used (must also set bit 1 if bit 6 is set)
        /// bit  7 - unused
        /// bit  8 - unused
        /// bit  9 - unused
        /// bit 10 - unused
        /// Bit 11 - Language encoding flag (EFS).  If this bit is set,
        ///          the filename and comment fields for this file
        ///          must be encoded using UTF-8. This library currently does not support UTF-8.
        /// Bit 12 - Reserved by PKWARE for enhanced compression.
        /// Bit 13 - Used when encrypting the Central Directory to indicate 
        ///          selected data values in the Local Header are masked to
        ///          hide their actual values.  See the section describing 
        ///          the Strong Encryption Specification for details.
        /// Bit 14 - Reserved by PKWARE.
        /// Bit 15 - Reserved by PKWARE.
        /// </code>

        public Int16 BitField
        {
            get { return _BitField; }
        }

        /// <summary>
        /// The compression method employed for this ZipEntry. 0x08 = Deflate.  0x00 =
        /// Store (no compression).  Really, this should be an enum.  But the zip spec
        /// makes it a byte. So here it is. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>When reading an entry from an existing zipfile, the value you retrieve here
        /// indicates the compression method used on the entry by the original creator of the zip.  
        /// When writing a zipfile, you can specify either 0x08 (Deflate) or 0x00 (None).  If you 
        /// try setting something else, it will throw an exception.  
        /// </para>
        /// <para>
        /// You may wish to set CompressionMethod to 0 (None) when zipping previously compressed
        /// data like jpg, png, or mp3 files.  This can save time and cpu cycles.
        /// </para>
        /// </remarks>
        /// 
        /// <example>
        /// In this example, the first entry added to the zip archive uses 
        /// the default behavior - compression is used where it makes sense.  
        /// The second entry, the MP3 file, is added to the archive without being compressed.
        /// <code>
        /// using (ZipFile zip = new ZipFile(ZipFileToCreate))
        /// {
        ///   ZipEntry e1= zip.AddFile(@"c:\temp\Readme.txt");
        ///   ZipEntry e2= zip.AddFile(@"c:\temp\StopThisTrain.mp3");
        ///   e2.CompressionMethod = 0;
        ///   zip.Save();
        /// }
        /// </code>
        /// 
        /// <code lang="VB">
        /// Using zip as new ZipFile(ZipFileToCreate)
        ///   zip.AddFile("c:\temp\Readme.txt")
        ///   Dim e2 as ZipEntry = zip.AddFile("c:\temp\StopThisTrain.mp3")
        ///   e2.CompressionMethod = 0
        ///   zip.Save
        /// End Using
        /// </code>
        /// </example>
        public Int16 CompressionMethod
        {
            get { return _CompressionMethod; }
            set
            {
                if (value == 0x00 || value == 0x08)
                    _CompressionMethod = value;
                else throw new InvalidOperationException("Unsupported compression method. Specify 8 or 0.");

                _ForceNoCompression = (_CompressionMethod == 0x0);
            }
        }


        /// <summary>
        /// The compressed size of the file, in bytes, within the zip archive. 
        /// </summary>
        /// <remarks>
        /// The compressed size is computed during compression. This means that it is only
        /// valid to read this AFTER reading in an existing zip file, or AFTER saving a
        /// zipfile you are creating.
        /// </remarks>
        public Int32 CompressedSize
        {
            get { return _CompressedSize; }
        }

        /// <summary>
        /// The size of the file, in bytes, before compression, or after extraction. 
        /// </summary>
        public Int32 UncompressedSize
        {
            get { return _UncompressedSize; }
        }

        /// <summary>
        /// The ratio of compressed size to uncompressed size. This is a double in the
        /// range of 0 to 100.  You could print it with a format string of "{3,5:F0}%"
        /// to see it as a percentage. If the size of the original uncompressed file is 0, 
        /// the return value will be zero. 
        /// </summary>
        public Double CompressionRatio
        {
            get
            {
                if (UncompressedSize == 0) return 0;
                return 100 * (1.0 - (1.0 * CompressedSize) / (1.0 * UncompressedSize));
            }
        }

        /// <summary>
        /// The CRC (Cyclic Redundancy Check) on the contents of the ZipEntry.
        /// </summary>
        public Int32 Crc32
        {
            get { return _Crc32; }
        }

        /// <summary>
        /// True if the entry is a directory (not a file). 
        /// This is a readonly property on the entry.
        /// </summary>
        public bool IsDirectory
        {
            get { return _IsDirectory; }
        }

        /// <summary>
        /// A derived property that is True if the entry uses encryption.  
        /// This is a readonly property on the entry.
        /// Upon reading an entry, this bool is determined by
        /// the data read.  When writing an entry, this bool is
        /// determined by whether the Encryption property is set to something other than
        /// EncryptionAlgorithm.None. 
        /// </summary>
        public bool UsesEncryption
        {
            get { return (Encryption != EncryptionAlgorithm.None); }
        }

        /// <summary>
        /// Set this to specify which encryption algorithm to use for the entry.
        /// In order for this to succeed, you must also set a Password on the entry.
        /// The set of algoritms is determined by the PKZIP specification from PKWare.
        /// The "traditional" encryption used by PKZIP is considered weak.  PKZip also
        /// supports strong encryption mechanisms including AES of various keysizes and
        /// Blowfish, among others.  This library does not implement the full PKZip
        /// spec. 
        /// </summary>
        public EncryptionAlgorithm Encryption
        {
            get { return _Encryption; }
            set { _Encryption = value; }
        }

        /// <summary>
        /// Set this to request that the entry be encrypted when writing the zip
        /// archive.  This is a write-only property on the entry. The password 
        /// is used to encrypt the entry during the Save() operation, or decrypt during
        /// the Extract() or OpenReader() operation. 
        /// </summary>
        public string Password
        {
            set
            {
                _Password = value;
                Encryption = (_Password == null)
                    ? EncryptionAlgorithm.None
                    : EncryptionAlgorithm.PkzipWeak;
            }
        }

        /// <summary>
        /// Specifies that the extraction should overwrite any existing files.
        /// This applies only when calling an Extract method. By default this 
        /// property is false. 
        /// </summary>
        public bool OverwriteOnExtract
        {
            get { return _OverwriteOnExtract; }
            set { _OverwriteOnExtract = value; }
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
        /// the application to for approval for re-reading the stream.  As with other
        /// properties (like Password and ForceNoCompression), setting the corresponding
        /// delegate on the ZipFile class itself will set it on all ZipEntry items that are
        /// subsequently added to the ZipFile instance.
        /// </para>
        ///
        /// </remarks>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.WillReadTwiceOnInflation"/>
        /// <seealso cref="Ionic.Utils.Zip.ReReadApprovalCallback"/>
        public ReReadApprovalCallback WillReadTwiceOnInflation
        {
            get;
            set;
        }


        /// <summary>
        /// A callback that allows the application to specify whether compression should
        /// be used for a given entry that is about to be added to the zip archive.
        /// </summary>
        ///
        /// <remarks>
        /// See <see cref="ZipFile.WantCompression" />
        /// </remarks>
        public WantCompressionCallback WantCompression
        {
            get;
            set;
        }



        /// <summary>
        /// Set to indicate whether to use UTF-8 encoding on filenames and 
        /// comments, according to the PKWare specification.  
        /// </summary>
        /// <remarks>
        /// If this flag is set, the entry will be marked as encoded with UTF-8, 
        /// according to the PWare spec, if necessary.  Necessary means, if the filename or 
        /// entry comment (if any) cannot be reflexively encoded with the default (IBM437) code page. 
        /// </remarks>
        /// <remarks>
        /// Setting this flag to true is equivalent to setting <c>ProvisionalAlternateEncoding</c> to <c>System.Text.Encoding.UTF8</c>
        /// </remarks>
        public bool UseUnicodeAsNecessary
        {
            get
            {
                return _provisionalAlternateEncoding == System.Text.Encoding.GetEncoding("UTF-8");
            }
            set
            {
                _provisionalAlternateEncoding = (value) ? System.Text.Encoding.GetEncoding("UTF-8") : Ionic.Utils.Zip.ZipFile.DefaultEncoding;
            }
        }

        /// <summary>
        /// The text encoding to use for this ZipEntry, when the default
        /// encoding is insufficient.
        /// </summary>
        /// <remarks>
        /// <para>
        /// According to the zip specification from PKWare, filenames and comments for a
        /// ZipEntry are encoded either with IBM437 or with UTF8.  But, some archivers do not
        /// follow the specification, and instead encode characters using the system default
        /// code page, or an arbitrary code page.  For example, WinRAR when run on a machine in
        /// Shanghai may encode filenames with the Chinese (Big-5) code page.  This behavior is
        /// contrary to the Zip specification, but it occurs anyway.  This property exists to
        /// support that non-compliant behavior when reading or writing zip files.
        /// </para>
        /// <para>
        /// When writing zip archives that will be read by one of these other archivers, use this property to 
        /// specify the code page to use when encoding filenames and comments into the zip
        /// file, when the IBM437 code page will not suffice.
        /// </para>
        /// <para>
        /// Be aware that a zip file created after you've explicitly specified the code page will not 
        /// be compliant to the PKWare specification, and may not be readable by compliant archivers. 
        /// On the other hand, many archivers are non-compliant and can read zip files created in 
        /// arbitrary code pages. 
        /// </para>
        /// <para>
        /// When using an arbitrary, non-UTF8 code page for encoding, there is no standard way for the 
        /// creator (DotNetZip) to specify in the zip file which code page has been used. DotNetZip is not
        /// able to inspect the zip file and determine the codepage used for the entries within it. Therefore, 
        /// you, the application author, must determine that.  If you use a codepage which results in filenames
        /// that are not legal in Windows, you will get exceptions upon extract. Caveat Emptor.
        /// </para>
        /// </remarks>
        public System.Text.Encoding ProvisionalAlternateEncoding
        {
            get
            {
                return _provisionalAlternateEncoding;
            }
            set
            {
                _provisionalAlternateEncoding = value;
            }
        }

        /// <summary>
        /// The text encoding actually used for this ZipEntry.
        /// </summary>
        public System.Text.Encoding ActualEncoding
        {
            get
            {
                return _actualEncoding;
            }
        }

#if NOTUSED

        private System.IO.Compression.DeflateStream CompressedStream
        {
            get
            {
                if (_CompressedStream == null)
                {
                    // we read from the underlying memory stream after data is written to the compressed stream
                    _UnderlyingMemoryStream = new System.IO.MemoryStream();
                    bool LeaveUnderlyingStreamOpen = true;

                    // we write to the compressed stream, and compression happens as we write.
                    _CompressedStream = new System.IO.Compression.DeflateStream(_UnderlyingMemoryStream,
										System.IO.Compression.CompressionMode.Compress,
										LeaveUnderlyingStreamOpen);
                }
                return _CompressedStream;
            }
        }
#endif




        private static bool ReadHeader(ZipEntry ze, System.Text.Encoding defaultEncoding)
        {
            int bytesRead = 0;

            ze._RelativeOffsetOfHeader = (int)ze.ArchiveStream.Position;

            int signature = Ionic.Utils.Zip.SharedUtilities.ReadSignature(ze.ArchiveStream);
            bytesRead += 4;

            // Return false if this is not a local file header signature.
            if (ZipEntry.IsNotValidSig(signature))
            {
                // Getting "not a ZipEntry signature" is not always wrong or an error. 
                // This will happen after the last entry in a zipfile.  In that case, we 
                // expect to read : 
                //    a ZipDirEntry signature (if a non-empty zip file) or 
                //    a ZipConstants.EndOfCentralDirectorySignature.  
                //
                // Anything else is a surprise.

                ze.ArchiveStream.Seek(-4, System.IO.SeekOrigin.Current); // unread the signature
                if (ZipDirEntry.IsNotValidSig(signature) && (signature != ZipConstants.EndOfCentralDirectorySignature))
                {
                    throw new BadReadException(String.Format("  ZipEntry::Read(): Bad signature (0x{0:X8}) at position  0x{1:X8}", signature, ze.ArchiveStream.Position));
                }
                return false;
            }

            byte[] block = new byte[26];
            int n = ze.ArchiveStream.Read(block, 0, block.Length);
            if (n != block.Length) return false;
            bytesRead += n;

            int i = 0;
            ze._VersionNeeded = (short)(block[i++] + block[i++] * 256);
            ze._BitField = (short)(block[i++] + block[i++] * 256);
            ze._CompressionMethod = (short)(block[i++] + block[i++] * 256);
            ze._TimeBlob = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
            // transform the time data into something usable (a DateTime)
            ze._LastModified = Ionic.Utils.Zip.SharedUtilities.PackedToDateTime(ze._TimeBlob);


            // The PKZIP spec says that if bit 3 is set (0x0008) in the General Purpose BitField, then the CRC,
            // Compressed size, and uncompressed size come directly after the file data.  The only way to find
            // it is to scan the zip archive for the signature of the Data Descriptor, and presume that that
            // signature does not appear in the (compressed) data of the compressed file.

            if ((ze._BitField & 0x0008) != 0x0008)
            {
                ze._Crc32 = (Int32)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
                ze._CompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
                ze._UncompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
            }
            else
            {
                // The CRC, compressed size, and uncompressed size are stored later in the stream.
                // Here, we advance the pointer.
                i += 12;
            }

            Int16 filenameLength = (short)(block[i++] + block[i++] * 256);
            Int16 extraFieldLength = (short)(block[i++] + block[i++] * 256);

            block = new byte[filenameLength];
            n = ze.ArchiveStream.Read(block, 0, block.Length);
            bytesRead += n;

            // if the UTF8 bit is set for this entry, we override the encoding the application requested.
            ze._actualEncoding = ((ze._BitField & 0x0800) == 0x0800)
        ? System.Text.Encoding.UTF8
        : defaultEncoding;

            ze._FileNameInArchive = ze._actualEncoding.GetString(block);

            // when creating an entry by reading, the LocalFileName is the same as the FileNameInArchivre
            ze._LocalFileName = ze._FileNameInArchive;

            if (extraFieldLength > 0)
            {
                ze._Extra = new byte[extraFieldLength];
                n = ze.ArchiveStream.Read(ze._Extra, 0, ze._Extra.Length);
                bytesRead += n;
            }

            // actually get the compressed size and CRC if necessary
            if ((ze._BitField & 0x0008) == 0x0008)
            {
                // This descriptor exists only if bit 3 of the general
                // purpose bit flag is set (see below).  It is byte aligned
                // and immediately follows the last byte of compressed data.
                // This descriptor is used only when it was not possible to
                // seek in the output .ZIP file, e.g., when the output .ZIP file
                // was standard output or a non-seekable device.  For ZIP64(tm) format
                // archives, the compressed and uncompressed sizes are 8 bytes each.

                long posn = ze.ArchiveStream.Position;

                // Here, we're going to loop until we find a ZipEntryDataDescriptorSignature and 
                // a consistent data record after that.   To be consistent, the data record must 
                // indicate the length of the entry data. 
                bool wantMore = true;
                long SizeOfDataRead = 0;
                int tries = 0;
                while (wantMore)
                {
                    tries++;
                    // We call the FindSignature shared routine to find the specified signature in the already-opened zip archive, 
                    // starting from the current cursor position in that filestream.  There are two possibilities:  either we find the 
                    // signature or we don't.  If we cannot find it, then the routine returns -1, and the ReadHeader() method returns false, 
                    // indicating we cannot read a legal entry header.  If we have found it, then the FindSignature() method returns 
                    // the number of bytes in the stream we had to seek forward, to find the sig.  We need this to determine if
                    // the zip entry is valid, later. 

                    long d = Ionic.Utils.Zip.SharedUtilities.FindSignature(ze.ArchiveStream, ZipConstants.ZipEntryDataDescriptorSignature);
                    if (d == -1) return false;

                    // total size of data read (through all loops of this). 
                    SizeOfDataRead += d;

                    // read 3x 4-byte fields (CRC, Compressed Size, Uncompressed Size)
                    block = new byte[12];
                    n = ze.ArchiveStream.Read(block, 0, block.Length);
                    if (n != 12) return false;
                    bytesRead += n;
                    i = 0;
                    ze._Crc32 = (Int32)(block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256);
                    ze._CompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
                    ze._UncompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;

                    wantMore = (SizeOfDataRead != ze._CompressedSize);
                    if (wantMore)
                    {
                        // Seek back to un-read the last 12 bytes  - maybe THEY contain 
                        // the ZipEntryDataDescriptorSignature.
                        // (12 bytes for the CRC, Comp and Uncomp size.)
                        ze.ArchiveStream.Seek(-12, System.IO.SeekOrigin.Current);

                        // Adjust the size to account for the false signature read in 
                        // FindSignature().
                        SizeOfDataRead += 4;
                    }
                }

                //if (SizeOfDataRead != ze._CompressedSize)
                //    throw new BadReadException("Data format error (bit 3 is set)");

                // seek back to previous position, to prepare to read file data
                ze.ArchiveStream.Seek(posn, System.IO.SeekOrigin.Begin);
            }

            ze._CompressedFileDataSize = ze._CompressedSize;

            if ((ze._BitField & 0x01) == 0x01)
            {
                // PKZIP encrypts the compressed data stream.  Encrypted files must
                // be decrypted before they can be extracted.

                // Each encrypted file has an extra 12 bytes stored at the start of
                // the data area defining the encryption header for that file.  The
                // encryption header is originally set to random values, and then
                // itself encrypted, using three, 32-bit keys.  The key values are
                // initialized using the supplied encryption password.  After each byte
                // is encrypted, the keys are then updated using pseudo-random number
                // generation techniques in combination with the same CRC-32 algorithm
                // used in PKZIP and described elsewhere in this document.

                ze._Encryption = EncryptionAlgorithm.PkzipWeak;

                // read the 12-byte encryption header
                ze._WeakEncryptionHeader = new byte[12];
                n = ze.ArchiveStream.Read(ze._WeakEncryptionHeader, 0, 12);
                if (n != 12) return false;

                bytesRead += n;

                // decrease the filedata size by 12 bytes
                ze._CompressedFileDataSize -= 12;
            }

            // remember the size of the blob for this entry. 
            // we also have the starting position in the stream for this entry. 
            ze._TotalEntrySize = bytesRead + ze._CompressedFileDataSize;
            ze._LengthOfHeader = bytesRead;

            // The pointer in the file is now at the start of the filedata, 
            // which is potentially compressed and encrypted.

            return true;
        }



        private static bool IsNotValidSig(int signature)
        {
            return (signature != ZipConstants.ZipEntrySignature);
        }


        /// <summary>
        /// Reads one ZipEntry from the given stream.  If the entry is encrypted, we don't
        /// actuall decrypt at this point. 
        /// </summary>
        /// <param name="zf">the zipfile this entry belongs to.</param>
        /// <returns>the ZipEntry read from the stream.</returns>
        internal static ZipEntry Read(ZipFile zf)
        {
            System.IO.Stream s = zf.ReadStream;
            System.Text.Encoding defaultEncoding = zf.ProvisionalAlternateEncoding;
            ZipEntry entry = new ZipEntry();
            entry._Source = EntrySource.Zipfile;
            entry._zipfile = zf;
            entry._archiveStream = s;
            if (!ReadHeader(entry, defaultEncoding)) return null;

            // store the position in the stream for this entry
            entry.__FileDataPosition = entry.ArchiveStream.Position;

            // seek past the data without reading it. We will read on Extract()
            s.Seek(entry._CompressedFileDataSize, System.IO.SeekOrigin.Current);

            // finally, seek past the (already read) Data descriptor if necessary
            if ((entry._BitField & 0x0008) == 0x0008)
            {
                s.Seek(16, System.IO.SeekOrigin.Current);
            }

            // workitem 5306
            // http://www.codeplex.com/DotNetZip/WorkItem/View.aspx?WorkItemId=5306
            HandleUnexpectedDataDescriptor(entry);

            return entry;
        }


        private static void HandleUnexpectedDataDescriptor(ZipEntry entry)
        {
            System.IO.Stream s = entry.ArchiveStream;
            // In some cases, the "data descriptor" is present, without a signature, even when bit 3 of the BitField is NOT SET.  
            // This is the CRC, followed
            //    by the compressed length and the uncompressed length (4 bytes for each 
            //    of those three elements).  Need to check that here.             
            //
            uint datum = (uint)Ionic.Utils.Zip.SharedUtilities.ReadInt(s);
            if (datum == entry._Crc32)
            {
                int sz = Ionic.Utils.Zip.SharedUtilities.ReadInt(s);
                if (sz == entry._CompressedSize)
                {
                    sz = Ionic.Utils.Zip.SharedUtilities.ReadInt(s);
                    if (sz == entry._UncompressedSize)
                    {
                        // ignore everything and discard it.
                    }
                    else
                        s.Seek(-12, System.IO.SeekOrigin.Current); // unread the three blocks
                }
                else
                    s.Seek(-8, System.IO.SeekOrigin.Current); // unread the two blocks
            }
            else
                s.Seek(-4, System.IO.SeekOrigin.Current); // unread the block

        }




        internal static string NameInArchive(String filename, string directoryPathInArchive)
        {
            string result = null;
            if (directoryPathInArchive == null)
                result = filename;

            else
            {
                if (String.IsNullOrEmpty(directoryPathInArchive))
                {
                    //if (filename.EndsWith("\\"))
                    //{
                    //    result = System.IO.Path.GetFileName(filename.Substring(0, filename.Length - 1));
                    //}
                    //else
                    result = System.IO.Path.GetFileName(filename);
                }
                else
                {
                    // explicitly specify a pathname for this file  
                    result = System.IO.Path.Combine(directoryPathInArchive, System.IO.Path.GetFileName(filename));
                }

            }
            return SharedUtilities.TrimVolumeAndSwapSlashes(result);
        }



        internal static ZipEntry Create(String filename, string nameInArchive)
        {
            return Create(filename, nameInArchive, null);
        }


        internal static ZipEntry Create(String filename, string nameInArchive, System.IO.Stream stream)
        {
            if (String.IsNullOrEmpty(filename))
                throw new Ionic.Utils.Zip.ZipException("The entry name must be non-null and non-empty.");

            ZipEntry entry = new ZipEntry();
            if (stream != null)
            {
                entry._sourceStream = stream;
                entry._LastModified = DateTime.Now;
            }
            else
            {
                entry._LastModified = (System.IO.File.Exists(filename) || System.IO.Directory.Exists(filename))
                    ? SharedUtilities.RoundToEvenSecond(System.IO.File.GetLastWriteTime(filename))
                    : DateTime.Now;

                if (!entry._LastModified.IsDaylightSavingTime() &&
                    DateTime.Now.IsDaylightSavingTime())
                {
                    entry._LastModified = entry._LastModified + new System.TimeSpan(1, 0, 0);
                }
            }

            entry._LocalFileName = filename; // may include a path
            entry._FileNameInArchive = nameInArchive.Replace('\\', '/');

            // we don't actually slurp in the file until the caller invokes Write on this entry.

            return entry;
        }




        #region Extract methods
        /// <summary>
        /// Extract the entry to the filesystem, starting at the current working directory. 
        /// </summary>
        /// 
        /// <overloads>
        /// This method has a bunch of overloads! One of them is sure to be
        /// the right one for you... If you don't like these, check out the 
        /// <c>ExtractWithPassword()</c> methods.
        /// </overloads>
        ///         
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.OverwriteOnExtract"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.Extract(bool)"/>
        ///
        /// <remarks>
        /// <para>
        /// Existing entries in the filesystem will not be overwritten. If you would like to 
        /// force the overwrite of existing files, see the <c>OverwriteOnExtract</c> property, 
        /// or try one of the overloads of the Extract method that accept a boolean flag
        /// to indicate explicitly whether you want overwrite.
        /// </para>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        public void Extract()
        {
            InternalExtract(".", null, null);
        }

        /// <summary>
        /// Extract the entry to a file in the filesystem, potentially overwriting
        /// any existing file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        /// <param name="overwrite">true if the caller wants to overwrite an existing file by the same name in the filesystem.</param>
        public void Extract(bool overwrite)
        {
            OverwriteOnExtract = overwrite;
            InternalExtract(".", null, null);
        }

        /// <summary>
        /// Extracts the entry to the specified stream. 
        /// For example, the caller could specify Console.Out, or a MemoryStream.
        /// </summary>
        /// 
        /// <param name="stream">the stream to which the entry should be extracted.  </param>
        /// 
        /// <remarks>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        public void Extract(System.IO.Stream stream)
        {
            InternalExtract(null, stream, null);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory. 
        /// </summary>
        /// 
        /// <param name="baseDirectory">the pathname of the base directory</param>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.OverwriteOnExtract"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.Extract(string, bool)"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipFile.Extract(string)"/>
        /// 
        /// <example>
        /// This example extracts only the entries in a zip file that are .txt files, into a directory called "textfiles".
        /// <code lang="C#">
        /// using (ZipFile zip = ZipFile.Read("PackedDocuments.zip"))
        /// {
        ///   foreach (string s1 in zip.EntryFilenames)
        ///   {
        ///     if (s1.EndsWith(".txt")) 
        ///     {
        ///       ZipEntry entry= zip[s1];
        ///       entry.Extract("textfiles");
        ///     }
        ///   }
        /// }
        /// </code>
        /// <code lang="VB">
        ///   Using zip As ZipFile = ZipFile.Read("PackedDocuments.zip")
        ///       Dim s1 As String
        ///       For Each s1 In zip.EntryFilenames
        ///           If s1.EndsWith(".txt") Then
        ///               Dim entry as ZipEntry
        ///               entry = zip(s1)
        ///               entry.Extract("textfiles")
        ///           End If
        ///       Next
        ///   End Using
        /// </code>
        /// </example>
        /// 
        /// <remarks>
        /// <para>
        /// Existing entries in the filesystem will not be overwritten. If you would like to 
        /// force the overwrite of existing files, see the <c>OverwriteOnExtract</c> property, 
        /// or try one of the overloads of the Extract method that accept a boolean flag
        /// to indicate explicitly whether you want overwrite.
        /// </para>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        public void Extract(string baseDirectory)
        {
            InternalExtract(baseDirectory, null, null);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory, 
        /// and potentially overwriting existing files in the filesystem. 
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="baseDirectory">the pathname of the base directory</param>
        /// <param name="overwrite">If true, overwrite any existing files if necessary upon extraction.</param>
        public void Extract(string baseDirectory, bool overwrite)
        {
            OverwriteOnExtract = overwrite;
            InternalExtract(baseDirectory, null, null);
        }

        /// <summary>
        /// Extract the entry to the filesystem, using the current working directory,
        /// and using the specified password. 
        /// </summary>
        ///
        /// <overloads>
        /// This method has a bunch of overloads! One of them is sure to be
        /// the right one for you...
        /// </overloads>
        ///         
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.OverwriteOnExtract"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.ExtractWithPassword(bool, string)"/>
        ///
        /// <remarks>
        /// <para>
        /// Existing entries in the filesystem will not be overwritten. If you would like to 
        /// force the overwrite of existing files, see the <c>OverwriteOnExtract</c> property, 
        /// or try one of the overloads of the ExtractWithPassword method that accept a boolean flag
        /// to indicate explicitly whether you want overwrite.
        /// </para>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        ///
        /// <param name="password">The Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(string password)
        {
            InternalExtract(".", null, password);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory,
        /// and using the specified password. 
        /// </summary>
        /// 
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.OverwriteOnExtract"/>
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.ExtractWithPassword(string, bool, string)"/>
        ///
        /// <remarks>
        /// <para>
        /// Existing entries in the filesystem will not be overwritten. If you would like to 
        /// force the overwrite of existing files, see the <c>OverwriteOnExtract</c> property, 
        /// or try one of the overloads of the ExtractWithPassword method that accept a boolean flag
        /// to indicate explicitly whether you want overwrite.
        /// </para>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="baseDirectory">The pathname of the base directory.</param>
        /// <param name="password">The Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(string baseDirectory, string password)
        {
            InternalExtract(baseDirectory, null, password);
        }

        /// <summary>
        /// Extract the entry to a file in the filesystem, potentially overwriting
        /// any existing file.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="overwrite">true if the caller wants to overwrite an existing 
        /// file by the same name in the filesystem.</param>
        /// <param name="password">The Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(bool overwrite, string password)
        {
            OverwriteOnExtract = overwrite;
            InternalExtract(".", null, password);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory, 
        /// and potentially overwriting existing files in the filesystem. 
        /// </summary>
        /// 
        /// <remarks>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </remarks>
        ///
        /// <param name="baseDirectory">the pathname of the base directory</param>
        /// <param name="overwrite">If true, overwrite any existing files if necessary upon extraction.</param>
        /// <param name="password">The Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(string baseDirectory, bool overwrite, string password)
        {
            OverwriteOnExtract = overwrite;
            InternalExtract(baseDirectory, null, password);
        }

        /// <summary>
        /// Extracts the entry to the specified stream, using the specified Password.
        /// For example, the caller could extract to Console.Out, or to a MemoryStream.
        /// </summary>
        /// 
        /// <remarks>
        /// See the remarks on the LastModified property, for some details 
        /// about how the last modified time of the created file is set.
        /// </remarks>
        /// 
        /// <param name="stream">the stream to which the entry should be extracted.  </param>
        /// <param name="password">The password to use for decrypting the entry.</param>
        public void ExtractWithPassword(System.IO.Stream stream, string password)
        {
            InternalExtract(null, stream, password);
        }


        /// <summary>
        /// Opens the backing stream for the zip entry in the archive, for reading. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// The ZipEntry has methods that extract the entry to an already-opened stream.
        /// This is an alternative method for those applications that wish to manipulate the stream directly.
        /// </para>
        /// <para>
        /// The CrcCalculatorStream that is returned is just a regular read-only stream - you can use it as you would
        /// any stream.  The one additional feature it adds is that it calculates a CRC32 on the bytes of the stream 
        /// as it is read.  This CRC should be used by the application to validate the content of the ZipEntry, when 
        /// the read is complete.  Check the example for how to do this. 
        /// </para>
        /// <para>
        /// If the entry is protected with a password, then you need to set the password on the entry prior to calling OpenReader().
        /// </para>
        /// </remarks>
        /// <example>
        /// In this example, we open a zipfile, then read in a named entry via a stream, scanning
        /// the bytes in the entry as we go.  Finally, the CRC and the size of the entry are verified.
        /// <code>
        /// using (ZipFile zip = new ZipFile(ZipFileToRead))
        /// {
        ///   ZipEntry e1= zip["Download.mp3"];
        ///   using (CrcCalculatorStream s = e1.OpenReader())
        ///   {
        ///     byte[] buffer = new byte[4096];
        ///     int n, totalBytesRead= 0;
        ///     do {
        ///       n = s.Read(buffer,0, buffer.Length);
        ///       totalBytesRead+=n; 
        ///     } while (n&gt;0);
        ///      if (s.Crc32 != e1.Crc32)
        ///       throw new Exception(string.Format("The Zip Entry failed the CRC Check. (0x{0:X8}!=0x{1:X8})", s.Crc32, e1.Crc32));
        ///      if (totalBytesRead != e1.UncompressedSize)
        ///       throw new Exception(string.Format("We read an unexpected number of bytes. ({0}!={1})", totalBytesRead, e1.UncompressedSize));
        ///   }
        /// }
        /// </code>
        /// <code lang="VB">
        ///   Using zip As New ZipFile(ZipFileToRead)
        ///       Dim e1 As ZipEntry = zip.Item("Download.mp3")
        ///       Using s As CrcCalculatorStream = e1.OpenReader
        ///           Dim n As Integer
        ///           Dim buffer As Byte() = New Byte(4096) {}
        ///           Dim totalBytesRead As Integer = 0
        ///           Do
        ///               n = s.Read(buffer, 0, buffer.Length)
        ///               totalBytesRead = (totalBytesRead + n)
        ///           Loop While (n &gt; 0)
        ///           If (s.Crc32 &lt;&gt; e1.Crc32) Then
        ///               Throw New Exception(String.Format("The Zip Entry failed the CRC Check. (0x{0:X8}!=0x{1:X8})", s.Crc32, e1.Crc32))
        ///           End If
        ///           If (totalBytesRead &lt;&gt; e1.UncompressedSize) Then
        ///               Throw New Exception(String.Format("We read an unexpected number of bytes. ({0}!={1})", totalBytesRead, e1.UncompressedSize))
        ///           End If
        ///       End Using
        ///   End Using
        /// </code>
        /// </example>
        /// <seealso cref="Ionic.Utils.Zip.ZipEntry.Extract(System.IO.Stream)"/>
        /// <returns>The Stream for reading.</returns>
        public CrcCalculatorStream OpenReader()
        {
            return InternalOpenReader(this._Password);
        }

        /// <summary>
        /// Opens the backing stream for an encrypted zip entry in the archive, for reading. 
        /// </summary>
        /// <remarks>
        /// <para>
        /// See the documentation on the OpenReader() method for full details.  This overload allows the 
        /// application to specify a password for the ZipEntry to be read. 
        /// </para>
        /// </remarks>
        /// <param name="password">The password to use for decrypting the entry.</param>
        /// <returns>The Stream for reading.</returns>
        public CrcCalculatorStream OpenReader(string password)
        {
            return InternalOpenReader(password);
        }


        private System.IO.Stream ArchiveStream
        {
            get
            {
                if (_archiveStream == null)
                {
                    if (_zipfile != null)
                    {
                        _zipfile.Reset();
                        _archiveStream = _zipfile.ReadStream;
                    }
                }
                return _archiveStream;
            }
        }


        private CrcCalculatorStream InternalOpenReader(string password)
        {
            ValidateCompression();
            ValidateEncryption();
            ZipCrypto cipher = SetupCipher(password);

            // seek to the beginning of the file data in the stream
            this.ArchiveStream.Seek(this.__FileDataPosition, System.IO.SeekOrigin.Begin);

            var instream = (Encryption == EncryptionAlgorithm.PkzipWeak)
        ? new ZipCipherStream(this.ArchiveStream, cipher, CryptoMode.Decrypt)
        : this.ArchiveStream;

            return new CrcCalculatorStream((CompressionMethod == 0x08) ?
                       new DeflateStream(instream, CompressionMode.Decompress, true) :
                       instream, _UncompressedSize);

        }
        #endregion



        private void OnExtractProgress(int bytesWritten, int totalBytesToWrite)
        {
            _ioOperationCanceled = _zipfile.OnBlockExtracted(FileName, bytesWritten, totalBytesToWrite);
        }

        private void OnBeforeExtract(string path)
        {
            if (!_zipfile._inExtractAll)
            {
                _ioOperationCanceled =
                    _zipfile.OnSingleEntryExtractProgress(FileName, path, true, OverwriteOnExtract);
            }
        }

        private void OnAfterExtract(string path)
        {
            if (!_zipfile._inExtractAll)
            {
                _zipfile.OnSingleEntryExtractProgress(FileName, path, false, OverwriteOnExtract);
            }
        }

        private void OnWriteBlock(int bytesWritten, int totalBytesToWrite)
        {
            _ioOperationCanceled = _zipfile.OnSaveBlock(FileName, bytesWritten, totalBytesToWrite);
        }


        // Pass in either basedir or s, but not both. 
        // In other words, you can extract to a stream or to a directory (filesystem), but not both!
        // The Password param is required for encrypted entries.
        private void InternalExtract(string baseDir, System.IO.Stream outstream, string password)
        {
            OnBeforeExtract(baseDir);
            _ioOperationCanceled = false;
            string TargetFile = null;
            System.IO.Stream output = null;

            try
            {
                ValidateCompression();
                ValidateEncryption();

                if (ValidateOutput(baseDir, outstream, out TargetFile)) return;

                // if none specified, use the password on the entry itself.
                if (password == null) password = _Password;

                ZipCrypto cipher = SetupCipher(password);

                if (TargetFile != null)
                {
                    // ensure the target path exists
                    if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(TargetFile)))
                        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(TargetFile));

                    // and ensure we can create the file
                    if ((OverwriteOnExtract) && (System.IO.File.Exists(TargetFile)))
                        System.IO.File.Delete(TargetFile);

                    output = new System.IO.FileStream(TargetFile, System.IO.FileMode.CreateNew);
                }
                else
                    output = outstream;

                if (_ioOperationCanceled)
                {
                    try
                    {
                        if (TargetFile != null)
                        {
                            if (output != null) output.Close();
                            // attempt to remove the target file if an exception has occurred:
                            if (System.IO.File.Exists(TargetFile))
                                System.IO.File.Delete(TargetFile);
                        }
                    }
                    finally { }

                }

                Int32 ActualCrc32 = _ExtractOne(output, cipher);

                if (_ioOperationCanceled)
                {
                    try
                    {
                        if (TargetFile != null)
                        {
                            if (output != null) output.Close();
                            // attempt to remove the target file if an exception has occurred:
                            if (System.IO.File.Exists(TargetFile))
                                System.IO.File.Delete(TargetFile);
                        }
                    }
                    finally { }
                }

                // After extracting, Validate the CRC32
                if (ActualCrc32 != _Crc32)
                {
                    throw new BadCrcException("CRC error: the file being extracted appears to be corrupted. " +
                                  String.Format("Expected 0x{0:X8}, actual 0x{1:X8}", _Crc32, ActualCrc32));
                }


                if (TargetFile != null)
                {
                    output.Close();
                    output = null;

                    // workitem 6191
                    DateTime AdjustedLastModified = LastModified;
                    if (DateTime.Now.IsDaylightSavingTime())
                    {
                        if (!LastModified.IsDaylightSavingTime())
                            AdjustedLastModified = LastModified - new System.TimeSpan(1, 0, 0);
                    }
#if NOTUSED
		    else 
		    {
			AdjustedLastModified= (LastModified.IsDaylightSavingTime())
			    ? LastModified  //+ new System.TimeSpan(1, 0, 0)
			    : LastModified ;
		    }
#endif
                    System.IO.File.SetLastWriteTime(TargetFile, AdjustedLastModified);
                }

                OnAfterExtract(baseDir);

            }
            catch
            {
                try
                {
                    if (TargetFile != null)
                    {
                        if (output != null) output.Close();
                        // attempt to remove the target file if an exception has occurred:
                        if (System.IO.File.Exists(TargetFile))
                            System.IO.File.Delete(TargetFile);
                    }
                }
                finally { }

                // re-raise the original exception
                throw;
            }

        }


        private ZipCrypto SetupCipher(string password)
        {
            ZipCrypto cipher = null;
            // decrypt the file header data here if necessary. 
            if (Encryption == EncryptionAlgorithm.PkzipWeak)
            {
                if (password == null)
                    throw new BadPasswordException("This entry requires a password.");

                cipher = new ZipCrypto();
                cipher.InitCipher(password);

                // Decrypt the header.  This has a side effect of "further initializing the
                // encryption keys" in the traditional zip encryption. 
                byte[] DecryptedHeader = cipher.DecryptMessage(_WeakEncryptionHeader, _WeakEncryptionHeader.Length);

                // CRC check
                // According to the pkzip spec, the final byte in the decrypted header 
                // is the highest-order byte in the CRC. We check it here. 
                if (DecryptedHeader[11] != (byte)((_Crc32 >> 24) & 0xff))
                {
                    // In the case that bit 3 of the general purpose bit flag is set to indicate
                    // the presence of an 'Extended File Header', the last byte of the decrypted
                    // header is sometimes compared with the high-order byte of the lastmodified 
                    // time, and not the CRC, to verify the password. 
                    //
                    // This is not documented in the PKWare Appnote.txt.  
                    // This was discovered this by analysis of the Crypt.c source file in the InfoZip library
                    // http://www.info-zip.org/pub/infozip/

                    if ((_BitField & 0x0008) != 0x0008)
                    {
                        throw new BadPasswordException("The password did not match.");
                    }
                    else if (DecryptedHeader[11] != (byte)((_TimeBlob >> 8) & 0xff))
                    {
                        throw new BadPasswordException("The password did not match.");
                    }
                }

                // We have a good password. 
            }

            return cipher;
        }


        private void ValidateEncryption()
        {
            if ((Encryption != EncryptionAlgorithm.PkzipWeak) &&
                (Encryption != EncryptionAlgorithm.None))
                throw new ArgumentException(String.Format("Unsupported Encryption algorithm ({0:X2})",
                              Encryption));
        }

        private void ValidateCompression()
        {
            if ((CompressionMethod != 0) && (CompressionMethod != 0x08))  // deflate
                throw new ArgumentException(String.Format("Unsupported Compression method ({0:X2})",
                              CompressionMethod));
        }


        private bool ValidateOutput(string basedir, Stream outstream, out string OutputFile)
        {
            if (basedir != null)
            {
                // Sometimes the name on the entry starts with a slash.
                // Rather than unpack to the root of the volume, we're going to 
                // drop the slash and unpack to the specified base directory. 
                OutputFile = (this.FileName.StartsWith("/"))
                    ? System.IO.Path.Combine(basedir, this.FileName.Substring(1))
                    : System.IO.Path.Combine(basedir, this.FileName);

                // check if a directory
                if ((IsDirectory) || (FileName.EndsWith("/")))
                {
                    if (!System.IO.Directory.Exists(OutputFile))
                        System.IO.Directory.CreateDirectory(OutputFile);
                    return true;  // true == all done, caller will return 
                }
                return false;  // false == work to do by caller.
            }

            if (outstream != null)
            {
                OutputFile = null;
                if ((IsDirectory) || (FileName.EndsWith("/")))
                {
                    // extract a directory to streamwriter?  nothing to do!
                    return true;  // true == all done!  caller can return
                }
                return false;
            }

            throw new ZipException("Cannot extract.", new ArgumentException("Invalid input.", "outstream | basedir"));
        }



        private void _CheckRead(int nbytes)
        {
            if (nbytes == 0)
                throw new BadReadException(String.Format("bad read of entry {0} from compressed archive.",
                             this.FileName));

        }


        private Int32 _ExtractOne(System.IO.Stream output, ZipCrypto cipher)
        {
            System.IO.Stream input = this.ArchiveStream;

            // seek to the beginning of the file data in the stream
            input.Seek(this.__FileDataPosition, System.IO.SeekOrigin.Begin);

            // to validate the CRC. 
            Int32 CrcResult = 0;

            byte[] bytes = new byte[READBLOCK_SIZE];

            // The extraction process varies depending on how the entry was stored.
            // It could have been encrypted, and it coould have been compressed, or both, or
            // neither. So we need to check both the encryption flag and the compression flag,
            // and take the proper action in all cases.  

            int LeftToRead = (CompressionMethod == 0x08) ? this.UncompressedSize : this._CompressedFileDataSize;

            // get a stream that either decrypts or not.
            Stream input2 = (Encryption == EncryptionAlgorithm.PkzipWeak)
                ? new ZipCipherStream(input, cipher, CryptoMode.Decrypt)
                : input;

            // using the above, now we get a stream that either decompresses or not.
            Stream input3 = (CompressionMethod == 0x08)
                ? new DeflateStream(input2, CompressionMode.Decompress, true)
                : input2;

            //var out2 = new CrcCalculatorStream(output, LeftToRead);
            int bytesWritten = 0;
            // as we read, we maybe decrypt, and then we maybe decompress. Then we write.
            using (var s1 = new CrcCalculatorStream(input3))
            {
                while (LeftToRead > 0)
                {
                    int len = (LeftToRead > bytes.Length) ? bytes.Length : LeftToRead;
                    int n = s1.Read(bytes, 0, len);
                    _CheckRead(n);
                    output.Write(bytes, 0, n);
                    LeftToRead -= n;
                    bytesWritten += n;

                    // fire the progress event, check for cancels
                    OnExtractProgress(bytesWritten, UncompressedSize);
                    if (_ioOperationCanceled)
                    {
                        break;
                    }
                }

                CrcResult = s1.Crc32;
            }

            return CrcResult;
        }




        internal void MarkAsDirectory()
        {
            _IsDirectory = true;
            // workitem 6279
            if (!_FileNameInArchive.EndsWith("/"))
                _FileNameInArchive += "/";
        }



        internal void WriteCentralDirectoryEntry(System.IO.Stream s)
        {
            byte[] bytes = new byte[4096];
            int i = 0;
            // signature 
            bytes[i++] = (byte)(ZipConstants.ZipDirEntrySignature & 0x000000FF);
            bytes[i++] = (byte)((ZipConstants.ZipDirEntrySignature & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((ZipConstants.ZipDirEntrySignature & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((ZipConstants.ZipDirEntrySignature & 0xFF000000) >> 24);

            // Version Made By
            bytes[i++] = _EntryHeader[4];
            bytes[i++] = _EntryHeader[5];

            // workitem 6182 - zero out extra field length before writing
            Int16 extraFieldLengthSave = (short)(_EntryHeader[28] + _EntryHeader[29] * 256);
            _EntryHeader[28] = 0;
            _EntryHeader[29] = 0;

            // Version Needed, Bitfield, compression method, lastmod,
            // crc, compressed and uncompressed sizes, filename length and extra field length -
            // are all the same as the local file header. So just copy them.
            int j = 0;
            for (j = 0; j < 26; j++)
                bytes[i + j] = _EntryHeader[4 + j];

            // workitem 6182 - restore extra field length after writing
            _EntryHeader[28] = (byte)(extraFieldLengthSave & 0x00FF);
            _EntryHeader[29] = (byte)((extraFieldLengthSave & 0xFF00) >> 8);

            i += j;  // positioned at next available byte

            // File (entry) Comment Length
            // the _CommentBytes private field was set during WriteHeader()
            int commentLength = (_CommentBytes == null) ? 0 : _CommentBytes.Length;

            // the size of our buffer defines the max length of the comment we can write
            if (commentLength + i > bytes.Length) commentLength = bytes.Length - i;
            bytes[i++] = (byte)(commentLength & 0x00FF);
            bytes[i++] = (byte)((commentLength & 0xFF00) >> 8);

            // Disk number start
            bytes[i++] = 0;
            bytes[i++] = 0;

            // internal file attrs
            bytes[i++] = (byte)((IsDirectory) ? 0 : 1);
            bytes[i++] = 0;

            // external file attrs
            bytes[i++] = (byte)((IsDirectory) ? 0x10 : 0x20);
            bytes[i++] = 0;
            bytes[i++] = 0xb6; // ?? not sure, this might also be zero
            bytes[i++] = 0x81; // ?? ditto

            // relative offset of local header (I think this can be zero)
            bytes[i++] = (byte)(_RelativeOffsetOfHeader & 0x000000FF);
            bytes[i++] = (byte)((_RelativeOffsetOfHeader & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_RelativeOffsetOfHeader & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_RelativeOffsetOfHeader & 0xFF000000) >> 24);

            // actual filename (starts at offset 30 in header) 

            Int16 filenameLength = (short)(_EntryHeader[26] + _EntryHeader[27] * 256);
            for (j = 0; j < filenameLength; j++)
                bytes[i + j] = _EntryHeader[30 + j];
            i += j;

            // "Extra field"
            // In the central directory, this library always writes nothing for 
            // the extra field.  

            // file (entry) comment
            if (commentLength != 0)
            {
                // now actually write the comment itself into the byte buffer
                for (j = 0; (j < commentLength) && (i + j < bytes.Length); j++)
                {
                    bytes[i + j] = _CommentBytes[j];
                    //System.BitConverter.GetBytes(c[j])[0];
                }
                i += j;
            }

            s.Write(bytes, 0, i);
        }


#if INFOZIP_UTF8
        static private bool FileNameIsUtf8(char[] FileNameChars)
        {
            bool isUTF8 = false;
            bool isUnicode = false;
            for (int j = 0; j < FileNameChars.Length; j++)
            {
                byte[] b = System.BitConverter.GetBytes(FileNameChars[j]);
                isUnicode |= (b.Length != 2);
                isUnicode |= (b[1] != 0);
                isUTF8 |= ((b[0] & 0x80) != 0);
            }

            return isUTF8;
        }
#endif


        private byte[] GetExtraField()
        {
            byte[] block = null;
            //var data = new System.Collections.Generic.List<byte>();
            if ((UsesEncryption) && (IsStrong(Encryption)))
            {
                // byte[] block= GetStrongEncryptionBlock();
                // return block;
            }

#if INFOZIP_UTF8
		if (FileNameIsUtf8(FileNameChars))
		{
		    _FilenameIsUtf8= true;
		    int datasize= 2+2+1+4+FileNameChars.Length; 
		    block= new byte[datasize];
		    int i=0, mark=0;
		    uint d= (uint) (datasize -4); 
		    block[i++]= 0x75;
		    block[i++]= 0x70;
		    block[i++]= (byte)(d & 0x00FF); 
		    block[i++]= (byte)(d & 0xFF00); 

		    // version
		    block[i++]= 1;

		    // skip the CRC on the filenamebytes, for now
		    mark = i;
		    i+= 4;

		    // UTF8 filename
		    for (int j = 0; j < FileNameChars.Length; j++) 
		    {
			byte[] b = System.BitConverter.GetBytes(FileNameChars[j]);
			block[i++]= b[0];
		    }


		    // filename field Crc32 - for the non-UTF8 filename field
		    CRC32 Crc32= new CRC32();
		    Crc32.SlurpBlock(block, mark+4, FileNameChars.Length);
		    i= mark;
		    block[i++] = (byte)(Crc32.Crc32Result & 0x000000FF);
		    block[i++] = (byte)((Crc32.Crc32Result & 0x0000FF00) >> 8);
		    block[i++] = (byte)((Crc32.Crc32Result & 0x00FF0000) >> 16);
		    block[i++] = (byte)((Crc32.Crc32Result & 0xFF000000) >> 24);
		}
#endif

            // could inject other blocks here...

            return block;
        }



        // workitem 6513: when writing, use alt encoding only when ibm437 will not do
        private System.Text.Encoding GenerateCommentBytes()
        {
            _CommentBytes = ibm437.GetBytes(_Comment);
            string s1 = ibm437.GetString(_CommentBytes);
            if (s1 == _Comment)
                return ibm437;
            else
            {
                _CommentBytes = _provisionalAlternateEncoding.GetBytes(_Comment);
                return _provisionalAlternateEncoding;
            }
        }


        // workitem 6513
        private void GetEncodedBytes(out byte[] result)
        {
            // here, we need to flip the backslashes to forward-slashes, 
            // also, we need to trim the \\server\share syntax from any UNC path.
            // and finally, we need to remove any leading .\

            string SlashFixed = FileName.Replace("\\", "/");
            string s1 = null;
            if ((TrimVolumeFromFullyQualifiedPaths) && (FileName.Length >= 3)
                && (FileName[1] == ':') && (SlashFixed[2] == '/'))
            {
                // trim off volume letter, colon, and slash
                s1 = SlashFixed.Substring(3);
            }
            else if ((FileName.Length >= 4)
                && ((SlashFixed[0] == '/') && (SlashFixed[1] == '/')))
            {
                int n = SlashFixed.IndexOf('/', 2);
                //System.Console.WriteLine("input Path '{0}'", FileName);
                //System.Console.WriteLine("xformed: '{0}'", SlashFixed);
                //System.Console.WriteLine("third slash: {0}\n", n);
                if (n == -1)
                    throw new ArgumentException("The path for that entry appears to be badly formatted");
                s1 = SlashFixed.Substring(n + 1);
            }
            else if ((FileName.Length >= 3)
                && ((SlashFixed[0] == '.') && (SlashFixed[1] == '/')))
            {
                // trim off dot and slash
                s1 = SlashFixed.Substring(2);
            }
            else
            {
                s1 = SlashFixed;
            }

            // workitem 6513: when writing, use the alternative encoding only when ibm437 will not do.
            result = ibm437.GetBytes(s1);
            string s2 = ibm437.GetString(result);
            _CommentBytes = null;
            if (s2 == s1)
            {
                // file can be encoded with ibm437, now try comment

                // case 1: no comment.  use ibm437
                if (_Comment == null || _Comment.Length == 0)
                {
                    _actualEncoding = ibm437;
                    return;
                }

                // there is a comment.  Get the encoded form.
                System.Text.Encoding commentEncoding = GenerateCommentBytes();

                // case 2: if the comment also uses 437, we're good. 
                if (commentEncoding.CodePage == 437)
                {
                    _actualEncoding = ibm437;
                    return;
                }

                // case 3: comment requires non-437 code page.  Use the same
                // code page for the filename.
                _actualEncoding = commentEncoding;
                result = commentEncoding.GetBytes(s1);
                return;
            }
            else
            {
                // use the provisional encoding
                result = _provisionalAlternateEncoding.GetBytes(s1);
                if (_Comment != null && _Comment.Length != 0)
                {
                    _CommentBytes = _provisionalAlternateEncoding.GetBytes(_Comment);
                }

                _actualEncoding = _provisionalAlternateEncoding;
                return;
            }
        }


        private bool WantReadAgain()
        {
            if (_CompressedSize < _UncompressedSize) return false;
            else if (ForceNoCompression) return false;

            // check delegate 
            if (WillReadTwiceOnInflation != null)
                return WillReadTwiceOnInflation(_UncompressedSize, _CompressedSize, FileName);

            return true;
        }


        // heuristic - if the filename is one of a known list of non-compressible files, 
        // return false. else true.  We apply this by just checking the extension. 
        private bool SeemsCompressible(string filename)
        {
            string re = "^(?i)(.+)\\.(mp3|png|docx|xlsx|zip)$";
            if (RE.Regex.IsMatch(filename, re))
                return false;
            return true;
        }


        private bool DefaultWantCompression()
        {
            if (_LocalFileName != null)
                return SeemsCompressible(_LocalFileName);

            if (_FileNameInArchive != null)
                return SeemsCompressible(_FileNameInArchive);

            return true;
        }



        private void FigureCompressionMethodForWriting(int cycle)
        {
            // if we've already tried with compression... turn it off this time
            if (cycle > 1)
            {
                _CompressionMethod = 0x0;
            }
            // compression for directories = 0x00 (No Compression)
            else if (IsDirectory)
            {
                _CompressionMethod = 0x0;
            }
            else if (__FileDataPosition != 0)
            {
                // If at this point, __FileDataPosition is non-zero, that means we've read this
                // entry from an existing zip archive. 
                // 
                // In this case, we just keep the existing file data and metadata (including
                // CompressionMethod, CRC, compressed size, uncompressed size, etc).
                // 
                // All those member variables have been set during read! 
                // 
            }
            else
            {
                // If __FileDataPosition is zero, then that means we will get the data from a file
                // or stream.  

                // It is never possible to compress a zero-length file, so we check for 
                // this condition. 

                long fileLength = 0;
                if (_sourceStream != null)
                {
                    fileLength = _sourceStream.Length;
                }
                else
                {
                    // special case zero-length files
                    System.IO.FileInfo fi = new System.IO.FileInfo(LocalFileName);
                    fileLength = fi.Length;
                }

                if (fileLength == 0)
                    _CompressionMethod = 0x00;

                else if (_ForceNoCompression)
                    _CompressionMethod = 0x00;

                // Ok, we're getting the data to be compressed from a non-zero length file
                // or stream.  In that case we check the callback to see if the app
                // wants to tell us whether to compress or not.  

                else if (WantCompression != null)
                {
                    _CompressionMethod = (short)(WantCompression(LocalFileName, _FileNameInArchive)
                                 ? 0x08 : 0x00);
                }
                else
                {
                    // if there is no callback set, we use the default behavior.
                    _CompressionMethod = (short)(DefaultWantCompression()
                                 ? 0x08 : 0x00);
                }
            }
        }



        // write the header info for an entry
        private void WriteHeader(System.IO.Stream s, int cycle)
        {
            byte[] bytes = new byte[READBLOCK_SIZE];

            int i = 0;
            // signature
            bytes[i++] = (byte)(ZipConstants.ZipEntrySignature & 0x000000FF);
            bytes[i++] = (byte)((ZipConstants.ZipEntrySignature & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((ZipConstants.ZipEntrySignature & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((ZipConstants.ZipEntrySignature & 0xFF000000) >> 24);

            // version needed- see AppNote.txt
            // need v5.1 for strong encryption, or v2.0 for no encryption or for PK encryption.
            Int16 VersionNeededToExctract = (Int16)20;
            bytes[i++] = (byte)(VersionNeededToExctract & 0x00FF);
            bytes[i++] = (byte)((VersionNeededToExctract & 0xFF00) >> 8);

            // get byte array including any encoding
            byte[] FileNameBytes;
            // workitem 6513
            GetEncodedBytes(out FileNameBytes);
            Int16 filenameLength = (Int16)FileNameBytes.Length;

            // set the UTF8 bit if necessary
            bool setUtf8Bit = (ActualEncoding.CodePage == System.Text.Encoding.UTF8.CodePage);

            // general purpose bitfield

            // In the current implementation, this library uses only these bits 
            // in the GP bitfield:
            //  bit 0 = if set, indicates the entry is encrypted
            //  bit 3 = if set, indicates the CRC, C and UC sizes follow the file data.
            //  bit 6 = strong encryption (not implemented yet)
            //  bit 11 = UTF-8 encoding is used in the comment and filename

            _BitField = (Int16)((UsesEncryption) ? 1 : 0);
            if (UsesEncryption && (IsStrong(Encryption)))
                _BitField |= 0x0020;

            if (setUtf8Bit) _BitField |= 0x0800;

            // The PKZIP spec says that if bit 3 is set (0x0008) in the General Purpose BitField,
            // then the CRC, Compressed size, and uncompressed size are written directly after the
            // file data.   
            // 
            // Those 3 quantities are not knowable until after the compression is done. Yet they
            // are required to be in the header.  Normally, we'd 
            //  - write the header, using zeros for these quantities
            //  - compress the data, and incidentally compute these quantities.
            //  - seek back and write the correct values them into the header. 
            //
            // This is nice because it is simpler and less error prone to read the zip file.
            //
            // But if seeking in the output stream is not possible, then we need to set the
            // appropriate bitfield and emit these quantities after the compressed file data in
            // the output.

            if (!s.CanSeek)
                _BitField |= 0x0008;

            bytes[i++] = (byte)(_BitField & 0x00FF);
            bytes[i++] = (byte)((_BitField & 0xFF00) >> 8);

            // Here, we want to set values for Compressed Size, Uncompressed Size, and CRC.
            // If we have __FileDataPosition as nonzero, then that means we are reading this
            // zip entry from a zip file, and we have good values for those quantities. 
            // 
            // If _FileDataPosition is zero, then we zero those quantities, and We will compute
            // actual values for the following three quantities when we do the compression.
            if (this.__FileDataPosition == 0)
            {
                _UncompressedSize = 0;
                _CompressedSize = 0;
                _Crc32 = 0;
                _crcCalculated = false;
            }

            // set compression method here
            FigureCompressionMethodForWriting(cycle);

            // compression method         
            bytes[i++] = (byte)(CompressionMethod & 0x00FF);
            bytes[i++] = (byte)((CompressionMethod & 0xFF00) >> 8);

            // LastMod
            _TimeBlob = Ionic.Utils.Zip.SharedUtilities.DateTimeToPacked(LastModified);

            bytes[i++] = (byte)(_TimeBlob & 0x000000FF);
            bytes[i++] = (byte)((_TimeBlob & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_TimeBlob & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_TimeBlob & 0xFF000000) >> 24);

            // CRC - zero now, actual value will be calculated later
            bytes[i++] = (byte)(_Crc32 & 0x000000FF);
            bytes[i++] = (byte)((_Crc32 & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_Crc32 & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_Crc32 & 0xFF000000) >> 24);

            // CompressedSize (Int32) - zero now, actual value will be calculated later
            bytes[i++] = (byte)(_CompressedSize & 0x000000FF);
            bytes[i++] = (byte)((_CompressedSize & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_CompressedSize & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_CompressedSize & 0xFF000000) >> 24);

            // UncompressedSize (Int32) - zero now, actual value will be calculated later
            bytes[i++] = (byte)(_UncompressedSize & 0x000000FF);
            bytes[i++] = (byte)((_UncompressedSize & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_UncompressedSize & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_UncompressedSize & 0xFF000000) >> 24);

            // filename length (Int16)
            bytes[i++] = (byte)(filenameLength & 0x00FF);
            bytes[i++] = (byte)((filenameLength & 0xFF00) >> 8);

            byte[] extra = GetExtraField();

            // extra field length (short)
            Int16 ExtraFieldLength = (Int16)((extra == null) ? 0 : extra.Length);
            bytes[i++] = (byte)(ExtraFieldLength & 0x00FF);
            bytes[i++] = (byte)((ExtraFieldLength & 0xFF00) >> 8);

            // The filename written to the archive.
            int j = 0;
            // The buffer is already encoded; we just copy across the bytes.
            for (j = 0; (j < FileNameBytes.Length) && (i + j < bytes.Length); j++)
                bytes[i + j] = FileNameBytes[j];

            i += j;

            // extra field 
            if (extra != null)
            {
                for (j = 0; j < extra.Length; j++)
                {
                    bytes[i + j] = extra[j];
                }
                i += j;
            }

            // remember the offset, within the output stream, of this particular entry header
            var counter = s as CountingStream;
            _RelativeOffsetOfHeader = (int)((counter != null) ? counter.BytesWritten : s.Position);

            _LengthOfHeader = i;

            // finally, write the header to the stream
            s.Write(bytes, 0, i);

            // preserve this header data, we'll use it again later.
            // ..when seeking backward after we have the Crc, compressed and uncompressed sizes
            // ..and when writing the central directory structure.
            _EntryHeader = new byte[i];
            for (j = 0; j < i; j++)
                _EntryHeader[j] = bytes[j];
        }




        private Int32 FigureCrc32()
        {
            if (_crcCalculated == false)
            {
                Stream input = null;
                // get the original stream:
                if (_sourceStream != null)
                {
                    _sourceStream.Position = 0;
                    input = _sourceStream;
                }
                else
                {
                    input = System.IO.File.OpenRead(LocalFileName);
                }
                var crc32 = new CRC32();

                _Crc32 = crc32.GetCrc32(input);
                if (_sourceStream == null)
                {
                    input.Close();
                    input.Dispose();
                }
                _crcCalculated = true;
            }
            return _Crc32;
        }


        internal void CopyMetaData(ZipEntry source)
        {
            this.__FileDataPosition = source.__FileDataPosition;
            this.CompressionMethod = source.CompressionMethod;
            this._CompressedFileDataSize = source._CompressedFileDataSize;
            this._UncompressedSize = source._UncompressedSize;
            this._BitField = source._BitField;
            this._LastModified = source._LastModified;
        }


        private void _WriteFileData(ZipCrypto cipher, System.IO.Stream s)
        {
            // Read in the data from the input stream (often a file in the filesystem),
            // and write it to the output stream, calculating a CRC on it as we go.
            // We will also deflate and encrypt as necessary. 

            Stream input = null;
            CrcCalculatorStream input1 = null;
            CountingStream counter = null;
            try
            {
                // s.Position may fail on some write-only streams, eg stdout or System.Web.HttpResponseStream
                // We swallow that exception, because we don't care! 
                this.__FileDataPosition = s.Position;
            }
            catch { }

            try
            {
                // get the original stream:
                if (_sourceStream != null)
                {
                    _sourceStream.Position = 0;
                    input = _sourceStream;
                }
                else
                {
                    input = System.IO.File.OpenRead(LocalFileName);
                }

                // wrap a CRC Calculator Stream around the raw input stream. 
                input1 = new CrcCalculatorStream(input);

                // wrap a counting stream around the raw output stream:
                counter = new CountingStream(s);

                // maybe wrap an encrypting stream around that:
                Stream output1 = (Encryption == EncryptionAlgorithm.PkzipWeak) ?
                    (Stream)(new ZipCipherStream(counter, cipher, CryptoMode.Encrypt)) : counter;

                // maybe wrap a DeflateStream around that
                Stream output2 = null;
                bool mustCloseDeflateStream = false;
                if (CompressionMethod == 0x08)
                {
                    output2 = new DeflateStream(output1, CompressionMode.Compress, true);
                    mustCloseDeflateStream = true;
                }
                else
                    output2 = output1;

                int fileLength = 0;
                if (_sourceStream == null)
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(LocalFileName);
                    fileLength = (int)fi.Length;
                }

                // as we emit the file, we maybe deflate, then maybe encrypt, then write the bytes. 
                byte[] buffer = new byte[READBLOCK_SIZE];
                int n = input1.Read(buffer, 0, READBLOCK_SIZE);
                while (n > 0)
                {
                    output2.Write(buffer, 0, n);
                    OnWriteBlock(counter.BytesWritten, fileLength);
                    if (_ioOperationCanceled)
                        break;
                    n = input1.Read(buffer, 0, READBLOCK_SIZE);
                }

                // by calling Close() on the deflate stream, we write the footer bytes, as necessary.
                if (mustCloseDeflateStream)
                    output2.Close();

            }
            finally
            {
                if (_sourceStream == null && input != null)
                {
                    input.Close();
                    input.Dispose();
                }
            }

            if (_ioOperationCanceled)
                return;


            _UncompressedSize = input1.TotalBytesSlurped;
            _CompressedSize = counter.BytesWritten;

            _Crc32 = input1.Crc32;

            if ((_Password != null) && (Encryption == EncryptionAlgorithm.PkzipWeak))
            {
                _CompressedSize += 12; // 12 extra bytes for the encryption header
            }

            int i = 8;
            _EntryHeader[i++] = (byte)(CompressionMethod & 0x00FF);
            _EntryHeader[i++] = (byte)((CompressionMethod & 0xFF00) >> 8);

            i = 14;
            // CRC - the correct value now
            _EntryHeader[i++] = (byte)(_Crc32 & 0x000000FF);
            _EntryHeader[i++] = (byte)((_Crc32 & 0x0000FF00) >> 8);
            _EntryHeader[i++] = (byte)((_Crc32 & 0x00FF0000) >> 16);
            _EntryHeader[i++] = (byte)((_Crc32 & 0xFF000000) >> 24);

            // CompressedSize - the correct value now
            _EntryHeader[i++] = (byte)(_CompressedSize & 0x000000FF);
            _EntryHeader[i++] = (byte)((_CompressedSize & 0x0000FF00) >> 8);
            _EntryHeader[i++] = (byte)((_CompressedSize & 0x00FF0000) >> 16);
            _EntryHeader[i++] = (byte)((_CompressedSize & 0xFF000000) >> 24);

            // UncompressedSize - the correct value now
            _EntryHeader[i++] = (byte)(_UncompressedSize & 0x000000FF);
            _EntryHeader[i++] = (byte)((_UncompressedSize & 0x0000FF00) >> 8);
            _EntryHeader[i++] = (byte)((_UncompressedSize & 0x00FF0000) >> 16);
            _EntryHeader[i++] = (byte)((_UncompressedSize & 0xFF000000) >> 24);


            // workitem 6414
            if (s.CanSeek)
            {
                // seek in the raw output stream, to the beginning of the header for this entry.
                s.Seek(this._RelativeOffsetOfHeader, System.IO.SeekOrigin.Begin);

                // finally, write the updated header to the output stream
                s.Write(_EntryHeader, 0, _EntryHeader.Length);

                // adjust the count on the CountingStream as necessary
                var s1 = s as CountingStream;
                if (s1 != null) s1.Adjust(_EntryHeader.Length);

                // seek in the raw output stream, to the end of the file data for this entry
                s.Seek(_CompressedSize, System.IO.SeekOrigin.Current);
            }
            else
            {
                // eg, ASP.NET Response.OutputStream, or stdout

                if ((_BitField & 0x0008) != 0x0008)
                    throw new ZipException("Logic error.");


                byte[] Descriptor = new byte[16];
                i = 0;
                // signature
                int sig = ZipConstants.ZipEntryDataDescriptorSignature;
                Descriptor[i++] = (byte)(sig & 0x000000FF);
                Descriptor[i++] = (byte)((sig & 0x0000FF00) >> 8);
                Descriptor[i++] = (byte)((sig & 0x00FF0000) >> 16);
                Descriptor[i++] = (byte)((sig & 0xFF000000) >> 24);

                // CRC - the correct value now
                Descriptor[i++] = (byte)(_Crc32 & 0x000000FF);
                Descriptor[i++] = (byte)((_Crc32 & 0x0000FF00) >> 8);
                Descriptor[i++] = (byte)((_Crc32 & 0x00FF0000) >> 16);
                Descriptor[i++] = (byte)((_Crc32 & 0xFF000000) >> 24);

                // CompressedSize - the correct value now
                Descriptor[i++] = (byte)(_CompressedSize & 0x000000FF);
                Descriptor[i++] = (byte)((_CompressedSize & 0x0000FF00) >> 8);
                Descriptor[i++] = (byte)((_CompressedSize & 0x00FF0000) >> 16);
                Descriptor[i++] = (byte)((_CompressedSize & 0xFF000000) >> 24);

                // UncompressedSize - the correct value now
                Descriptor[i++] = (byte)(_UncompressedSize & 0x000000FF);
                Descriptor[i++] = (byte)((_UncompressedSize & 0x0000FF00) >> 8);
                Descriptor[i++] = (byte)((_UncompressedSize & 0x00FF0000) >> 16);
                Descriptor[i++] = (byte)((_UncompressedSize & 0xFF000000) >> 24);

                // finally, write the updated header to the output stream
                s.Write(Descriptor, 0, Descriptor.Length);
            }
        }



        internal void Write(System.IO.Stream outstream)
        {
            if (_Source == EntrySource.Zipfile)
            {
                CopyThroughOneEntry(outstream);
                return;
            }

            // Ok, the source for this entry is not a previously created zip file.  Therefore we
            // will need to process the bytestream (compute crc, maybe compress, maybe encrypt)
            // in order to create the zip.
            //
            // We do this in potentially 2 passes: The first time we do it as requested, maybe
            // with compression and maybe encryption.  If that causes the bytestream to inflate
            // in size, and if compression was on, then we turn off compression and do it again.

            bool readAgain = true;
            int nCycles = 0;
            do
            {
                nCycles++;

                // write the header:
                WriteHeader(outstream, nCycles);

                if (IsDirectory) return;  // nothing more to do! 

                ZipCrypto cipher = null;

                // now, write the actual file data. (incl the encrypted header)
                _EmitOne(outstream, out cipher);

                // The file data has now been written to the stream, and 
                // the file pointer is positioned directly after file data.

                if (nCycles > 1) readAgain = false;
                else if (!outstream.CanSeek) readAgain = false;
                else if (cipher != null && CompressedSize - 12 <= UncompressedSize) readAgain = false;
                else readAgain = WantReadAgain();

                if (readAgain)
                {
                    // seek back!
                    // seek in the raw output stream, to the beginning of the file data for this entry
                    outstream.Seek(_RelativeOffsetOfHeader, System.IO.SeekOrigin.Begin);

                    // if the last entry expands, we read again; but here, we must truncate the stream
                    // to prevent garbage data after the end-of-central-directory.
                    outstream.SetLength(outstream.Position);

                    // adjust the count on the CountingStream as necessary
                    var s1 = outstream as CountingStream;
                    if (s1 != null) s1.Adjust(_TotalEntrySize);
                }
            }
            while (readAgain);
        }




        private void _EmitOne(System.IO.Stream outstream, out ZipCrypto cipher)
        {
            // If PKZip (weak) encryption is in use, then the entry data is preceded by 
            // 12-byte "encryption header" for the entry.
            byte[] encryptionHeader = null;
            cipher = null;
            if (_Password != null && Encryption == EncryptionAlgorithm.PkzipWeak)
            {
                cipher = new ZipCrypto();
                // apply the password to the keys 
                cipher.InitCipher(_Password);

                // generate the random 12-byte header:
                var rnd = new System.Random();
                encryptionHeader = new byte[12];
                rnd.NextBytes(encryptionHeader);

                // Here, it is important to encrypt the random header, INCLUDING the final byte
                // which is the high-order byte of the CRC32.  We must do this before 
                // we encrypt the file data.  This step changes the state of the cipher, or in the
                // words of the PKZIP spec, it "further initializes" the cipher keys.

                // No way around this: must read the stream to compute the actual CRC
                FigureCrc32();
                encryptionHeader[11] = (byte)((this._Crc32 >> 24) & 0xff);

                byte[] cipherText = cipher.EncryptMessage(encryptionHeader, encryptionHeader.Length);

                // Write the ciphered bonafide encryption header. 
                outstream.Write(cipherText, 0, cipherText.Length);
            }

            // write the (potentially compressed, potentially encrypted) file data
            _WriteFileData(cipher, outstream);

            _TotalEntrySize = _LengthOfHeader + _CompressedSize;
        }



        private void CopyThroughOneEntry(System.IO.Stream outstream)
        {
            int n;
            byte[] bytes = new byte[READBLOCK_SIZE];

            // just read from the existing input zipfile and write to the output
            System.IO.Stream input = this.ArchiveStream;

            // seek to the beginning of the entry data (header + file data) in the stream
            input.Seek(this._RelativeOffsetOfHeader, System.IO.SeekOrigin.Begin);

            // Here, we need to grab-n-cache the header - it is used later when 
            // writing the Central Directory Structure.
            _EntryHeader = new byte[this._LengthOfHeader];
            n = input.Read(_EntryHeader, 0, _EntryHeader.Length);
            _CheckRead(n);

            // once again, seek to the beginning of the entry data in the input stream
            input.Seek(this._RelativeOffsetOfHeader, System.IO.SeekOrigin.Begin);

            // workitem 5616
            // remember the offset, within the output stream, of this particular entry header.
            // This may have changed if any of the other entries changed (eg, if a different
            // entry was removed or added.)
            var counter = outstream as CountingStream;
            _RelativeOffsetOfHeader = (int)((counter != null) ? counter.BytesWritten : outstream.Position);

            // copy through the header, filedata, everything...
            int Remaining = this._TotalEntrySize;
            while (Remaining > 0)
            {
                int len = (Remaining > bytes.Length) ? bytes.Length : Remaining;

                // read
                n = input.Read(bytes, 0, len);
                _CheckRead(n);

                // write
                outstream.Write(bytes, 0, n);
                Remaining -= n;
            }
        }



        static internal bool IsStrong(EncryptionAlgorithm e)
        {
            return ((e != EncryptionAlgorithm.None)
                && (e != EncryptionAlgorithm.PkzipWeak));
        }


        private DateTime _LastModified;
        private bool _TrimVolumeFromFullyQualifiedPaths = true;  // by default, trim them.
        private bool _ForceNoCompression;  // by default, false: do compression if it makes sense.
        private string _LocalFileName;
        private string _FileNameInArchive;
        private Int16 _VersionNeeded;
        private Int16 _BitField;
        private Int16 _CompressionMethod;
        private string _Comment;
        private bool _IsDirectory;
        private byte[] _CommentBytes;
        private Int32 _CompressedSize;
        private Int32 _CompressedFileDataSize; // CompressedSize less 12 bytes for the encryption header, if any
        private Int32 _UncompressedSize;
        private Int32 _TimeBlob;
        private bool _crcCalculated = false;
        private Int32 _Crc32;
        private byte[] _Extra;
        private bool _OverwriteOnExtract;
        private static System.Text.Encoding ibm437 = System.Text.Encoding.GetEncoding("IBM437");
        private System.Text.Encoding _provisionalAlternateEncoding = System.Text.Encoding.GetEncoding("IBM437");
        private System.Text.Encoding _actualEncoding = null;

        internal ZipFile _zipfile;
        private long __FileDataPosition;
        //private System.IO.MemoryStream _UnderlyingMemoryStream;
        //private System.IO.Compression.DeflateStream _CompressedStream;
        private byte[] _EntryHeader;
        private int _RelativeOffsetOfHeader;
        private int _LengthOfHeader;
        private int _TotalEntrySize;

        private string _Password;
        internal EntrySource _Source = EntrySource.None;
        private EncryptionAlgorithm _Encryption = EncryptionAlgorithm.None;
        private byte[] _WeakEncryptionHeader;
        private System.IO.Stream _archiveStream;
        private System.IO.Stream _sourceStream;
        private object LOCK = new object();
        private bool _ioOperationCanceled;

        private const int READBLOCK_SIZE = 0x2200;
    }
}