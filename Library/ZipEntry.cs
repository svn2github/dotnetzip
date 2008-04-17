// ZipEntry.cs
//
// Copyright (c) 2006, 2007, 2008 Microsoft Corporation.  All rights reserved.
//
// Part of an implementation of a zipfile class library. 
// See the file ZipFile.cs for the license and for further information.
//
// Tue, 27 Mar 2007  15:30


using System;

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
        //AES128, AES192, AES256, etc  // not implemented yet
    }

    /// <summary>
    /// Represents a single entry in a ZipFile. Typically, applications
    /// get a ZipEntry by enumerating the entries within a ZipFile. 
    /// </summary>
    public partial class ZipEntry
    {
        private ZipEntry() { }

        /// <summary>
        /// The time and date at which the file indicated by the ZipEntry was last modified. 
        /// </summary>
        public DateTime LastModified
        {
            get { return _LastModified; }
        }

        /// <summary>
        /// When this is set, this class trims the volume (eg C:\) from any 
        /// fully-qualified pathname on the ZipEntry, 
        /// before writing the ZipEntry into the ZipFile. This flag affects only 
        /// zip creation.  
        /// </summary>
        public bool TrimVolumeFromFullyQualifiedPaths
        {
            get { return _TrimVolumeFromFullyQualifiedPaths; }
            set { _TrimVolumeFromFullyQualifiedPaths = value; }
        }

        /// <summary>
        /// The name of the filesystem file, referred to by the ZipEntry. 
        /// This may be different than the path used in the archive itself.
        /// </summary>
        public string LocalFileName
        {
            get { return _LocalFileName; }
        }

        /// <summary>
        /// The name of the file contained in the ZipEntry. 
        /// When writing a zip, this path has backslashes replaced with 
        /// forward slashes, according to the zip spec, for compatibility
        /// with Unix and Amiga. 
        /// </summary>
        public string FileName
        {
            get { return _FileNameInArchive; }
        }
        /// <summary>
        /// The version of the zip engine needed to read the ZipEntry.  This is usually 0x14. 
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
        /// a bitfield as defined in the zip spec. 
        /// </summary>
        public Int16 BitField
        {
            get { return _BitField; }
        }

        /// <summary>
        /// The compression method employed for this ZipEntry. 0x08 = Deflate.  0x00 = Store (no compression). 
        /// Really, this should be an enum.  
        /// </summary>
        public Int16 CompressionMethod
        {
            get { return _CompressionMethod; }
        }

        /// <summary>
        /// The compressed size of the file, in bytes, within the zip archive. 
        /// </summary>
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
        /// The ratio of compressed size to uncompressed size. 
        /// </summary>
        public Double CompressionRatio
        {
            get
            {
                // this may return NaN
                return 100 * (1.0 - (1.0 * CompressedSize) / (1.0 * UncompressedSize));
            }
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
        /// archive.  This is a write-only property on the entry.
        /// </summary>
        public string Password
        {
            set { 
                _Password = value;
                Encryption = (_Password == null)?
                    EncryptionAlgorithm.None :
                    EncryptionAlgorithm.PkzipWeak;
            }
        }

        /// <summary>
        /// Specifies that the extraction should overwrite any existing files.
        /// This applies only when calling an Extract method.
        /// </summary>
        public bool OverwriteOnExtract
        {
            get { return _OverwriteOnExtract; }
            set { _OverwriteOnExtract = value; }
        }


        private byte[] _FileData
        {
            get
            {
                if (__filedata == null)
                {
                }
                return __filedata;
            }
        }

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


        internal byte[] Header
        {
            get
            {
                return _EntryHeader;
            }
        }


        private static bool ReadHeader(System.IO.Stream s, ZipEntry ze)
        {
            int signature = Ionic.Utils.Zip.Shared.ReadSignature(s);

            // Return false if this is not a local file header signature.
            if (ZipEntry.IsNotValidSig(signature))
            {
                s.Seek(-4, System.IO.SeekOrigin.Current); // unread the signature
                // Getting "not a ZipEntry signature" is not always wrong or an error. 
                // This can happen when walking through a zipfile.  After the last compressed entry, 
                // we expect to read a ZipDirEntry signature.  When we get this is how we 
                // know we've reached the end of the compressed entries. 
                if (ZipDirEntry.IsNotValidSig(signature))
                {
                    throw new Exception(String.Format("  ZipEntry::Read(): Bad signature ({0:X8}) at position  0x{1:X8}", signature, s.Position));
                }
                return false;
            }

            byte[] block = new byte[26];
            int n = s.Read(block, 0, block.Length);
            if (n != block.Length) return false;

            int i = 0;
            ze._VersionNeeded = (short)(block[i++] + block[i++] * 256);
            ze._BitField = (short)(block[i++] + block[i++] * 256);
            ze._CompressionMethod = (short)(block[i++] + block[i++] * 256);
            ze._LastModDateTime = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;

            // The PKZIP spec says that if bit 3 is set (0x0008) in the General Purpose BitField, then the CRC,
            // Compressed size, and uncompressed size come directly after the file data.  The only way to find
            // it is to scan the zip archive for the signature of the Data Descriptor, and presume that that
            // signature does not appear in the (compressed) data of the compressed file.

            if ((ze._BitField & 0x0008) != 0x0008)
            {
                ze._Crc32 = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
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
            n = s.Read(block, 0, block.Length);
            ze._FileNameInArchive = Ionic.Utils.Zip.Shared.StringFromBuffer(block, 0, block.Length);

            // when creating an entry by reading, the LocalFileName is the same as the FileNameInArchivre
            ze._LocalFileName = ze._FileNameInArchive;

            if (extraFieldLength > 0)
            {
                ze._Extra = new byte[extraFieldLength];
                n = s.Read(ze._Extra, 0, ze._Extra.Length);
            }

            // transform the time data into something usable
            ze._LastModified = Ionic.Utils.Zip.Shared.PackedToDateTime(ze._LastModDateTime);

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

                long posn = s.Position;
                long SizeOfDataRead = Ionic.Utils.Zip.Shared.FindSignature(s, ZipConstants.ZipEntryDataDescriptorSignature);
                if (SizeOfDataRead == -1) return false;

                // read 3x 4-byte fields (CRC, Compressed Size, Uncompressed Size)
                block = new byte[12];
                n = s.Read(block, 0, block.Length);
                if (n != 12) return false;
                i = 0;
                ze._Crc32 = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
                ze._CompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;
                ze._UncompressedSize = block[i++] + block[i++] * 256 + block[i++] * 256 * 256 + block[i++] * 256 * 256 * 256;

                if (SizeOfDataRead != ze._CompressedSize)
                    throw new Exception("Data format error (bit 3 is set)");

                // seek back to previous position, to read file data
                s.Seek(posn, System.IO.SeekOrigin.Begin);
            }

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
                n = s.Read(ze._WeakEncryptionHeader, 0, ze._WeakEncryptionHeader.Length);
                if (n != 12) return false;

                // decrease the compressed size by 12 bytes
                ze._CompressedSize -= 12;
            }

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
        /// <param name="s">the stream to read from.</param>
        /// <returns>the ZipEntry read from the stream.</returns>
        internal static ZipEntry Read(System.IO.Stream s)
        {
            ZipEntry entry = new ZipEntry();

            if (!ReadHeader(s, entry)) return null;

            entry.__filedata = new byte[entry.CompressedSize];

            int n = s.Read(entry._FileData, 0, entry._FileData.Length);
            if (n != entry._FileData.Length)
            {
                throw new Exception("badly formatted zip file.");
            }
            // finally, seek past the (already read) Data descriptor if necessary
            if ((entry._BitField & 0x0008) == 0x0008)
            {
                s.Seek(16, System.IO.SeekOrigin.Current);
            }
            return entry;
        }



        internal static ZipEntry Create(String filename)
        {
            return ZipEntry.Create(filename, null);
        }

        internal static ZipEntry Create(String filename, string DirectoryPathInArchive)
        {
            return Create(filename, DirectoryPathInArchive, null);
        }


        //Daniel Bedarf
        private bool _isStream;
        private System.IO.Stream _inputStream;
        internal static ZipEntry Create(String filename, string DirectoryPathInArchive, System.IO.Stream stream)
        {
            ZipEntry entry = new ZipEntry();
            if (stream != null)
            {
                entry._isStream = true;
                entry._inputStream = stream;
            }
            entry._LocalFileName = filename; // may include a path
            if (DirectoryPathInArchive == null)
                entry._FileNameInArchive = filename;
            else
            {
                // explicitly specify a pathname for this file  
                entry._FileNameInArchive =
                  System.IO.Path.Combine(DirectoryPathInArchive, System.IO.Path.GetFileName(filename));
            }

            // FIXME? - we set the last modified time of the entry in the zip to NOW. 
            // I'm thinking this should more accurately be, the lastmod time of the 
            // file in the filesystem.  I may be wrong though. 
            entry._LastModified = DateTime.Now; ;

            // adjust the time if the .NET BCL thinks it is in DST.  
            // see the note elsewhere in this file for more info. 
            if (entry._LastModified.IsDaylightSavingTime())
            {
                System.DateTime AdjustedTime = entry._LastModified - new System.TimeSpan(1, 0, 0);
                entry._LastModDateTime = Ionic.Utils.Zip.Shared.DateTimeToPacked(AdjustedTime);
            }
            else
                entry._LastModDateTime = Ionic.Utils.Zip.Shared.DateTimeToPacked(entry._LastModified);

            // we don't actually slurp in the file until the caller invokes Write on this entry.

            return entry;
        }


        /// <summary>
        /// Extract the entry to the filesystem, starting at the current working directory. 
        /// </summary>
        /// 
        /// <overloads>This method has a whole bunch of overloads.</overloads>
        ///         
        /// <remarks>
        /// <para>
        /// The last modified time of the created file may be adjusted 
        /// during extraction to compensate
        /// for differences in how the .NET Base Class Library deals
        /// with daylight saving time (DST) versus how the Windows
        /// filesystem deals with daylight saving time. 
        /// See http://blogs.msdn.com/oldnewthing/archive/2003/10/24/55413.aspx for more context.
        ///</para>
        /// <para>
        /// In a nutshell: Daylight savings time rules change regularly.  In
        /// 2007, for example, the inception week of DST changed.  In 1977,
        /// DST was in place all year round. in 1945, likewise.  And so on.
        /// Win32 does not attempt to guess which time zone rules were in
        /// effect at the time in question.  It will render a time as
        /// "standard time" and allow the app to change to DST as necessary.
        ///  .NET makes a different choice.
        ///</para>
        /// <para>
        /// Compare the output of FileInfo.LastWriteTime.ToString("f") with
        /// what you see in the property sheet for a file that was last
        /// written to on the other side of the DST transition. For example,
        /// suppose the file was last modified on October 17, during DST but
        /// DST is not currently in effect. Explorer's file properties
        /// reports Thursday, October 17, 2003, 8:45:38 AM, but .NETs
        /// FileInfo reports Thursday, October 17, 2003, 9:45 AM.
        ///</para>
        /// <para>
        /// Win32 says, "Thursday, October 17, 2002 8:45:38 AM PST". Note:
        /// Pacific STANDARD Time. Even though October 17 of that year
        /// occurred during Pacific Daylight Time, Win32 displays the time as
        /// standard time because that's what time it is NOW.
        ///</para>
        /// <para>
        /// .NET BCL assumes that the current DST rules were in place at the
        /// time in question.  So, .NET says, "Well, if the rules in effect
        /// now were also in effect on October 17, 2003, then that would be
        /// daylight time" so it displays "Thursday, October 17, 2003, 9:45
        /// AM PDT" - daylight time.
        ///</para>
        /// <para>
        /// So .NET gives a value which is more intuitively correct, but is
        /// also potentially incorrect, and which is not invertible. Win32
        /// gives a value which is intuitively incorrect, but is strictly
        /// correct.
        ///</para>
        /// <para>
        /// With this adjustment, I add one hour to the tweaked .NET time, if
        /// necessary.  That is to say, if the time in question had occurred
        /// in what the .NET BCL assumed to be DST (an assumption that may be
        /// wrong given the constantly changing DST rules).
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
        /// See the remarks on the non-parameterized version of the Extract() method, 
        /// for information on the last modified time of the created file.
        /// </para>
        /// </remarks>
        /// <param name="Overwrite">true if the caller wants to overwrite an existing file by the same name in the filesystem.</param>
        public void Extract(bool Overwrite)
        {
            OverwriteOnExtract = Overwrite;
            InternalExtract(".", null, null);
        }

        /// <summary>
        /// Extracts the entry to the specified stream. 
        /// For example, the caller could specify Console.Out, or a MemoryStream.
        /// </summary>
        /// <param name="s">the stream to which the entry should be extracted.  </param>
        public void Extract(System.IO.Stream s)
        {
            InternalExtract(null, null, s);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory. 
        /// </summary>
        /// <para>
        /// See the remarks on the non-parameterized version of the Extract() method, 
        /// for information on the last modified time of the created file.
        /// </para>
        /// <param name="BaseDirectory">the pathname of the base directory</param>
        public void Extract(string BaseDirectory)
        {
            InternalExtract(BaseDirectory, null, null);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory, 
        /// and potentially overwriting existing files in the filesystem. 
        /// </summary>
        /// <para>
        /// See the remarks on the non-parameterized version of the Extract() method, 
        /// for information on the last modified time of the created file.
        /// </para>
        /// <param name="BaseDirectory">the pathname of the base directory</param>
        /// <param name="Overwrite">If true, overwrite any existing files if necessary upon extraction.</param>
        public void Extract(string BaseDirectory, bool Overwrite)
        {
            OverwriteOnExtract = Overwrite;
            InternalExtract(BaseDirectory, null, null);
        }


        /// <summary>
        /// Extract the entry to the filesystem, using the current working directory,
        /// and using the specified password. 
        /// </summary>
        /// <para>
        /// See the remarks on the non-parameterized version of the Extract() method, 
        /// for information on the last modified time of the created file.
        /// </para>
        /// <param name="Password">the Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(string Password)
        {
            InternalExtract(".", Password, null);
        }


        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory,
        /// and using the specified password. 
        /// </summary>
        /// <para>
        /// See the remarks on the non-parameterized version of the Extract() method, 
        /// for information on the last modified time of the created file.
        /// </para>
        /// <param name="BaseDirectory">the pathname of the base directory.</param>
        /// <param name="Password">the Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(string BaseDirectory, string Password)
        {
            InternalExtract(BaseDirectory, Password, null);
        }

        /// <summary>
        /// Extract the entry to a file in the filesystem, potentially overwriting
        /// any existing file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// See the remarks on the non-parameterized version of the Extract() method, 
        /// for information on the last modified time of the created file.
        /// </para>
        /// </remarks>
        /// <param name="Overwrite">true if the caller wants to overwrite an existing file by the same name in the filesystem.</param>
        /// <param name="Password">the Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(bool Overwrite, string Password)
        {
            OverwriteOnExtract = Overwrite;
            InternalExtract(".", Password, null);
        }

        /// <summary>
        /// Extract the entry to the filesystem, starting at the specified base directory, 
        /// and potentially overwriting existing files in the filesystem. 
        /// </summary>
        /// <para>
        /// See the remarks on the non-parameterized version of the Extract() method, 
        /// for information on the last modified time of the created file.
        /// </para>
        /// <param name="BaseDirectory">the pathname of the base directory</param>
        /// <param name="Overwrite">If true, overwrite any existing files if necessary upon extraction.</param>
        /// <param name="Password">the Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(string BaseDirectory, bool Overwrite, string Password)
        {
            OverwriteOnExtract = Overwrite;
            InternalExtract(BaseDirectory, Password, null);
        }


        /// <summary>
        /// Extracts the entry to the specified stream, using the specified Password.
        /// For example, the caller could extract to Console.Out, or to a MemoryStream.
        /// </summary>
        /// <param name="s">the stream to which the entry should be extracted.  </param>
        /// <param name="Password">the Password to use for decrypting the entry.</param>
        public void ExtractWithPassword(System.IO.Stream s, string Password)
        {
            InternalExtract(null, Password, s);
        }




        // Pass in either basedir or s, but not both. 
        // In other words, you can extract to a stream or to a directory (filesystem), but not both!
        // The Password param is required for encrypted entries.
        private void InternalExtract(string basedir, string Password, System.IO.Stream s)
        {
            string TargetFile = null;
            if (basedir != null)
            {
                TargetFile = System.IO.Path.Combine(basedir, FileName);

                // check if a directory
                if ((IsDirectory) || (FileName.EndsWith("/")))
                {
                    if (!System.IO.Directory.Exists(TargetFile))
                        System.IO.Directory.CreateDirectory(TargetFile);
                    return;
                }
            }
            else if (s != null)
            {
                if ((IsDirectory) || (FileName.EndsWith("/")))
                    // extract a directory to streamwriter?  nothing to do!
                    return;
            }
            else throw new Exception("Invalid input.");

            byte[] ActualFileData = null;

            // decrypt file data here if necessary. 
            switch (Encryption)
            {
                case EncryptionAlgorithm.PkzipWeak:
                    {
                        if (Password == null)
                            throw new System.Exception("This entry requires a password.");

                        var cipher = new ZipCrypto();
                        cipher.InitCipher(Password);

                        // Decrypt the header.  This has a side effect of "further initializing the
                        // encryption keys" in the traditional zip encryption. 
                        byte[] DecryptedHeader = cipher.DecryptMessage(_WeakEncryptionHeader);

                        // CRC check
                        // According to the pkzip spec, the final byte in the decrypted header 
                        // is the highest-order byte in the CRC. We check it here. 
                        if (DecryptedHeader[11] != (byte)((_Crc32 >> 24) & 0xff))
                        {
                            throw new Exception("The password did not match.");
                        }

                        // We have a match. Now decrypt the file data itself.
                        // This is a memory-intensive implementation. If the entry is a 400k file, 
                        // we now have 2 400k byte arrays in memory...
                        ActualFileData = cipher.DecryptMessage(_FileData);
                    }
                    break;

                case EncryptionAlgorithm.None:
                    ActualFileData = _FileData;
                    break;
            }


            using (System.IO.MemoryStream memstream = new System.IO.MemoryStream(ActualFileData))
            {
                System.IO.Stream input = null;
                try
                {
                    // logic error bug (reported by computa_mike)
                    // cannot rely on sizes matching to determine if compression has been used! 
                    if (CompressionMethod == 0)
                    // if (CompressedSize == UncompressedSize)
                    {
                        // the System.IO.Compression.DeflateStream class does not handle uncompressed data.
                        // so if an entry is not compressed, then we just translate the bytes directly.
                        input = memstream;
                    }
                    else if (CompressionMethod == 0x08)  // deflate
                    {
                        input = new System.IO.Compression.DeflateStream(memstream, System.IO.Compression.CompressionMode.Decompress);
                    }
                    else
                    {
                        throw new Exception(String.Format("Unsupported Compression method ({0:X2})", CompressionMethod));
                    }

                    if (TargetFile != null)
                    {
                        // ensure the target path exists
                        if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(TargetFile)))
                        {
                            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(TargetFile));
                        }
                    }

                    System.IO.Stream output = null;
                    try
                    {
                        if (TargetFile != null)
                        {
                            if ((OverwriteOnExtract) && (System.IO.File.Exists(TargetFile)))
                            {
                                System.IO.File.Delete(TargetFile);
                            }
                            output = new System.IO.FileStream(TargetFile, System.IO.FileMode.CreateNew);
                        }
                        else
                            output = s;

                        byte[] bytes = new byte[4096];
                        int n;

                        if (_Debug)
                        {
                            Console.WriteLine("{0}: _FileData.Length= {1}", TargetFile, ActualFileData.Length);
                            Console.WriteLine("{0}: memstream.Position: {1}", TargetFile, memstream.Position);
                            n = _FileData.Length;
                            if (n > 1000)
                            {
                                n = 500;
                                Console.WriteLine("{0}: truncating dump from {1} to {2} bytes...", TargetFile, ActualFileData.Length, n);
                            }
                            for (int j = 0; j < n; j += 2)
                            {
                                if ((j > 0) && (j % 40 == 0))
                                    System.Console.WriteLine();
                                System.Console.Write(" {0:X2}", ActualFileData[j]);
                                if (j + 1 < n)
                                    System.Console.Write("{0:X2}", ActualFileData[j + 1]);
                            }
                            System.Console.WriteLine("\n");
                        }

                        n = 1; // anything non-zero
                        while (n != 0)
                        {
                            if (_Debug) Console.WriteLine("{0}: about to read...", TargetFile);
                            n = input.Read(bytes, 0, bytes.Length);
                            if (_Debug) Console.WriteLine("{0}: got {1} bytes", TargetFile, n);
                            if (n > 0)
                            {
                                if (_Debug) Console.WriteLine("{0}: about to write...", TargetFile);
                                output.Write(bytes, 0, n);
                            }
                        }

                        // somewhere in here we want to compute and validate the CRC. 
                    }
                    finally
                    {
                        // we only close the output stream if we opened it. 
                        if ((output != null) && (TargetFile != null))
                        {
                            output.Close();
                            output.Dispose();
                        }
                    }

                    if (TargetFile != null)
                    {
                        // We may have to adjust the last modified time to compensate
                        // for differences in how the .NET Base Class Library deals
                        // with daylight saving time (DST) versus how the Windows
                        // filesystem deals with daylight saving time. See 
                        // http://blogs.msdn.com/oldnewthing/archive/2003/10/24/55413.aspx for some context. 

                        // in a nutshell: Daylight savings time rules change regularly.  In
                        // 2007, for example, the inception week of DST changed.  In 1977,
                        // DST was in place all year round. in 1945, likewise.  And so on.
                        // Win32 does not attempt to guess which time zone rules were in
                        // effect at the time in question.  It will render a time as
                        // "standard time" and allow the app to change to DST as necessary.
                        //  .NET makes a different choice.

                        // -------------------------------------------------------
                        // Compare the output of FileInfo.LastWriteTime.ToString("f") with
                        // what you see in the property sheet for a file that was last
                        // written to on the other side of the DST transition. For example,
                        // suppose the file was last modified on October 17, during DST but
                        // DST is not currently in effect. Explorer's file properties
                        // reports Thursday, October 17, 2003, 8:45:38 AM, but .NETs
                        // FileInfo reports Thursday, October 17, 2003, 9:45 AM.

                        // Win32 says, "Thursday, October 17, 2002 8:45:38 AM PST". Note:
                        // Pacific STANDARD Time. Even though October 17 of that year
                        // occurred during Pacific Daylight Time, Win32 displays the time as
                        // standard time because that's what time it is NOW.

                        // .NET BCL assumes that the current DST rules were in place at the
                        // time in question.  So, .NET says, "Well, if the rules in effect
                        // now were also in effect on October 17, 2003, then that would be
                        // daylight time" so it displays "Thursday, October 17, 2003, 9:45
                        // AM PDT" - daylight time.

                        // So .NET gives a value which is more intuitively correct, but is
                        // also potentially incorrect, and which is not invertible. Win32
                        // gives a value which is intuitively incorrect, but is strictly
                        // correct.
                        // -------------------------------------------------------

                        // With this adjustment, I add one hour to the tweaked .NET time, if
                        // necessary.  That is to say, if the time in question had occurred
                        // in what the .NET BCL assumed to be DST (an assumption that may be
                        // wrong given the constantly changing DST rules).

                        if (LastModified.IsDaylightSavingTime())
                        {
                            DateTime AdjustedLastModified = LastModified + new System.TimeSpan(1, 0, 0);
                            System.IO.File.SetLastWriteTime(TargetFile, AdjustedLastModified);
                        }
                        else
                            System.IO.File.SetLastWriteTime(TargetFile, LastModified);
                    }
                }
                finally
                {
                    // we only close the output stream if we opened it. 
                    // we cannot use using() here because in some cases we do not want to Dispose the stream!
                    if ((input != null) && (input != memstream))
                    {
                        input.Close();
                        input.Dispose();
                    }
                }
            }
        }


        internal void MarkAsDirectory()
        {
            _IsDirectory = true;
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
            bytes[i++] = Header[4];
            bytes[i++] = Header[5];

            // Version Needed, Bitfield, compression method, lastmod,
            // crc, compressed and uncompressed sizes, filename length and extra field length -
            // are all the same as the local file header. So just copy them
            int j = 0;
            for (j = 0; j < 26; j++)
                bytes[i + j] = Header[4 + j];

            i += j;  // positioned at next available byte

            int commentLength = 0;
            // File (entry) Comment Length
            if ((Comment == null) || (Comment.Length == 0))
            {
                // no comment!
                bytes[i++] = (byte)0;
                bytes[i++] = (byte)0;
            }
            else
            {
                commentLength = Comment.Length;
                // the size of our buffer defines the max length of the comment we can write
                if (commentLength + i > bytes.Length) commentLength = bytes.Length - i;
                bytes[i++] = (byte)(commentLength & 0x00FF);
                bytes[i++] = (byte)((commentLength & 0xFF00) >> 8);
            }

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

            if (_Debug) System.Console.WriteLine("\ninserting filename into CDS: (length= {0})", Header.Length - 30);
            // actual filename (starts at offset 34 in header) 
            for (j = 0; j < Header.Length - 30; j++)
            {
                bytes[i + j] = Header[30 + j];
                if (_Debug) System.Console.Write(" {0:X2}", bytes[i + j]);
            }
            if (_Debug) System.Console.WriteLine();
            i += j;

            // "Extra field"
            // in this library, it is always nothing

            // file (entry) comment
            if (commentLength != 0)
            {
                char[] c = Comment.ToCharArray();
                // now actually write the comment itself into the byte buffer
                for (j = 0; (j < commentLength) && (i + j < bytes.Length); j++)
                {
                    bytes[i + j] = System.BitConverter.GetBytes(c[j])[0];
                }
                i += j;
            }

            s.Write(bytes, 0, i);
        }



        private byte[] GetExtraField()
        {
            if ((UsesEncryption) && (IsStrong(Encryption)))
            {
                // byte[] block= GetStrongEncryptionBlock();
                // return block;
                return null;
            }

            // could inject other blocks here...

            return null;
        }



        private void WriteHeader(System.IO.Stream s, byte[] bytes)
        {
            // write the header info for an entry

            int i = 0;
            // signature
            bytes[i++] = (byte)(ZipConstants.ZipEntrySignature & 0x000000FF);
            bytes[i++] = (byte)((ZipConstants.ZipEntrySignature & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((ZipConstants.ZipEntrySignature & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((ZipConstants.ZipEntrySignature & 0xFF000000) >> 24);

            // version needed- see AppNote.txt
            // need v5.1 for strong encryption, or v2.0 for no encryption.
            Int16 FixedVersionNeeded = (Int16)20;
            bytes[i++] = (byte)(FixedVersionNeeded & 0x00FF);
            bytes[i++] = (byte)((FixedVersionNeeded & 0xFF00) >> 8);


            // general purpose bitfield
            // bit  0 - set if encryption is used.
            // b. 1-2 - set to determine whether normal, max, fast deflation.  
            //          This library always leaves these bits unset when writing (indicating 
            //          "normal" deflation").

            // bit  3 - indicates crc32, compressed and uncompressed sizes are zero in
            //          local header.  We always leave this as zero on writing, but can read
            //          a zip with it nonzero. 

            // bit  4 - reserved for "enhanced deflating". This library doesn't do enhanced deflating.
            // bit  5 - set to indicate the zip is compressed patched data.  This library doesn't do that.
            // bit  6 - set if strong encryption is used (must also set bit 1 if bit 6 is set)
            // bit  7 - unused
            // bit  8 - unused
            // bit  9 - unused
            // bit 10 - unused
            // Bit 11 - Language encoding flag (EFS).  If this bit is set,
            //          the filename and comment fields for this file
            //          must be encoded using UTF-8. This library currently does not support UTF-8.
            // Bit 12 - Reserved by PKWARE for enhanced compression.
            // Bit 13 - Used when encrypting the Central Directory to indicate 
            //          selected data values in the Local Header are masked to
            //          hide their actual values.  See the section describing 
            //          the Strong Encryption Specification for details.
            // Bit 14 - Reserved by PKWARE.
            // Bit 15 - Reserved by PKWARE.

            // The short story is that in the current implementation, the only thing
            // this library potentially writes to the general purpose Bitfield is
            // encryption indicators.
            Int16 BitField = (Int16)((UsesEncryption) ? 1 : 0);
            if (UsesEncryption && (IsStrong(Encryption)))
                BitField |= 0x20;

            bytes[i++] = (byte)(BitField & 0x00FF);
            bytes[i++] = (byte)((BitField & 0xFF00) >> 8);

            Int16 CompressionMethod = 0x00; // 0x08 = Deflate, 0x00 == No Compression

            // compression for directories = 0x00 (No Compression)

            if (!IsDirectory)
            {
                CompressionMethod = 0x08;
                // CRC32 (Int32)
                if (_FileData != null)
                {
                    // If at this point, _FileData is non-null, that means we've read this
                    // entry from an existing zip archive. We must just copy the existing
                    // file data, CRC, compressed size, and uncompressed size over to the
                    // new (updated) archive.
                }
                else
                {
                    // If _FileData is null, then that means we will get the data from a file
                    // or stream.  In that case we need to read the file or stream, and
                    // compute the CRC, and compressed and uncompressed sizes from that
                    // source.

                    //Daniel Bedarf
                    long fileLength = 0;
                    if (_isStream)
                    {
                        fileLength = _inputStream.Length;
                    }
                    else
                    {
                        // special case zero-length files
                        System.IO.FileInfo fi = new System.IO.FileInfo(LocalFileName);
                        fileLength = fi.Length;
                    }
                    if (fileLength == 0)
                    {
                        CompressionMethod = 0x00;
                        _UncompressedSize = 0;
                        _CompressedSize = 0;
                        _Crc32 = 0;

                    }
                    else
                    {
                        // Read in the data from the file in the filesystem, compress it, and 
                        // calculate a CRC on it as we read. 

                        CRC32 crc32 = new CRC32();
                        // Daniel Bedarf
                        if (_isStream)
                        {
                            _inputStream.Position = 0;
                            UInt32 crc = crc32.GetCrc32AndCopy(_inputStream, CompressedStream);
                            _Crc32 = (Int32)crc;
                        }
                        else
                        {
                            using (System.IO.Stream input = System.IO.File.OpenRead(LocalFileName))
                            {
                                UInt32 crc = crc32.GetCrc32AndCopy(input, CompressedStream);
                                _Crc32 = (Int32)crc;
                            }
                        }
                        CompressedStream.Close();  // to get the footer bytes written to the underlying stream
                        _CompressedStream = null;

                        _UncompressedSize = crc32.TotalBytesRead;
                        _CompressedSize = (Int32)_UnderlyingMemoryStream.Length;

                        // It is possible that applying this stream compression on a previously compressed
                        // file (entry) (like a zip, jpg or png) or a very small file will actually result
                        // in an increase in the size of the data.  In that case, we discard the
                        // compressed bytes, store the uncompressed data, and mark the CompressionMethod
                        // as 0x00 (uncompressed).  When we do this we need to recompute the CRC, and
                        // fill the _UnderlyingMemoryStream with the right (raw) data.

                        if (_CompressedSize > _UncompressedSize)
                        {
                            //Daniel Bedarf
                            if (_isStream)
                            {
                                _UnderlyingMemoryStream = new System.IO.MemoryStream();
                                _inputStream.Position = 0;
                                UInt32 crc = crc32.GetCrc32AndCopy(_inputStream, _UnderlyingMemoryStream);
                                _Crc32 = (Int32)crc;
                            }
                            else
                            {
                                // read the file again
                                _UnderlyingMemoryStream = new System.IO.MemoryStream();
                                using (System.IO.Stream input = System.IO.File.OpenRead(LocalFileName))
                                {
                                    UInt32 crc = crc32.GetCrc32AndCopy(input, _UnderlyingMemoryStream);
                                    _Crc32 = (Int32)crc;
                                }
                            }
                            _UncompressedSize = crc32.TotalBytesRead;
                            _CompressedSize = (Int32)_UnderlyingMemoryStream.Length;
                            if (_CompressedSize != _UncompressedSize) throw new Exception("No compression but unequal stream lengths!");
                            CompressionMethod = 0x00;
                        }
                    }
                }
            }

            // compression method         
            bytes[i++] = (byte)(CompressionMethod & 0x00FF);
            bytes[i++] = (byte)((CompressionMethod & 0xFF00) >> 8);

            // LastMod
            bytes[i++] = (byte)(_LastModDateTime & 0x000000FF);
            bytes[i++] = (byte)((_LastModDateTime & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_LastModDateTime & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_LastModDateTime & 0xFF000000) >> 24);

            // CRC - calculated above
            bytes[i++] = (byte)(_Crc32 & 0x000000FF);
            bytes[i++] = (byte)((_Crc32 & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_Crc32 & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_Crc32 & 0xFF000000) >> 24);

            // CompressedSize (Int32)
            if ((_Password != null) && (Encryption == EncryptionAlgorithm.PkzipWeak))
            {
                _CompressedSize += 12; // bytes for the encryption header
            }
            bytes[i++] = (byte)(_CompressedSize & 0x000000FF);
            bytes[i++] = (byte)((_CompressedSize & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_CompressedSize & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_CompressedSize & 0xFF000000) >> 24);

            // UncompressedSize (Int32)
            if (_Debug) System.Console.WriteLine("Uncompressed Size: {0}", _UncompressedSize);
            bytes[i++] = (byte)(_UncompressedSize & 0x000000FF);
            bytes[i++] = (byte)((_UncompressedSize & 0x0000FF00) >> 8);
            bytes[i++] = (byte)((_UncompressedSize & 0x00FF0000) >> 16);
            bytes[i++] = (byte)((_UncompressedSize & 0xFF000000) >> 24);

            // filename length (Int16)
            Int16 filenameLength = (Int16)FileName.Length;
            // see note below about TrimVolumeFromFullyQualifiedPaths.
            if ((TrimVolumeFromFullyQualifiedPaths) && (FileName[1] == ':') && (FileName[2] == '\\')) filenameLength -= 3;
            // apply upper bound to the length
            if (filenameLength + i > bytes.Length) filenameLength = (Int16)(bytes.Length - (Int16)i);
            bytes[i++] = (byte)(filenameLength & 0x00FF);
            bytes[i++] = (byte)((filenameLength & 0xFF00) >> 8);


            byte[] extra = GetExtraField();

            // extra field length (short)
            Int16 ExtraFieldLength = (Int16)((extra == null) ? 0 : extra.Length);
            bytes[i++] = (byte)(ExtraFieldLength & 0x00FF);
            bytes[i++] = (byte)((ExtraFieldLength & 0xFF00) >> 8);

            // Tue, 27 Mar 2007  16:35

            // Creating a zip that contains entries with "fully qualified" pathnames
            // can result in a zip archive that is unreadable by Windows Explorer.
            // Such archives are valid according to other tools but not to explorer.
            // To avoid this, we can trim off the leading volume name and slash (eg
            // c:\) when creating (writing) a zip file.  We do this by default and we
            // leave the old behavior available with the
            // TrimVolumeFromFullyQualifiedPaths flag - set it to false to get the old
            // behavior.  It only affects zip creation.

            // Tue, 05 Feb 2008  12:25
            // Replace backslashes with forward slashes in the archive

            // the filename written to the archive
            char[] c = ((TrimVolumeFromFullyQualifiedPaths) && (FileName[1] == ':') && (FileName[2] == '\\')) ?
          FileName.Substring(3).Replace("\\", "/").ToCharArray() :  // trim off volume letter, colon, and slash
          FileName.Replace("\\", "/").ToCharArray();

            int j = 0;

            if (_Debug)
            {
                System.Console.WriteLine("local header: writing filename, {0} chars", c.Length);
                System.Console.WriteLine("starting offset={0}", i);
            }

            for (j = 0; (j < c.Length) && (i + j < bytes.Length); j++)
            {
                bytes[i + j] = System.BitConverter.GetBytes(c[j])[0];
                if (_Debug) System.Console.Write(" {0:X2}", bytes[i + j]);
            }
            if (_Debug) System.Console.WriteLine();

            i += j;


            // extra field (at this time, this includes only the Strong Encryption Block, as necessary)
            if (extra != null)
            {
                for (j = 0; j < extra.Length; j++)
                    bytes[i + j] = extra[j];

                i += j;
            }

            // remember the offset, within the stream, of this particular entry header
            _RelativeOffsetOfHeader = (int)s.Length;

            if (_Debug)
            {
                System.Console.WriteLine("\nAll header data:");
                for (j = 0; j < i; j++)
                    System.Console.Write(" {0:X2}", bytes[j]);
                System.Console.WriteLine();
            }
            // finally, write the header to the stream
            s.Write(bytes, 0, i);

            // preserve this header data for use with the central directory structure.
            _EntryHeader = new byte[i];
            if (_Debug) System.Console.WriteLine("preserving header of {0} bytes", _EntryHeader.Length);
            for (j = 0; j < i; j++)
                _EntryHeader[j] = bytes[j];
        }


        internal void Write(System.IO.Stream s)
        {
            byte[] bytes = new byte[4096];
            int n;

            // write the header:
            WriteHeader(s, bytes);

            if (IsDirectory) return;  // nothing more to do! (need to close memory stream?)

            if (_Debug)
            {
                Console.WriteLine("{0}: writing compressed data to zipfile...", FileName);
                Console.WriteLine("{0}: total data length: {1}", FileName, _CompressedSize);
            }

            if (_CompressedSize == 0)
            {
                // nothing more to write. 
                // (and, I Think we do not want to close the memory stream.)

                // if (_UnderlyingMemoryStream != null)
                // {
                //   _UnderlyingMemoryStream.Close();
                //   _UnderlyingMemoryStream = null;
                // }
                return;
            }

            // write the actual file data: 
            if (_FileData != null)
            {
                // use the existing compressed data we read from the extant zip archive
                s.Write(_FileData, 0, _FileData.Length);
            }
            else
            {
                // _FileData is null.

                // In this case, we rely on the compressed data that was placed 
                // in the _UnderlyingMemoryStream, in the WriteHeader() method).

                _UnderlyingMemoryStream.Position = 0;

                ZipCrypto cipher = null;
                if ((_Password != null) && (Encryption == EncryptionAlgorithm.PkzipWeak))
                {
                    cipher = new ZipCrypto();

                    // apply the password to the keys 
                    cipher.InitCipher(_Password);

                    // generate the random 12-byte header:
                    var rnd = new System.Random();
                    var Header = new byte[12];
                    rnd.NextBytes(Header);
                    Header[11]= (byte)((this._Crc32 >>24) & 0xff);

                    byte[] EncryptedHeader = cipher.EncryptMessage(Header);

                    // Write the encryption header. 
                    s.Write(EncryptedHeader, 0, EncryptedHeader.Length);
                }

                while ((n = _UnderlyingMemoryStream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    if (_Debug)
                    {
                        Console.WriteLine("{0}: transferring {1} bytes...", FileName, n);
                        for (int j = 0; j < n; j += 2)
                        {
                            if ((j > 0) && (j % 40 == 0))
                                System.Console.WriteLine();
                            System.Console.Write(" {0:X2}", bytes[j]);
                            if (j + 1 < n)
                                System.Console.Write("{0:X2}", bytes[j + 1]);
                        }
                        System.Console.WriteLine("\n");
                    }

                    if ((_Password != null) && (Encryption == EncryptionAlgorithm.PkzipWeak))
                    {
                        byte[] c = cipher.EncryptMessage(bytes);
                        s.Write(c, 0, n);
                    }
                    else
                        s.Write(bytes, 0, n);
                }

                //_CompressedStream.Close();
                //_CompressedStream= null;
                _UnderlyingMemoryStream.Close();
                _UnderlyingMemoryStream = null;
            }
        }


        internal bool IsStrong(EncryptionAlgorithm e)
        {
            return ((e != EncryptionAlgorithm.None)
                && (e != EncryptionAlgorithm.PkzipWeak));
        }


        private bool _Debug = false;

        private DateTime _LastModified;
        private bool _TrimVolumeFromFullyQualifiedPaths = true;  // by default, trim them.
        private string _LocalFileName;
        private string _FileNameInArchive;
        private Int16 _VersionNeeded;
        private Int16 _BitField;
        private Int16 _CompressionMethod;
        private string _Comment;
        private bool _IsDirectory;
        private Int32 _CompressedSize;
        private Int32 _UncompressedSize;
        private Int32 _LastModDateTime;
        private Int32 _Crc32;
        private byte[] _Extra;

        private bool _OverwriteOnExtract = false;

        private byte[] __filedata;
        private System.IO.MemoryStream _UnderlyingMemoryStream;
        private System.IO.Compression.DeflateStream _CompressedStream;
        private byte[] _EntryHeader;
        private int _RelativeOffsetOfHeader;

        private string _Password;
        private EncryptionAlgorithm _Encryption = EncryptionAlgorithm.None;
        private byte[] _WeakEncryptionHeader;

    }

}
