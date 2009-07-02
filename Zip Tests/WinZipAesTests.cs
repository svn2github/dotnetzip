// WinZipAesTests.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa and Microsoft Corporation.  
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
// Time-stamp: <2009-July-01 14:52:50>
//
// ------------------------------------------------------------------
//
// This module defines the tests of the WinZIP AES Encryption capability
// of DotNetZip.
//
// ------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Zip;
using Ionic.Zip.Tests.Utilities;

namespace Ionic.Zip.Tests.WinZipAes
{
    /// <summary>
    /// Summary description for WinZipAesTests
    /// </summary>
    [TestClass]
    public class WinZipAesTests
    {
        private System.Random _rnd;

        public WinZipAesTests()
        {
            _rnd = new System.Random();
        }

        #region Context
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #endregion

        #region Test Init and Cleanup
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //


        private string CurrentDir = null;
        private string TopLevelDir = null;

        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            TestUtilities.Initialize(ref CurrentDir, ref TopLevelDir);
            _FilesToRemove.Add(TopLevelDir);
        }


        System.Collections.Generic.List<string> _FilesToRemove = new System.Collections.Generic.List<string>();

        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            TestUtilities.Cleanup(CurrentDir, _FilesToRemove);
        }

        #endregion


        [TestMethod]
        public void WinZipAes_CreateZip()
        {
            _Internal_CreateZip_WinZipAes("WinZipAes_CreateZip", 14400, 5000);
        }


        private void _Internal_CreateZip_WinZipAes(string name, int size1, int size2)
        {
            EncryptionAlgorithm[] EncOptions = { //EncryptionAlgorithm.None, 
                                                   EncryptionAlgorithm.WinZipAes256,
                                                   EncryptionAlgorithm.WinZipAes128, 
                                                   EncryptionAlgorithm.PkzipWeak
                                               };

            for (int k = 0; k < EncOptions.Length; k++)
            {
                string filename = null;
                string password = TestUtilities.GenerateRandomPassword();
                Directory.SetCurrentDirectory(TopLevelDir);
                TestContext.WriteLine("\n\n==================Trial {0}...", k);
                string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("{0}-{1}.zip", name, k));

                TestContext.WriteLine("Creating file {0}", zipFileToCreate);
                TestContext.WriteLine("  Password:   {0}", password);
                TestContext.WriteLine("  Encryption: {0}", EncOptions[k].ToString());
                int entries = _rnd.Next(11) + 8;
                //int entries = 2;

                var checksums = new Dictionary<string, string>();
                using (ZipFile zip1 = new ZipFile())
                {
                    zip1.Encryption = EncOptions[k];
                    if (zip1.Encryption != EncryptionAlgorithm.None)
                        zip1.Password = password;

                    for (int i = 0; i < entries; i++)
                    {
                        if (_rnd.Next(2) == 1)
                        {
                            filename = Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                            int filesize = _rnd.Next(size1) + size2;
                            TestUtilities.CreateAndFillFileBinary(filename, filesize);
                        }
                        else
                        {
                            filename = Path.Combine(TopLevelDir, String.Format("Data{0}.txt", i));
                            int filesize = _rnd.Next(size1) + size2;
                            TestUtilities.CreateAndFillFileText(filename, filesize);
                        }
                        zip1.AddFile(filename, "");

                        var chk = TestUtilities.ComputeChecksum(filename);
                        checksums.Add(Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                    }

                    zip1.Comment = String.Format("This archive uses Encryption: {0}, password({1})",
                        zip1.Encryption, password);
                    zip1.Save(zipFileToCreate);
                }


                TestContext.WriteLine("---------------Reading {0}...", zipFileToCreate);
                System.Threading.Thread.Sleep(1200); // seems to be a race condition?  sometimes?
                using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
                {
                    string extractDir = String.Format("extract{0}", k);
                    foreach (var e in zip2)
                    {
                        TestContext.WriteLine(" Entry: {0}  c({1})  unc({2})", e.FileName, e.CompressedSize, e.UncompressedSize);
                        Assert.AreEqual<EncryptionAlgorithm>(EncOptions[k], e.Encryption);
                        e.ExtractWithPassword(extractDir, password);
                        filename = Path.Combine(extractDir, e.FileName);
                        string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filename));
                        Assert.IsTrue(checksums.ContainsKey(e.FileName), "Checksum is missing");
                        Assert.AreEqual<string>(checksums[e.FileName], actualCheckString, "Checksums for ({0}) do not match.", e.FileName);
                        TestContext.WriteLine("     Checksums match ({0}).\n", actualCheckString);
                    }
                }
            }
        }




        [TestMethod]
        public void WinZipAes_CreateZip_NoPassword()
        {
            Directory.SetCurrentDirectory(TopLevelDir);
            string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("WinZipAes_CreateZip_NoPassword.zip"));

            TestContext.WriteLine("Creating file {0}", zipFileToCreate);
            int entries = _rnd.Next(11) + 8;

            string filename = null;
            var checksums = new Dictionary<string, string>();
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Encryption = EncryptionAlgorithm.WinZipAes256;
                for (int i = 0; i < entries; i++)
                {
                    if (_rnd.Next(2) == 1)
                    {
                        filename = Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                        int filesize = _rnd.Next(144000) + 5000;
                        TestUtilities.CreateAndFillFileBinary(filename, filesize);
                    }
                    else
                    {
                        filename = Path.Combine(TopLevelDir, String.Format("Data{0}.txt", i));
                        int filesize = _rnd.Next(144000) + 5000;
                        TestUtilities.CreateAndFillFileText(filename, filesize);
                    }
                    zip1.AddFile(filename, "");

                    var chk = TestUtilities.ComputeChecksum(filename);
                    checksums.Add(Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                }

                zip1.Comment = String.Format("This archive uses Encryption: {0}, no password!", zip1.Encryption);
                // With no password, we expect no encryption in the output.
                zip1.Save(zipFileToCreate);
            }


            // validate all the checksums
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (ZipEntry e in zip2)
                {
                    if (!e.IsDirectory)
                    {
                        e.Extract("unpack");
                        string PathToExtractedFile = Path.Combine("unpack", e.FileName);

                        Assert.IsTrue(checksums.ContainsKey(e.FileName));

                        // verify the checksum of the file is correct
                        string expectedCheckString = checksums[e.FileName];
                        string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(PathToExtractedFile));
                        Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", PathToExtractedFile);
                    }
                }
            }

        }



        [TestMethod]
        public void WinZipAes_CreateZip_DirectoriesOnly()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "WinZipAes_CreateZip_DirectoriesOnly.zip");
            Assert.IsFalse(File.Exists(zipFileToCreate), "The temporary zip file '{0}' already exists.", zipFileToCreate);

            string password = TestUtilities.GenerateRandomPassword();
            string DirToZip = Path.Combine(TopLevelDir, "zipthis");
            Directory.CreateDirectory(DirToZip);

            int entries = 0;
            int subdirCount = _rnd.Next(8) + 8;

            TestContext.WriteLine("Creating file   {0}", zipFileToCreate);
            TestContext.WriteLine("  Password:     {0}", password);
            TestContext.WriteLine("  #directories: {0}", subdirCount);

            for (int i = 0; i < subdirCount; i++)
            {
                string SubDir = Path.Combine(DirToZip, "EmptyDir" + i);
                Directory.CreateDirectory(SubDir);
            }

            Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Encryption = EncryptionAlgorithm.WinZipAes256;
                zip1.Password = password;
                zip1.AddDirectory(Path.GetFileName(DirToZip));
                zip1.Save(zipFileToCreate);
            }

            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), entries,
                    "The zip file created has the wrong number of entries.");
        }



        [TestMethod]
        public void WinZipAes_CreateZip_ZeroLengthFiles()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "WinZipAes_CreateZip_ZeroLengthFiles.zip");
            Assert.IsFalse(File.Exists(zipFileToCreate), "The temporary zip file '{0}' already exists.", zipFileToCreate);

            string password = TestUtilities.GenerateRandomPassword();

            TestContext.WriteLine("Creating file {0}", zipFileToCreate);
            TestContext.WriteLine("  Password:   {0}", password);

            int entries = _rnd.Next(21) + 5;
            int i;
            string[] FilesToZip = new string[entries];
            for (i = 0; i < entries; i++)
                FilesToZip[i] = TestUtilities.CreateUniqueFile("zerolength", TopLevelDir);

            using (ZipFile zip = new ZipFile())
            {
                zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                zip.Password = password;

                for (i = 0; i < FilesToZip.Length; i++)
                {
                    string pathToUse = Path.Combine(Path.GetFileName(TopLevelDir),
                        Path.GetFileName(FilesToZip[i]));
                    zip.AddFile(pathToUse);
                }
                zip.Save(zipFileToCreate);
            }

            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), FilesToZip.Length,
                    "The zip file created has the wrong number of entries.");
        }



        [TestMethod]
        public void WinZipAes_CreateZip_VerySmallFiles()
        {
            _Internal_CreateZip_WinZipAes("WinZipAes_CreateZip_VerySmallFilesv", 14, 5);
        }


        [TestMethod]
        public void WinZipAes_ReadZips()
        {
            _Internal_ReadZip_WinZipAes("winzip-AES256-multifiles-pw-BarbieDoll.zip", "BarbieDoll", 10);
            _Internal_ReadZip_WinZipAes("winzip-aes128-pw-ThunderScalpXXX$.zip", "ThunderScalpXXX$", 34);
        }

        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.BadPasswordException))]
        public void WinZipAes_ReadZip_Fail_BadPassword()
        {
            _Internal_ReadZip_WinZipAes("winzip-AES256-multifiles-pw-BarbieDoll.zip", "WrongPassword!!##", 99);
        }

        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.BadPasswordException))]
        public void WinZipAes_ReadZip_Fail_NoPassword()
        {
            _Internal_ReadZip_WinZipAes("winzip-AES256-multifiles-pw-BarbieDoll.zip", null, 99);
        }

        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.BadPasswordException))]
        public void WinZipAes_ReadZip_Fail_WrongMethod()
        {
            _Internal_ReadZip_WinZipAes("winzip-AES256-multifiles-pw-BarbieDoll.zip", "-null-", 99);
        }


        int zipCount = 0;
        public void _Internal_ReadZip_WinZipAes(string zipfile, string password, int expectedFilesExtracted)
        {
            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            // This is an AES-encrypted zip produced by WinZip
            string ZipFileToRead = Path.Combine(SourceDir, 
                String.Format("Zip Tests\\bin\\Debug\\zips\\{0}", zipfile));

            Directory.SetCurrentDirectory(TopLevelDir);

            Assert.IsTrue(File.Exists(ZipFileToRead), "The zip file '{0}' does not exist.", ZipFileToRead);

            // extract all the files 
            int actualFilesExtracted=0;
            string extractDir = String.Format("Extract{0}", zipCount++);

            using (ZipFile zip2 = ZipFile.Read(ZipFileToRead))
            {
                //zip2.Password = password;
                foreach (ZipEntry e in zip2)
                {
                    if (!e.IsDirectory)
                    {
                        if (password == "-null-")
e.Extract(extractDir);
                        else
                        e.ExtractWithPassword(extractDir, password);
                        actualFilesExtracted++;
                    }
                }
            }
            Assert.AreEqual<int>(expectedFilesExtracted, actualFilesExtracted);
        }


        [TestMethod]
        public void WinZipAes_CreateZip_NoCompression()
        {
            Directory.SetCurrentDirectory(TopLevelDir);
            string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("WinZipAes_CreateZip_NoCompression.zip"));
            string password = TestUtilities.GenerateRandomPassword();

            TestContext.WriteLine("=======================================");
            TestContext.WriteLine("Creating file {0}", zipFileToCreate);
            TestContext.WriteLine("  Password:   {0}", password);
            int entries = _rnd.Next(21) + 5;

            string filename = null;
            var checksums = new Dictionary<string, string>();
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Encryption = EncryptionAlgorithm.WinZipAes256;
                zip1.Password = password;
                zip1.ForceNoCompression = true;

                for (int i = 0; i < entries; i++)
                {
                    if (_rnd.Next(2) == 1)
                    {
                        filename = Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                        int filesize = _rnd.Next(144000) + 5000;
                        TestUtilities.CreateAndFillFileBinary(filename, filesize);
                    }
                    else
                    {
                        filename = Path.Combine(TopLevelDir, String.Format("Data{0}.txt", i));
                        int filesize = _rnd.Next(144000) + 5000;
                        TestUtilities.CreateAndFillFileText(filename, filesize);
                    }
                    zip1.AddFile(filename, "");

                    var chk = TestUtilities.ComputeChecksum(filename);
                    checksums.Add(Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                }

                zip1.Comment = String.Format("This archive uses Encryption({0}) password({1}) no compression.", zip1.Encryption, password);
                zip1.Save(zipFileToCreate);
            }



            // validate all the checksums
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (ZipEntry e in zip2)
                {
                    if (!e.IsDirectory)
                    {
                        Assert.AreEqual<short>(0, e.CompressionMethod);

                        e.ExtractWithPassword("unpack", password);

                        string PathToExtractedFile = Path.Combine("unpack", e.FileName);

                        Assert.IsTrue(checksums.ContainsKey(e.FileName));

                        // verify the checksum of the file is correct
                        string expectedCheckString = checksums[e.FileName];
                        string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(PathToExtractedFile));
                        Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", PathToExtractedFile);
                    }
                }
            }

        }


        [TestMethod]
        public void WinZipAes_CreateZip_EmptyPassword()
        {
            Directory.SetCurrentDirectory(TopLevelDir);
            string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("WinZipAes_CreateZip_EmptyPassword.zip"));
            string password = "";

            TestContext.WriteLine("=======================================");
            TestContext.WriteLine("Creating file {0}", zipFileToCreate);
            TestContext.WriteLine("  Password:   '{0}'", password);
            int entries = _rnd.Next(21) + 5;

            string filename = null;
            var checksums = new Dictionary<string, string>();
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Encryption = EncryptionAlgorithm.WinZipAes256;
                zip1.Password = password;

                for (int i = 0; i < entries; i++)
                {
                    if (_rnd.Next(2) == 1)
                    {
                        filename = Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                        int filesize = _rnd.Next(144000) + 5000;
                        TestUtilities.CreateAndFillFileBinary(filename, filesize);
                    }
                    else
                    {
                        filename = Path.Combine(TopLevelDir, String.Format("Data{0}.txt", i));
                        int filesize = _rnd.Next(144000) + 5000;
                        TestUtilities.CreateAndFillFileText(filename, filesize);
                    }
                    zip1.AddFile(filename, "");

                    var chk = TestUtilities.ComputeChecksum(filename);
                    checksums.Add(Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                }

                zip1.Comment = String.Format("This archive uses Encryption({0}) password({1}) no compression.", zip1.Encryption, password);
                zip1.Save(zipFileToCreate);
            }


            // validate all the checksums
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (ZipEntry e in zip2)
                {
                    if (!e.IsDirectory)
                    {
                        e.ExtractWithPassword("unpack", password);

                        string PathToExtractedFile = Path.Combine("unpack", e.FileName);

                        Assert.IsTrue(checksums.ContainsKey(e.FileName));

                        // verify the checksum of the file is correct
                        string expectedCheckString = checksums[e.FileName];
                        string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(PathToExtractedFile));
                        Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", PathToExtractedFile);
                    }
                }
            }
        }


        [TestMethod]
        public void RemoveEntryAndSave()
        {
            // make a few text files
            string[] TextFiles = new string[3];
            for (int i = 0; i < TextFiles.Length; i++)
            {
                TextFiles[i] = Path.Combine(TopLevelDir, String.Format("TextFile{0}.txt", i));
                TestUtilities.CreateAndFillFileText(TextFiles[i], _rnd.Next(4000) + 5000);
            }
            TestContext.WriteLine(new String('=', 66));
            TestContext.WriteLine("RemoveEntryAndSave()");
            for (int k = 0; k < 2; k++)
            {
                TestContext.WriteLine(new String('-', 55));
                TestContext.WriteLine("Trial {0}", k);
                string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("RemoveEntryAndSave-{0}.zip", k));

                // create the zip: add some files, and Save() it
                using (ZipFile zip = new ZipFile())
                {
                    if (k == 1)
                    {
                        TestContext.WriteLine("Specifying a password...");
                        zip.Password = "password";
                        zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                    }
                    for (int i = 0; i < TextFiles.Length; i++)
                        zip.AddFile(TextFiles[i], "");

                    zip.AddEntry("Readme.txt", "", "This is the content of the file. Ho ho ho!");
                    TestContext.WriteLine("Save...");
                    zip.Save(zipFileToCreate);
                }

                // remove a file and re-Save
                using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
                {
                    int entryToRemove = _rnd.Next(TextFiles.Length);
                    TestContext.WriteLine("Removing an entry...: {0}", Path.GetFileName(TextFiles[entryToRemove]));
                    zip2.RemoveEntry(Path.GetFileName(TextFiles[entryToRemove]));
                    zip2.Save();
                }

                // Verify the files are in the zip
                Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), TextFiles.Length,
                 String.Format("Trial {0}: The Zip file has the wrong number of entries.", k));
            }
        }


        [TestMethod]
        public void WinZipAes_SmallBuffers_wi7967()
        {
            Directory.SetCurrentDirectory(TopLevelDir);
            string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("WinZipAes_SmallBuffers_wi7967.zip"));
            string password = Path.GetRandomFileName() + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

            int[] sizes = { 0,1,2,3,4,5,6,7,8,9,10,13,21,35,93 };
            for (int i=0; i < sizes.Length;  i++) 
            {
                MemoryStream zippedStream = new MemoryStream();
                byte[] buffer = new byte[sizes[i]];
                _rnd.NextBytes(buffer);
                MemoryStream source = new MemoryStream(buffer);

                using (var zip = new ZipFile())
                {
                    source.Seek(0, SeekOrigin.Begin);
                    zip.Password = password;
                    zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                    zip.AddEntry(Path.GetRandomFileName(), "", source);
                    zip.Save(zippedStream);
                }
            }
        }
        
        

        [TestMethod]
        [ExpectedException(typeof(System.InvalidOperationException))]        
        public void WinZipAes_Update_SwitchCompression()
        {
            Directory.SetCurrentDirectory(TopLevelDir);
            string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("WinZipAes_Update_SwitchCompression.zip"));
            string password = TestUtilities.GenerateRandomPassword();

            TestContext.WriteLine("=======================================");
            TestContext.WriteLine("Creating file {0}", zipFileToCreate);
            TestContext.WriteLine("  Password:   {0}", password);
            int entries = _rnd.Next(21) + 5;

            string filename = null;
            var checksums = new Dictionary<string, string>();
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Encryption = EncryptionAlgorithm.WinZipAes256;
                zip1.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                zip1.Password = password;

                for (int i = 0; i < entries; i++)
                {
                    if (_rnd.Next(2) == 1)
                    {
                        filename = Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                        int filesize = _rnd.Next(144000) + 5000;
                        TestUtilities.CreateAndFillFileBinary(filename, filesize);
                    }
                    else
                    {
                        filename = Path.Combine(TopLevelDir, String.Format("Data{0}.txt", i));
                        int filesize = _rnd.Next(144000) + 5000;
                        TestUtilities.CreateAndFillFileText(filename, filesize);
                    }
                    zip1.AddFile(filename, "");

                    var chk = TestUtilities.ComputeChecksum(filename);
                    checksums.Add(Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                }

                zip1.Comment = String.Format("This archive uses Encryption({0}) password({1}) no compression.", zip1.Encryption, password);
                TestContext.WriteLine("{0}", zip1.Comment);
                TestContext.WriteLine("Saving the zip...");
                zip1.Save(zipFileToCreate);
            }

            TestContext.WriteLine("=======================================");
            TestContext.WriteLine("Updating the zip file");
            // Update the zip file
            using (ZipFile zip = ZipFile.Read(zipFileToCreate))
            {
                for (int j = 0; j < 5; j++)
                {
                    zip[j].CompressionMethod = 0;
                }
                zip.Save();
            }

            // validate all the checksums
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (ZipEntry e in zip2)
                {
                    if (!e.IsDirectory)
                    {
                        e.ExtractWithPassword("unpack", password);

                        string PathToExtractedFile = Path.Combine("unpack", e.FileName);

                        Assert.IsTrue(checksums.ContainsKey(e.FileName));

                        // verify the checksum of the file is correct
                        string expectedCheckString = checksums[e.FileName];
                        string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(PathToExtractedFile));
                        Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", PathToExtractedFile);
                    }
                }
            }
        }


    }
}
