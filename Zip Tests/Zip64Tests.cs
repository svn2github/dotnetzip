// Zip64Tests.cs
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
// Time-stamp: <2009-June-28 18:33:53>
//
// ------------------------------------------------------------------
//
// This module defines the tests for the ZIP64 capability within DotNetZip.
//
// ------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Zip;
using Ionic.Zip.Tests.Utilities;


namespace Ionic.Zip.Tests.Zip64
{

    /// <summary>
    /// Summary description for Zip64Tests
    /// </summary>
    [TestClass]
    public class Zip64Tests : IExec
    {
        private System.Random _rnd;

        public Zip64Tests()
        {
            _rnd = new System.Random();
        }

        #region Context
        private TestContext testContextInstance;

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
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


            private static string _HugeZipFile;
        private string GetHugeZipFile()
        {
                if (_HugeZipFile == null)
                {
                    _HugeZipFile = _CreateHugeZipfile();
                }
                return _HugeZipFile;
        }

        
        //         [ClassInitialize()]
        //             public static void MyClassInitialize(TestContext testContext)
        //         {
        //         }

        [ClassCleanup()]
            public static void MyClassCleanup()
        {
            if (_HugeZipFile != null)
            {
                if (File.Exists(_HugeZipFile))
                {
                    File.Delete(_HugeZipFile);
                    Directory.Delete(Path.GetDirectoryName(_HugeZipFile), true);
                }
            }
        }


        private Object LOCK = new Object();
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
            if (_txrx!=null)
            {
                try
                {
                    _txrx.Send("stop");
                    _txrx = null;
                }
                catch { }
            }
        }



        private string _CreateHugeZipfile()
        {
            lock (LOCK)
            {
                string HugeZipFile = TestUtilities.GenerateUniquePathname("tmp.zip");
                string TempDir = TestUtilities.GenerateUniquePathname("Zip64Tests.tmp");

                // remember this directory so we can restore later
                string originalDir = Directory.GetCurrentDirectory();

                Directory.SetCurrentDirectory(CurrentDir);
                Directory.CreateDirectory(TempDir);
                Directory.SetCurrentDirectory(Path.GetDirectoryName(TempDir));

                // create a huge ZIP64 archive with a true 64-bit offset.
                string testBin = TestUtilities.GetTestBinDir(CurrentDir);
                string progressMonitorTool = Path.Combine(testBin, "Resources\\UnitTestProgressMonitor.exe");
                string requiredDll = Path.Combine(testBin, "Resources\\Ionic.CopyData.dll");

                Assert.IsTrue(File.Exists(progressMonitorTool), "progress monitor tool does not exist ({0})", progressMonitorTool);
                Assert.IsTrue(File.Exists(requiredDll), "required DLL does not exist ({0})", requiredDll);

                string progressChannel = "Zip64_Setup";
                // start the progress monitor
                string ignored;
                TestUtilities.Exec_NoContext(progressMonitorTool, String.Format("-channel {0}", progressChannel), false, out ignored);

                // System.Reflection.Assembly.Load(requiredDll);

                System.Threading.Thread.Sleep(1000);
                _txrx = new Ionic.CopyData.Transceiver();
                _txrx.Channel = progressChannel;
                _txrx.Send("test Zip64 Test Setup");
                _txrx.Send("bars 3");
                System.Threading.Thread.Sleep(120);
                _txrx.Send("status Creating files");
                _txrx.Send(String.Format("pb 0 max {0}", 3));

                string ZipFileToCreate = Path.Combine(TempDir, "Zip64Data.zip");

                Directory.SetCurrentDirectory(TempDir);

                // create a directory with some files in it, to zip
                string dirToZip = "dir";
                Directory.CreateDirectory(dirToZip);

                // create a few files in that directory
                int numFilesToAdd = _rnd.Next(3) + 6;

                _sizeBase = 0x20000000;
                _sizeRandom = 0x01000000;

#if SKIP_THIS
     
     // Rather than create these large files with random content and
     // then zip them, we'll add entries from streams, and provide the
     // streams just-in-time.  This means no write for the original
     // file, nor read during zip saving, which avoids a ton of I/O, and
     // is faster.
     
            _txrx.Send(String.Format("pb 1 max {0}", numFilesToAdd));
            int baseSize = _rnd.Next(0x1f0000ff) + 80000;
            bool firstFileDone = false;
            string fileName = "";
            string firstBinary = null;
            string firstText = null;
            long fileSize = 0;
            
            Action<Int64> progressUpdate = (x) =>
                {
                    _txrx.Send(String.Format("pb 2 value {0}", x));
                    _txrx.Send(String.Format("status Creating {0}, [{1}/{2}] ({3:N0}%)", fileName, x, fileSize, ((double)x)/ (0.01 * fileSize) ));
                };

            // It takes a long time to create a large file. And we need
            // a bunch of them.  
            
            for (int i = 0; i < numFilesToAdd; i++)
            {
                int n = _rnd.Next(2);
                fileName = string.Format("Pippo{0}.{1}", i, (n==0) ? "bin" : "txt" );
                    if (i != 0)
                    {
                        int x = _rnd.Next(6);
                        if (x != 0)
                        {
                            string folderName = string.Format("folder{0}", x);
                            fileName = Path.Combine(folderName, fileName);
                            if (!Directory.Exists(Path.Combine(dirToZip, folderName)))
                                Directory.CreateDirectory(Path.Combine(dirToZip, folderName));
                        }
                    }
                fileName = Path.Combine(dirToZip, fileName);
                // first file is 2x larger
                fileSize = (firstFileDone) ? (baseSize + _rnd.Next(0x880000)) : (2*baseSize);
                _txrx.Send(String.Format("pb 2 max {0}", fileSize));
                if (n==0) 
                    TestUtilities.CreateAndFillFileBinary(fileName, fileSize, progressUpdate);
                else
                    TestUtilities.CreateAndFillFileText(fileName, fileSize, progressUpdate);
                firstFileDone = true;
                _txrx.Send("pb 1 step");
            }
#endif

                _txrx.Send("pb 0 step");

                // Add links to a few very large files into the same directory.
                // We do this because creating such large files will take a very very long time.

                _txrx.Send("status Creating links");
                var namesOfLargeFiles = new String[]
                {
                    "c:\\dinoch\\PST\\archive.pst",
                    "c:\\dinoch\\PST\\archive1.pst", 
                    "c:\\dinoch\\PST\\Lists.pst",
                    "c:\\dinoch\\PST\\OldStuff.pst",
                    "c:\\dinoch\\PST\\Personal1.pst",
                };

                string subdir = Path.Combine(dirToZip, "largelinks");
                Directory.CreateDirectory(subdir);
                Directory.SetCurrentDirectory(subdir);
                var w = System.Environment.GetEnvironmentVariable("Windir");
                Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);
                var fsutil = Path.Combine(Path.Combine(w, "system32"), "fsutil.exe");
                Assert.IsTrue(File.Exists(fsutil), "fsutil.exe does not exist ({0})", fsutil);
                foreach (var f in namesOfLargeFiles)
                {
                    Assert.IsTrue(File.Exists(f));
                    string cmd = String.Format("hardlink create {0} {1}", Path.GetFileName(f), f);
                    _txrx.Send("status " + cmd);
                    TestUtilities.Exec_NoContext(fsutil, cmd, out ignored);
                    cmd = String.Format("hardlink create {0}-Copy1{1} {2}",
                                        Path.GetFileNameWithoutExtension(f), Path.GetExtension(f), f);
                    TestUtilities.Exec_NoContext(fsutil, cmd, out ignored);
                }

                Directory.SetCurrentDirectory(TempDir); // again

                // pb1 and pb2 will be set in the SaveProgress handler
                _txrx.Send("pb 0 step");
                _txrx.Send("status Saving the zip...");
                _pb1Set = false;
                _testTitle = "Zip64 Test Setup";
                using (ZipFile zip = new ZipFile())
                {
                    zip.SaveProgress += zip64_SaveProgress;
                    zip.UpdateDirectory(dirToZip, "");
                    zip.UseZip64WhenSaving = Zip64Option.Always;
                    // use large buffer to speed up save for large files:
                    zip.BufferSize = 1024 * 756;
                    zip.CodecBufferSize = 1024 * 128;
                    for (int i = 0; i < numFilesToAdd; i++)
                        zip.AddEntry("random" + i + ".txt", "", Stream.Null);

                    zip.Save(ZipFileToCreate);
                }

                _txrx.Send("pb 0 step");
                System.Threading.Thread.Sleep(120);

                Directory.Delete(dirToZip, true);
                _txrx.Send("pb 0 step");
                System.Threading.Thread.Sleep(120);

                _txrx.Send("stop");

                // restore the cwd:
                Directory.SetCurrentDirectory(originalDir);

                return ZipFileToCreate;
            }
        }
        
        #endregion


        


            [TestMethod]
            public void Zip64_Create()
        {
            Zip64Option[] Options = { Zip64Option.Always, Zip64Option.Never, Zip64Option.AsNecessary };
            for (int k = 0; k < Options.Length; k++)
            {
                string filename = null;
                Directory.SetCurrentDirectory(TopLevelDir);
                TestContext.WriteLine("\n\n==================Trial {0}...", k);
                string ZipFileToCreate = Path.Combine(TopLevelDir, String.Format("Zip64_Create-{0}.zip", k));

                TestContext.WriteLine("Creating file {0}", ZipFileToCreate);
                TestContext.WriteLine("  ZIP64 option: {0}", Options[k].ToString());
                int entries = _rnd.Next(5) + 13;
                //int entries = 3;


                var checksums = new Dictionary<string, string>();
                using (ZipFile zip1 = new ZipFile())
                {
                    for (int i = 0; i < entries; i++)
                    {
                        if (_rnd.Next(2) == 1)
                        {
                            filename = Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                            int filesize = _rnd.Next(44000) + 5000;
                            //int filesize = 2000;
                            TestUtilities.CreateAndFillFileBinary(filename, filesize);
                        }
                        else
                        {
                            filename = Path.Combine(TopLevelDir, String.Format("Data{0}.txt", i));
                            int filesize = _rnd.Next(44000) + 5000;
                            //int filesize = 1000;
                            TestUtilities.CreateAndFillFileText(filename, filesize);
                        }
                        zip1.AddFile(filename, "");

                        var chk = TestUtilities.ComputeChecksum(filename);
                        checksums.Add(Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                    }

                    zip1.UseZip64WhenSaving = Options[k];
                    zip1.Comment = String.Format("This archive uses zip64 option: {0}", Options[k].ToString());
                    zip1.Save(ZipFileToCreate);

                    if (Options[k] == Zip64Option.Always)
                        Assert.IsTrue(zip1.OutputUsedZip64.Value);
                    else if (Options[k] == Zip64Option.Never)
                        Assert.IsFalse(zip1.OutputUsedZip64.Value);

                }

                TestContext.WriteLine("---------------Reading {0}...", ZipFileToCreate);
                using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
                {
                    string extractDir = String.Format("extract{0}", k);
                    foreach (var e in zip2)
                    {
                        TestContext.WriteLine(" Entry: {0}  c({1})  unc({2})", e.FileName, e.CompressedSize, e.UncompressedSize);

                        e.Extract(extractDir);
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
            public void Zip64_Convert()
        {
            string trialDescription = "Trial {0}/{1}:  create archive as 'zip64={2}', then open it and re-save with 'zip64={3}'";
            Zip64Option[] z64a = { 
                Zip64Option.Never,
                Zip64Option.Always,
                Zip64Option.AsNecessary};

            for (int u = 0; u < 2; u++)
            {

                for (int m = 0; m < z64a.Length; m++)
                {
                    for (int n = 0; n < z64a.Length; n++)
                    {
                        int k = m * z64a.Length + n;

                        string filename = null;
                        Directory.SetCurrentDirectory(TopLevelDir);
                        TestContext.WriteLine("\n\n==================Trial {0}...", k);

                        TestContext.WriteLine(trialDescription, k, (z64a.Length * z64a.Length) - 1, z64a[m], z64a[n]);

                        string ZipFileToCreate = Path.Combine(TopLevelDir, String.Format("Zip64_Convert-{0}.A.zip", k));

                        int entries = _rnd.Next(8) + 6;
                        //int entries = 2;
                        TestContext.WriteLine("Creating file {0}, zip64={1}, {2} entries",
                                              Path.GetFileName(ZipFileToCreate), z64a[m].ToString(), entries);

                        var checksums = new Dictionary<string, string>();
                        using (ZipFile zip1 = new ZipFile())
                        {
                            for (int i = 0; i < entries; i++)
                            {
                                if (_rnd.Next(2) == 1)
                                {
                                    filename = Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(44000) + 5000);
                                }
                                else
                                {
                                    filename = Path.Combine(TopLevelDir, String.Format("Data{0}.txt", i));
                                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(44000) + 5000);
                                }
                                zip1.AddFile(filename, "");

                                var chk = TestUtilities.ComputeChecksum(filename);
                                checksums.Add(Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                            }

                            TestContext.WriteLine("---------------Saving to {0} with Zip64={1}...",
                                                  Path.GetFileName(ZipFileToCreate), z64a[m].ToString());
                            zip1.UseZip64WhenSaving = z64a[m];
                            zip1.Comment = String.Format("This archive uses Zip64Option={0}", z64a[m].ToString());
                            zip1.Save(ZipFileToCreate);
                        }


                        Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entries,
                                             "The Zip file has the wrong number of entries.");


                        string newFile = ZipFileToCreate.Replace(".A.", ".B.");
                        using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
                        {
                            TestContext.WriteLine("---------------Extracting {0} ...",
                                                  Path.GetFileName(ZipFileToCreate));
                            string extractDir = String.Format("extract-{0}-{1}.A", k, u);
                            foreach (var e in zip2)
                            {
                                TestContext.WriteLine(" {0}  crc({1:X8})  c({2:X8}) unc({3:X8})", e.FileName, e.Crc32, e.CompressedSize, e.UncompressedSize);

                                e.Extract(extractDir);
                                filename = Path.Combine(extractDir, e.FileName);
                                string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filename));
                                Assert.IsTrue(checksums.ContainsKey(e.FileName), "Checksum is missing");
                                Assert.AreEqual<string>(checksums[e.FileName], actualCheckString, "Checksums for ({0}) do not match.", e.FileName);
                            }

                            if (u==1)
                            {
                                TestContext.WriteLine("---------------Updating:  Renaming an entry...");
                                zip2[4].FileName += ".renamed";

                                string entriesToRemove = (_rnd.Next(2) == 0) ? "*.txt" : "*.bin";
                                TestContext.WriteLine("---------------Updating:  Removing {0} entries...", entriesToRemove);
                                zip2.RemoveSelectedEntries(entriesToRemove);
                            }

                            TestContext.WriteLine("---------------Saving to {0} with Zip64={1}...",
                                                  Path.GetFileName(newFile), z64a[n].ToString());

                            zip2.UseZip64WhenSaving = z64a[n];
                            zip2.Comment = String.Format("This archive uses Zip64Option={0}", z64a[n].ToString());
                            zip2.Save(newFile);
                        }



                        using (ZipFile zip3 = ZipFile.Read(newFile))
                        {
                            TestContext.WriteLine("---------------Extracting {0} ...",
                                                  Path.GetFileName(newFile));
                            string extractDir = String.Format("extract-{0}-{1}.B", k, u);
                            foreach (var e in zip3)
                            {
                                TestContext.WriteLine(" {0}  crc({1:X8})  c({2:X8}) unc({3:X8})", e.FileName, e.Crc32, e.CompressedSize, e.UncompressedSize);

                                e.Extract(extractDir);
                                filename = Path.Combine(extractDir, e.FileName);
                                string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filename));
                                if (!e.FileName.EndsWith(".renamed"))
                                {
                                    Assert.IsTrue(checksums.ContainsKey(e.FileName), "Checksum is missing");
                                    Assert.AreEqual<string>(checksums[e.FileName], actualCheckString, "Checksums for ({0}) do not match.", e.FileName);
                                }
                            }
                        }
                    }
                }
            }
        }


        Ionic.CopyData.Transceiver _txrx;
        bool _pb2Set;
        bool _pb1Set;

        
#if NOTUSED
        void zip1_SaveProgress(object sender, SaveProgressEventArgs e)
        {
            string msg; 
            switch (e.EventType)
            {
                case ZipProgressEventType.Saving_Started:
                    _txrx.Send("status saving started...");
                    _pb1Set = false;
                    break;

                case ZipProgressEventType.Saving_BeforeWriteEntry:
                    _txrx.Send(String.Format("status Compressing {0}", e.CurrentEntry.FileName));
                    if (!_pb1Set)
                    {
                        _txrx.Send(String.Format("pb 1 max {0}", e.EntriesTotal));
                        _pb1Set = true;
                    }
                    _pb2Set = false;
                    break;
                    
                case ZipProgressEventType.Saving_EntryBytesRead:
                    if (!_pb2Set)
                    {
                        _txrx.Send(String.Format("pb 2 max {0}", e.TotalBytesToTransfer));
                        _pb2Set = true;
                    }
                    _txrx.Send(String.Format("status Saving {0} :: [{2}/{3}] ({1:N0}%)",
                                             e.CurrentEntry.FileName,
                                             ((double)e.BytesTransferred) / (0.01 * e.TotalBytesToTransfer),
                                             e.BytesTransferred, e.TotalBytesToTransfer));
                    msg = String.Format("pb 2 value {0}", e.BytesTransferred);
                    _txrx.Send(msg);
                    break;
                    
                case ZipProgressEventType.Saving_AfterWriteEntry:
                    _txrx.Send("pb 1 step");
                    break;
                    
                case ZipProgressEventType.Saving_Completed:
                    _txrx.Send("status Save completed");
                    _pb1Set = false;
                    _pb2Set = false;
                    _txrx.Send("pb 1 max 1");
                    _txrx.Send("pb 1 value 1");
                    break;
            }
        }
#endif



        private string _testTitle;
        private int _sizeBase;
        private int _sizeRandom;
        
        private void zip64_SaveProgress(object sender, SaveProgressEventArgs e)
        {
            string msg; 
            switch (e.EventType)
            {
                case ZipProgressEventType.Saving_Started:
                    _txrx.Send("status saving started...");
                    _pb1Set = false;
                    break;

                case ZipProgressEventType.Saving_BeforeWriteEntry:
                    _txrx.Send(String.Format("status Compressing {0}", e.CurrentEntry.FileName));
                    if (!_pb1Set)
                    {
                        _txrx.Send(String.Format("pb 1 max {0}", e.EntriesTotal));
                        _pb1Set = true;
                    }
                    _pb2Set = false;

                    if (e.CurrentEntry.Source == ZipEntrySource.Stream &&
                        e.CurrentEntry.InputStream == Stream.Null)
                    {
                        Stream s = new Ionic.Zip.Tests.Utilities.RandomTextInputStream(_sizeBase + _rnd.Next(_sizeRandom));
                        e.CurrentEntry.InputStream = s;
                    }
                    
                    break;
                    
                case ZipProgressEventType.Saving_EntryBytesRead:
                    if (!_pb2Set)
                    {
                        _txrx.Send(String.Format("pb 2 max {0}", e.TotalBytesToTransfer));
                        _pb2Set = true;
                    }
                    _txrx.Send(String.Format("status Saving {0} :: [{2}/{3}] ({1:N0}%)",
                                             e.CurrentEntry.FileName,
                                             ((double)e.BytesTransferred) / (0.01 * e.TotalBytesToTransfer),
                                             e.BytesTransferred, e.TotalBytesToTransfer));
                    msg = String.Format("pb 2 value {0}", e.BytesTransferred);
                    _txrx.Send(msg);
                    break;
                    
            case ZipProgressEventType.Saving_AfterWriteEntry:
                _txrx.Send("test " +  _testTitle); // just in case it was missed
                _txrx.Send("pb 1 step");
                break;
                    
            case ZipProgressEventType.Saving_Completed:
                _txrx.Send("status Save completed");
                _pb1Set = false;
                _pb2Set = false;
                _txrx.Send("pb 1 max 1");
                _txrx.Send("pb 1 value 1");
                break;
            }
        }


        

        private int _numFilesToExtract;
        void zip_ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            switch (e.EventType)
            {
                case ZipProgressEventType.Extracting_BeforeExtractEntry:
                    if (!_pb1Set)
                    {
                        _txrx.Send(String.Format("pb 1 max {0}", _numFilesToExtract));
                        _pb1Set = true;
                    }
                    _pb2Set = false;
                    break;
                    
                case ZipProgressEventType.Extracting_EntryBytesWritten:
                    if (!_pb2Set)
                    {
                        _txrx.Send(String.Format("pb 2 max {0}", e.TotalBytesToTransfer));
                        _pb2Set = true;
                    }
                    _txrx.Send(String.Format("status {0} {1} :: [{3}/{4}] ({2:N0}%)",
                                             verb,
                                             e.CurrentEntry.FileName,
                                             ((double)e.BytesTransferred) / (0.01 * e.TotalBytesToTransfer),
                                             e.BytesTransferred, e.TotalBytesToTransfer));
                    string msg = String.Format("pb 2 value {0}", e.BytesTransferred);
                    _txrx.Send(msg);
                    break;

                case ZipProgressEventType.Extracting_AfterExtractEntry:
                    _txrx.Send("pb 1 step");
                    break;
            }
        }



        string verb;
        
        private void VerifyZip(string zipfile)
        {
            _pb1Set = false;
            Stream bitBucket = Stream.Null;
            TestContext.WriteLine("\nChecking file {0}", zipfile);
            verb = "Verifying";
            using (ZipFile zip = ZipFile.Read(zipfile))
            {
                zip.BufferSize = 65536*8; // 65536 * 8 = 512k - large buffer better for large files
                _numFilesToExtract = zip.Entries.Count;
                zip.ExtractProgress += zip_ExtractProgress;
                foreach (var s in zip.EntryFileNames)
                {
                    TestContext.WriteLine("  Entry: {0}", s);
                    zip[s].Extract(bitBucket);
                }
            }
            System.Threading.Thread.Sleep(0x500);
        }

        
        
        
        [Timeout(19400000), TestMethod] // in milliseconds. 7200000 = 2 hours; 13,200,000 = 3:40
            public void Zip64_Update()
        {
            _txrx = new Ionic.CopyData.Transceiver();
            try
            {
                int numUpdates = 2;

                string ZipFileToUpdate = GetHugeZipFile(); // this may take a long time
                Assert.IsTrue(File.Exists(ZipFileToUpdate), "The required ZIP file does not exist ({0})",  ZipFileToUpdate);

                string testBin = TestUtilities.GetTestBinDir(CurrentDir);
                string progressMonitorTool = Path.Combine(testBin, "Resources\\UnitTestProgressMonitor.exe");
                string requiredDll = Path.Combine(testBin, "Resources\\Ionic.CopyData.dll");
                Assert.IsTrue(File.Exists(progressMonitorTool), "progress monitor tool does not exist ({0})",  progressMonitorTool);
                Assert.IsTrue(File.Exists(requiredDll), "required DLL does not exist ({0})",  requiredDll);

                int baseSize = _rnd.Next(0x1000ff) + 80000;

                string progressChannel = "Zip64-Update";
                // start the progress monitor
                this.Exec(progressMonitorTool, String.Format("-channel {0}", progressChannel), false);

                // System.Reflection.Assembly.Load(requiredDll);

                System.Threading.Thread.Sleep(1000);
                _txrx.Channel = progressChannel;
                System.Threading.Thread.Sleep(450);
                _txrx.Send("test Zip64 Update");
                System.Threading.Thread.Sleep(120);
                _txrx.Send("status Creating files");
                _txrx.Send(String.Format("pb 0 max {0}", numUpdates + 1));


                // make sure it is larger than the 4.2gb size
                FileInfo fi = new FileInfo(ZipFileToUpdate);
                Assert.IsTrue(fi.Length > (long)System.UInt32.MaxValue, "The zip file ({0}) is not large enough.", ZipFileToUpdate);
            
                _txrx.Send("status Verifying the zip");
                VerifyZip(ZipFileToUpdate);
            
                _txrx.Send("pb 0 step");

                var sw = new StringWriter();
                for (int j=0; j < numUpdates; j++)
                {
                    _txrx.Send("test Zip64 Update");
                    // create another folder with a single file in it
                    string subdir = String.Format("newfolder-{0}", j);
                    Directory.CreateDirectory(subdir);
                    string fileName = Path.Combine(subdir, "newfile.txt");
                    long size = baseSize + _rnd.Next(28000);
                    TestUtilities.CreateAndFillFileBinary(fileName, size);

                    TestContext.WriteLine("");
                    TestContext.WriteLine("Updating the zip file...");
                    _txrx.Send("status Updating the zip file...");
                    // update the zip with that new folder+file
                    using (ZipFile zip = ZipFile.Read(ZipFileToUpdate))
                    {
                        zip.SaveProgress += zip64_SaveProgress;
                        zip.StatusMessageTextWriter = sw;
                        zip.UpdateDirectory(subdir, subdir);
                        zip.UseZip64WhenSaving = Zip64Option.Always;
                        zip.BufferSize = 65536*8; // 65536 * 8 = 512k
                        zip.Save();
                    }
                    
                    string status = sw.ToString();
                    TestContext.WriteLine(status);

                    _txrx.Send("status Verifying the zip");
                    _txrx.Send("pb 0 step");
                }

                VerifyZip(ZipFileToUpdate);
                
                _txrx.Send("pb 0 step");

                System.Threading.Thread.Sleep(120);
            }
            finally
            {
                if (_txrx!=null)
                {
                    try
                    {
                        _txrx.Send("stop");
                        _txrx = null;
                    }
                    catch { }
                }
            }
        }


        
        [TestMethod]
            public void Zip64_Winzip_Unzip_Small()
        {
            EncryptionAlgorithm[] crypto = { EncryptionAlgorithm.None, 
                                             EncryptionAlgorithm.PkzipWeak,
                                             EncryptionAlgorithm.WinZipAes128, 
                                             EncryptionAlgorithm.WinZipAes256,
            };

            Zip64Option[] z64 = { Zip64Option.Never,
                                  Zip64Option.AsNecessary, 
                                  Zip64Option.Always,
            };


            string testBin = TestUtilities.GetTestBinDir(CurrentDir);
            string fileToZip = Path.Combine(testBin, "Ionic.Zip.dll");

            Directory.SetCurrentDirectory(TopLevelDir);

            for (int n=0; n < crypto.Length; n++)
            {
                for (int m=0; m < z64.Length; m++)
                {
                    string zipFile = Path.Combine(TopLevelDir, String.Format("Zip64-Winzip-Unzip-AES-{0}-{1}.zip", n, m));
                    string password = Path.GetRandomFileName();

                    TestContext.WriteLine("=================================");
                    TestContext.WriteLine("Creating {0}...", Path.GetFileName(zipFile));
                    TestContext.WriteLine("Encryption:{0}  Zip64:{1} pw={2}", 
                        crypto[n].ToString(), z64[m].ToString(), password);
                    using (var zip = new ZipFile())
                    {
                        zip.Comment = String.Format("Encryption={0}  Zip64={1}  pw={2}", 
                            crypto[n].ToString(), z64[m].ToString(), password);
                        zip.Encryption = crypto[n];
                        zip.Password = password;
                        zip.UseZip64WhenSaving = z64[m];
                        zip.AddFile(fileToZip, "file");
                        zip.Save(zipFile);
                    }

                    TestContext.WriteLine("Unzipping with WinZip...");
            
                    string extractDir = String.Format("extract.{0}.{1}",n,m);
                    Directory.CreateDirectory(extractDir);
            
                    string progfiles = System.Environment.GetEnvironmentVariable("ProgramFiles");
                    string wzunzip = Path.Combine(progfiles, "winzip\\wzunzip.exe");
                    Assert.IsTrue(File.Exists(wzunzip), "exe ({0}) does not exist", wzunzip);

                    // this will throw if the command has a non-zero exit code.
                    this.Exec(wzunzip,
                              String.Format("-s{0} -d {1} {2}\\", password, zipFile, extractDir));
                }
            }
        }
        


        
        [Timeout(19400000), TestMethod] // in milliseconds. 7200000 = 2 hours; 194 = more than 4 hrs
            public void Zip64_Winzip_Unzip_Huge()
        {
            _txrx = new Ionic.CopyData.Transceiver();
            try
            {
                string ZipFileToExtract = GetHugeZipFile(); // may take a long time
                Assert.IsTrue(File.Exists(ZipFileToExtract), "required ZIP file does not exist ({0})",  ZipFileToExtract);

                string testBin = TestUtilities.GetTestBinDir(CurrentDir);
                string progressMonitorTool = Path.Combine(testBin, "Resources\\UnitTestProgressMonitor.exe");
                string requiredDll = Path.Combine(testBin, "Resources\\Ionic.CopyData.dll");
                Assert.IsTrue(File.Exists(progressMonitorTool), "progress monitor tool does not exist ({0})",  progressMonitorTool);
                Assert.IsTrue(File.Exists(requiredDll), "required DLL does not exist ({0})",  requiredDll);

                string extractDir = "extract";
                Directory.SetCurrentDirectory(TopLevelDir);
                Directory.CreateDirectory(extractDir);

                string progressChannel = "Zip64-WinZip-Unzip";
                // start the progress monitor
                this.Exec(progressMonitorTool, String.Format("-channel {0}", progressChannel), false);

                // System.Reflection.Assembly.Load(requiredDll);

                System.Threading.Thread.Sleep(1000);
                _txrx.Channel = progressChannel;
                System.Threading.Thread.Sleep(450);
                _txrx.Send("test Zip64 WinZip unzip");
                System.Threading.Thread.Sleep(120);
                _txrx.Send("status Creating files");
                _txrx.Send(String.Format("pb 0 max {0}", 3));

            
                // make sure it is larger than the 4.2gb size
                FileInfo fi = new FileInfo(ZipFileToExtract);
                Assert.IsTrue(fi.Length > (long)System.UInt32.MaxValue, "The zip file ({0}) is not large enough.", ZipFileToExtract);

                // This takes a long time, like an hour. Maybe skip it?
                // _txrx.Send("status Verifying the zip");
                // VerifyZip(ZipFileToUpdate);
            
                _txrx.Send("pb 0 step");

                _txrx.Send("status Counting entries in the zip file...");

                int numEntries = TestUtilities.CountEntries(ZipFileToExtract);

                _txrx.Send("status Using WinZip to list the entries...");

                // examine and unpack the zip archive via WinZip
                string progfiles = System.Environment.GetEnvironmentVariable("ProgramFiles");
                string wzzip = Path.Combine(progfiles, "winzip\\wzzip.exe");
                Assert.IsTrue(File.Exists(wzzip), "exe ({0}) does not exist", wzzip);

                // first, examine the zip entry metadata:
                string wzzipOut = this.Exec(wzzip, String.Format("-vt {0}", ZipFileToExtract));
                TestContext.WriteLine(wzzipOut);

                int x = 0;
                int y = 0;
                int wzzipEntryCount=0;
                string textToLookFor= "Filename: ";
                TestContext.WriteLine("================");
                TestContext.WriteLine("Files listed by WinZip:");
                while (true)
                {
                    x = wzzipOut.IndexOf(textToLookFor, y);
                    if (x < 0) break;
                    y = wzzipOut.IndexOf("\n", x);
                    string name = wzzipOut.Substring(x + textToLookFor.Length, y-x-1).Trim();
                    TestContext.WriteLine("  {0}", name);
                    if (!name.EndsWith("\\"))
                    {
                        wzzipEntryCount++;
                        if (wzzipEntryCount > numEntries * 3) throw new Exception("too many entries!");
                    }
                }
                TestContext.WriteLine("================");

                Assert.AreEqual(numEntries, wzzipEntryCount, "Unexpected number of entries found by WinZip.");

                _txrx.Send("pb 0 step");
                System.Threading.Thread.Sleep(120);

                _txrx.Send(String.Format("pb 1 max {0}", numEntries*2));
                x=0; y = 0;
                _txrx.Send("status Extracting the entries...");
                string wzunzip = Path.Combine(progfiles, "winzip\\wzunzip.exe");
                Assert.IsTrue(File.Exists(wzunzip), "exe ({0}) does not exist", wzunzip);
                int nCycles = 0;
                while (true)
                {
                    _txrx.Send("test Zip64 WinZip extract");
                    x = wzzipOut.IndexOf(textToLookFor, y);
                    nCycles++;
                    if (x < 0) break;
                    if (nCycles > numEntries * 4) throw new Exception("too many entries?");
                    y = wzzipOut.IndexOf("\n", x);
                    string name = wzzipOut.Substring(x + textToLookFor.Length, y-x-1).Trim();
                    if (!name.EndsWith("\\"))
                    {
                        _txrx.Send(String.Format("status Extracting {0} ({1}/{2})...", name, nCycles, wzzipEntryCount));
                        this.Exec(wzunzip,
                                       String.Format("-d {0} {1}\\ {2}", ZipFileToExtract, extractDir, name));
                        string path = Path.Combine(extractDir, name);
                        _txrx.Send("pb 1 step");
                        Assert.IsTrue(File.Exists(path), "extracted file ({0}) does not exist", path);
                        File.Delete(path);
                        System.Threading.Thread.Sleep(120);
                        _txrx.Send("pb 1 step");
                    }
                }

                _txrx.Send("pb 0 step");
                System.Threading.Thread.Sleep(120);

            }
            finally
            {
                try 
                {
                    if (_txrx!=null)
                    {
                        _txrx.Send("stop");
                        _txrx = null;
                    }
                }
                catch { }
            }

        }

        
    }
    
}
