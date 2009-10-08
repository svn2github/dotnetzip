// Streams.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa 
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License. 
// See the file License.txt for the license details.
// More info on: http://dotnetzip.codeplex.com
//
// ------------------------------------------------------------------
//
// last saved (in emacs): 
// Time-stamp: <2009-October-07 18:49:17>
//
// ------------------------------------------------------------------
//
// This module defines tests for Streams interfaces into DotNetZip.  
// ZipOutputStream, ZipInputStream, etc
//
// ------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Zip;
using Ionic.Zip.Tests.Utilities;


namespace Ionic.Zip.Tests.Streams
{
    /// <summary>
    /// Summary description for StreamsTests
    /// </summary>
    [TestClass]
    public class StreamsTests : IonicTestClass
    {
        public StreamsTests() : base() { }


        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanupEx()
        {
        }

        EncryptionAlgorithm[] crypto =
            {
                EncryptionAlgorithm.None, 
                EncryptionAlgorithm.PkzipWeak,
                EncryptionAlgorithm.WinZipAes128,
                EncryptionAlgorithm.WinZipAes256,
            };

#if NOT
        EncryptionAlgorithm[] cryptoNoPkzip = 
            {
                EncryptionAlgorithm.None, 
                EncryptionAlgorithm.WinZipAes128,
                EncryptionAlgorithm.WinZipAes256,
            };
#endif

        Ionic.Zlib.CompressionLevel[] compLevels =
            {
                Ionic.Zlib.CompressionLevel.None,
                Ionic.Zlib.CompressionLevel.BestSpeed,
                Ionic.Zlib.CompressionLevel.Default,
                Ionic.Zlib.CompressionLevel.BestCompression,
            };

        Zip64Option[] z64 =
            {
                Zip64Option.Never,
                Zip64Option.AsNecessary, 
                Zip64Option.Always,
            };


        [TestMethod]
        public void ReadZip_OpenReader()
        {
            string[] passwords = { null, Path.GetRandomFileName(), "EE", "***()" };

            for (int j = 0; j < compLevels.Length; j++)
            {
                for (int k = 0; k < passwords.Length; k++)
                {
                    string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("ReadZip_OpenReader-{0}-{1}.zip", j, k));

                    int entriesAdded = 0;
                    String filename = null;

                    string subdir = Path.Combine(TopLevelDir, String.Format("A{0}{1}", j, k));
                    Directory.CreateDirectory(subdir);

                    int fileCount = _rnd.Next(10) + 10;
                    for (int i = 0; i < fileCount; i++)
                    {
                        filename = Path.Combine(subdir, String.Format("file{0:D2}.txt", i));
                        int filesize = _rnd.Next(34000) + 5000;
                        TestUtilities.CreateAndFillFileText(filename, filesize);
                        entriesAdded++;
                    }

                    using (ZipFile zip1 = new ZipFile())
                    {
                        zip1.CompressionLevel = compLevels[j];
                        zip1.Password = passwords[k];
                        zip1.AddDirectory(subdir, Path.GetFileName(subdir));
                        zip1.Save(zipFileToCreate);
                    }

                    // Verify the files are in the zip
                    Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), entriesAdded,
                                         String.Format("Trial {0}-{1}: The Zip file has the wrong number of entries.", j, k));

                    // now extract the files and verify their contents
                    using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
                    {

                        for (int i = 0; i < 3; i++)
                        {
                            // try once with Password set on ZipFile,
                            // another with password on the entry, and
                            // a third time with password passed into the OpenReader() method.
                            if (i == 0)
                                zip2.Password = passwords[k];

                            foreach (string eName in zip2.EntryFileNames)
                            {
                                ZipEntry e1 = zip2[eName];

                                if (!e1.IsDirectory)
                                {

                                    Ionic.Zlib.CrcCalculatorStream s = null;
                                    try
                                    {
                                        if (i == 0)
                                            s = e1.OpenReader();
                                        else if (i == 1)
                                            s = e1.OpenReader(passwords[k]);
                                        else
                                        {
                                            e1.Password = passwords[k];
                                            s = e1.OpenReader();
                                        }
                                        string outFile = Path.Combine(TopLevelDir, String.Format("{0}.{1}.out", eName, i));
                                        using (var output = File.Create(outFile))
                                        {
                                            byte[] buffer = new byte[4096];
                                            int n, totalBytesRead = 0;
                                            do
                                            {
                                                n = s.Read(buffer, 0, buffer.Length);
                                                totalBytesRead += n;
                                                output.Write(buffer, 0, n);
                                            } while (n > 0);

                                            output.Flush();
                                            output.Close();
                                            TestContext.WriteLine("CRC expected({0:X8}) actual({1:X8})",
                                                                  e1.Crc, s.Crc);

                                            Assert.AreEqual<Int32>(s.Crc, e1.Crc,
                                                                   string.Format("The Entry {0} failed the CRC Check.", eName));

                                            Assert.AreEqual<Int32>(totalBytesRead, (int)e1.UncompressedSize,
                                                                   string.Format("We read an unexpected number of bytes. ({0})", eName));
                                        }
                                    }
                                    finally
                                    {
                                        if (s != null)
                                            s.Close();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }



        
        [TestMethod]
        public void AddEntry_JitProvided()
        {
            for (int i = 0; i < crypto.Length; i++)
            {
                for (int k = 0; k < compLevels.Length; k++)
                {
                    string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("AddEntry_JitProvided.{0}.{1}.zip", i, k));

                    Directory.SetCurrentDirectory(TopLevelDir);
                    string dirToZip = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                    var files = TestUtilities.GenerateFilesFlat(dirToZip);

                    string password = Path.GetRandomFileName();

                    using (var zip = new ZipFile())
                    {
                        TestContext.WriteLine("=================================");
                        TestContext.WriteLine("Creating {0}...", Path.GetFileName(zipFileToCreate));
                        TestContext.WriteLine("Encryption({0})  Compression({1})  pw({2})",
                                              crypto[i].ToString(), compLevels[k].ToString(), password);

                        zip.Password = password;
                        zip.Encryption = crypto[i];
                        zip.CompressionLevel = compLevels[k];

                        foreach (var file in files)
                            zip.AddEntry(file,
                                         (name) => File.Open(name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                                         (name, stream) => stream.Close()
                                         );
                        zip.Save(zipFileToCreate);
                    }

                    if (crypto[i] == EncryptionAlgorithm.None)
                        WinzipVerify(zipFileToCreate);
                    else
                        WinzipVerify(zipFileToCreate, password);

                    Assert.AreEqual<int>(files.Length, TestUtilities.CountEntries(zipFileToCreate),
                                         "Trial ({0},{1}): The zip file created has the wrong number of entries.", i, k);
                }
            }
        }



        private delegate void TestCompressionLevels(string[] files, EncryptionAlgorithm crypto, bool seekable, int cycle, string format);

        [TestMethod]
        public void AddEntry_WriteDelegate()
        {
            _TestDriver(new TestCompressionLevels(_Internal_AddEntry_WriteDelegate), "WriteDelegate", true, false);
        }


        [TestMethod]
        public void AddEntry_WriteDelegate_NonSeekable()
        {
            _TestDriver(new TestCompressionLevels(_Internal_AddEntry_WriteDelegate), "WriteDelegate", false, false);
        }


        [TestMethod]
        public void AddEntry_WriteDelegate_ZeroBytes_wi8931()
        {
            _TestDriver(new TestCompressionLevels(_Internal_AddEntry_WriteDelegate), "WriteDelegate", true, true);
        }


        private void _TestDriver(TestCompressionLevels test, string label, bool seekable, bool zero)
        {
            Directory.SetCurrentDirectory(TopLevelDir);


            int[] fileCounts = new int[] { 1, 2, _rnd.Next(4) + 3, _rnd.Next(14) + 13 };

            for (int j = 0; j < fileCounts.Length; j++)
            {
                string dirToZip = String.Format("subdir{0}", j);
                string[] files = null;
                if (zero)
                {
                    // zero length files
                    Directory.CreateDirectory(dirToZip);
                    files = new string[fileCounts[j]];
                    for (int i = 0; i < fileCounts[j]; i++)
                        files[i] = TestUtilities.CreateUniqueFile("zerolength", dirToZip);
                }
                else
                    files = TestUtilities.GenerateFilesFlat(dirToZip, fileCounts[j], 40000, 72000);


                for (int i = 0; i < crypto.Length; i++)
                {
                    string format = String.Format("{0}.{1}.filecount{2}.Encryption.{3}.{4}seekable.{5}.zip",
                                                  label,
                                                  (zero) ? "ZeroBytes" : "regular",
                                                  fileCounts[j],
                                                  crypto[i].ToString(),
                                                  seekable ? "" : "non",
                                                  "{0}");

                    test(files, crypto[i], seekable, i, format);
                }
            }
        }



        private void _Internal_AddEntry_WriteDelegate(string[] files,
                                                      EncryptionAlgorithm crypto,
                                                      bool seekable,
                                                      int cycle,
                                                      string format)
        {
            int BufferSize = 2048;


            for (int k = 0; k < compLevels.Length; k++)
            {
                string zipFileToCreate = Path.Combine(TopLevelDir, String.Format(format, k));
                string password = Path.GetRandomFileName();

                using (var zip = new ZipFile())
                {
                    TestContext.WriteLine("=================================");
                    TestContext.WriteLine("Creating {0}...", Path.GetFileName(zipFileToCreate));
                    TestContext.WriteLine("Encryption({0})  Compression({1})  pw({2})",
                                          crypto.ToString(), compLevels[k].ToString(), password);

                    zip.Password = password;
                    zip.Encryption = crypto;
                    zip.CompressionLevel = compLevels[k];

                    foreach (var file in files)
                    {
                        zip.AddEntry(file, (name, output) =>
                            {
                                using (var input = File.Open(name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                {
                                    byte[] buffer = new byte[BufferSize];
                                    int n;
                                    while ((n = input.Read(buffer, 0, buffer.Length)) != 0)
                                    {
                                        output.Write(buffer, 0, n);
                                    }
                                }
                            });
                    }


                    if (!seekable)
                    {
                        // conditionally use a non-seekable output stream
                        using (Stream raw = File.Open(zipFileToCreate, FileMode.Create, FileAccess.ReadWrite))
                        {
                            using (var ns = new Ionic.Zip.Tests.NonSeekableOutputStream(raw))
                            {
                                zip.Save(ns);
                            }
                        }
                    }
                    else
                        zip.Save(zipFileToCreate);
                }

                WinzipVerify(zipFileToCreate, password);

                Assert.AreEqual<int>(files.Length, TestUtilities.CountEntries(zipFileToCreate),
                                     "Trial ({0},{1}): The zip file created has the wrong number of entries.", cycle, k);
            }
        }



        [TestMethod]
        [ExpectedException(typeof(ZipException))]
        public void Create_ZipOutputStream_ZeroBytes_NonSeekable()
        {
            Directory.SetCurrentDirectory(TopLevelDir);

            string dirToZip = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
            Directory.CreateDirectory(dirToZip);
            int fileCount = 3;
            string[] files = new string[fileCount];
            for (int i = 0; i < fileCount; i++)
                files[i] = TestUtilities.CreateUniqueFile("zerolength", dirToZip);

            _Internal_Create_ZipOutputStream(files, EncryptionAlgorithm.PkzipWeak, false, 99,
                                             "ZipOutputStream.ZeroBytes.Nonseekable.PkzipWeak.{0}.zip");
        }



        [TestMethod]
        [ExpectedException(typeof(System.InvalidOperationException))]
        public void Create_ZipOutputStream_WriteBeforePutNextEntry()
        {
            string zipFileToCreate = "Create_ZipOutputStream_WriteBeforePutNextEntry.zip";
            using (FileStream fs = File.Open(zipFileToCreate, FileMode.Create, FileAccess.ReadWrite))
            {
                using (var output = new ZipOutputStream(fs))
                {
                    //output.PutNextEntry("entry1.txt");
                    byte[] buffer = Encoding.ASCII.GetBytes("This is the content for entry #1.");
                    output.Write(buffer, 0, buffer.Length);
                }
            }
        }


        
                
        [TestMethod]
        public void Create_ZipOutputStream_EmptyEntries()
        {
            Directory.SetCurrentDirectory(TopLevelDir);
            for (int i = 0; i < crypto.Length; i++)
            {
                for (int j = 0; j < compLevels.Length; j++)
                {
                    string password = Path.GetRandomFileName();

                    for (int k = 0; k < 2; k++)
                    {

                        string zipFileToCreate = String.Format("Create_ZipOutputStream_EmptyEntries.Encryption.{0}.{1}.{2}.zip",
                                                              crypto[i].ToString(), compLevels[j].ToString(), k);

                        using (FileStream fs = File.Open(zipFileToCreate, FileMode.Create, FileAccess.ReadWrite))
                        {
                            using (var output = new ZipOutputStream(fs))
                            {
                                byte[] buffer;
                                output.Password = password;
                                output.Encryption = crypto[i];
                                output.CompressionLevel = compLevels[j];
                                output.PutNextEntry("entry1.txt");
                                if (k == 0)
                                {
                                    buffer = Encoding.ASCII.GetBytes("This is the content for entry #1.");
                                    output.Write(buffer, 0, buffer.Length);
                                }

                                output.PutNextEntry("entry2.txt");  // this will be zero length
                                output.PutNextEntry("entry3.txt");
                                if (k == 0)
                                {
                                    buffer = Encoding.ASCII.GetBytes("This is the content for entry #3.");
                                    output.Write(buffer, 0, buffer.Length);
                                }
                                output.PutNextEntry("entry4.txt");  // this will be zero length
                                output.PutNextEntry("entry5.txt");  // this will be zero length
                            }
                        }

                        WinzipVerify(zipFileToCreate, password);

                        Assert.AreEqual<int>(5, TestUtilities.CountEntries(zipFileToCreate),
                                             "Trial ({0},{1}): The zip file created has the wrong number of entries.", i, j);
                    }
                }

            }
        }




        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Create_ZipOutputStream_DuplicateEntry()
        {
            string zipFileToCreate = "Create_ZipOutputStream_DuplicateEntry.zip";

            string entryName = Path.GetRandomFileName();

            using (FileStream fs = File.Open(zipFileToCreate, FileMode.Create, FileAccess.ReadWrite))
            {
                using (var output = new ZipOutputStream(fs))
                {
                    output.PutNextEntry(entryName);
                    output.PutNextEntry(entryName);
                }
            }
        }



        [TestMethod]
        public void Create_ZipOutputStream()
        {
            _TestDriver(new TestCompressionLevels(_Internal_Create_ZipOutputStream), "ZipOutputStream", true, false);
        }

        [TestMethod]
        public void Create_ZipOutputStream_NonSeekable()
        {
            _TestDriver(new TestCompressionLevels(_Internal_Create_ZipOutputStream), "ZipOutputStream", false, false);
        }



        [TestMethod]
        public void Create_ZipOutputStream_ZeroLength_wi8933()
        {
            _TestDriver(new TestCompressionLevels(_Internal_Create_ZipOutputStream), "ZipOutputStream", true, true);
        }



        private void _Internal_Create_ZipOutputStream(string[] files,
                                                      EncryptionAlgorithm crypto,
                                                      bool seekable,
                                                      int cycle,
                                                      string format)
        {
            int BufferSize = 2048;

            for (int k = 0; k < compLevels.Length; k++)
            {
                string zipFileToCreate = Path.Combine(TopLevelDir, String.Format(format, k));
                string password = Path.GetRandomFileName();

                Stream raw = File.Open(zipFileToCreate, FileMode.Create, FileAccess.ReadWrite);

                // conditionally use a non-seekable output stream
                if (!seekable)
                    raw = new Ionic.Zip.Tests.NonSeekableOutputStream(raw);

                using (raw)
                {
                    TestContext.WriteLine("=================================");
                    TestContext.WriteLine("Creating {0}...", Path.GetFileName(zipFileToCreate));
                    TestContext.WriteLine("Encryption({0})  Compression({1})  pw({2})",
                                          crypto.ToString(), compLevels[k].ToString(), password);

                    using (var output = new ZipOutputStream(raw))
                    {
                        if (crypto != EncryptionAlgorithm.None)
                        {
                            output.Password = password;
                            output.Encryption = crypto;
                        }
                        output.CompressionLevel = compLevels[k];

                        foreach (var file in files)
                        {
                            TestContext.WriteLine("file: {0}", file);

                            output.PutNextEntry(file);
                            using (var input = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Write))
                            {
                                byte[] buffer = new byte[BufferSize];
                                int n;
                                while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    output.Write(buffer, 0, n);
                                }
                            }
                        }

                    }
                }

                WinzipVerify(zipFileToCreate, password);

                Assert.AreEqual<int>(files.Length, TestUtilities.CountEntries(zipFileToCreate),
                                     "Trial ({0},{1}): The zip file created has the wrong number of entries.", cycle, k);
            }

        }


    }
}
