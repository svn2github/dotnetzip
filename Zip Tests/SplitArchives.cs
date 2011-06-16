// SplitArchives.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009-2011 Dino Chiesa.
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
// Time-stamp: <2011-June-16 09:28:07>
//
// ------------------------------------------------------------------
//
// This module defines tests for split (or 'spanned') archives.
//
// ------------------------------------------------------------------

// define REMOTE_FILESYSTEM in order to use a remote filesystem for storage of
// the ZIP64 large archive (which can beb huge). Leave it undefined to simply
// use the local TEMP directory.
//#define REMOTE_FILESYSTEM

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;


using Ionic.Zip.Tests.Utilities;


namespace Ionic.Zip.Tests.Split
{
    /// <summary>
    /// Summary description for ErrorTests
    /// </summary>
    [TestClass]
    public class Split : IonicTestClass
    {
        Ionic.CopyData.Transceiver _txrx;

        public Split() : base() { }

        [TestCleanup()]
        public void MyTestCleanupEx()
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


        void StartProgressMonitor(string progressChannel)
        {
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);
            string progressMonitorTool = Path.Combine(testBin, "Resources\\UnitTestProgressMonitor.exe");
            string requiredDll = Path.Combine(testBin, "Resources\\Ionic.CopyData.dll");
            Assert.IsTrue(File.Exists(progressMonitorTool), "progress monitor tool does not exist ({0})",  progressMonitorTool);
            Assert.IsTrue(File.Exists(requiredDll), "required DLL does not exist ({0})",  requiredDll);

            // start the progress monitor
            string ignored;
            //this.Exec(progressMonitorTool, String.Format("-channel {0}", progressChannel), false);
            TestUtilities.Exec_NoContext(progressMonitorTool, String.Format("-channel {0}", progressChannel), false, out ignored);
        }



        void StartProgressClient(string progressChannel, string title, string initialStatus)
        {
            _txrx = new Ionic.CopyData.Transceiver();
            System.Threading.Thread.Sleep(1000);
            _txrx.Channel = progressChannel;
            System.Threading.Thread.Sleep(450);
            _txrx.Send("test " + title);
            System.Threading.Thread.Sleep(120);
            _txrx.Send("status " + initialStatus);
        }


        [TestMethod, Timeout(360000)]  // 360000 - 6 minutes
        public void Create_SegmentedArchive()
        {
            string dirToZip = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

            string progressChannel = "segmentedzip";
            StartProgressMonitor(progressChannel);
            StartProgressClient(progressChannel, "Segmented Zips", "Creating files");

            _txrx.Send("pb 0 max 2");

            int numFiles = _rnd.Next(10) + 8;
            int overflows = 0;
            string msg;

            _txrx.Send(String.Format("pb 1 max {0}", numFiles));

            var update = new Action<int,int,Int64>( (x,y,z) => {
                    switch (x)
                    {
                        case 0:
                        _txrx.Send(String.Format("pb 2 max {0}", ((int)z)));
                        break;
                        case 1:
                        msg = String.Format("pb 2 value {0}", ((int)z));
                        _txrx.Send(msg);
                        break;
                        case 2:
                        _txrx.Send("pb 1 step");
                        _txrx.Send("pb 2 value 0");
                        msg = String.Format("status created {0}/{1} files",
                                            y+1,
                                            ((int)z));
                        _txrx.Send(msg);
                        break;
                    }
                });


            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateLargeFilesWithChecksums(dirToZip, numFiles, update,
                                          out filesToZip, out checksums);
            _txrx.Send("pb 0 step");
            int[] segmentSizes = { 0, 64*1024, 128*1024, 512*1024, 1024*1024,
                                   2*1024*1024, 8*1024*1024, 16*1024*1024,
                                   1024*1024*1024 };

            _txrx.Send("status zipping...");
            _txrx.Send(String.Format("pb 1 max {0}", segmentSizes.Length));

            System.EventHandler<Ionic.Zip.SaveProgressEventArgs> sp = (sender1, e1) =>
                {
                    switch (e1.EventType)
                    {
                        case ZipProgressEventType.Saving_Started:
                        _txrx.Send(String.Format("pb 2 max {0}", filesToZip.Length));
                        _txrx.Send("pb 2 value 0");
                        break;

                        case ZipProgressEventType.Saving_AfterWriteEntry:
                        TestContext.WriteLine("Saved entry {0}, {1} bytes",
                                              e1.CurrentEntry.FileName,
                                              e1.CurrentEntry.UncompressedSize);
                        _txrx.Send("pb 2 step");
                        break;
                    }
                };


            for (int m=0; m < segmentSizes.Length; m++)
            {
                string trialDir = String.Format("trial{0}", m);
                Directory.CreateDirectory(trialDir);
                string zipFileToCreate = Path.Combine(trialDir,
                                                      String.Format("Archive-{0}.zip",m));
                int maxSegSize = segmentSizes[m];

                msg = String.Format("status seg {0}/{1} ({2}k)",
                                    m+1, segmentSizes.Length, maxSegSize/1024);
                _txrx.Send(msg);

                TestContext.WriteLine("=======");
                TestContext.WriteLine("Trial {0}", m);
                if (maxSegSize > 0)
                    TestContext.WriteLine("Creating a segmented zip...segsize({0})", maxSegSize);
                else
                    TestContext.WriteLine("Creating a regular zip...");

                var sw = new StringWriter();
                bool aok = false;
                try
                {
                    using (var zip = new ZipFile())
                    {
                        zip.StatusMessageTextWriter = sw;
                        zip.BufferSize = 0x8000;
                        zip.CodecBufferSize = 0x8000;
                        zip.AddDirectory(dirToZip, "files");
                        zip.MaxOutputSegmentSize = maxSegSize;
                        zip.SaveProgress += sp;
                        zip.Save(zipFileToCreate);
                    }
                    aok = true;
                }
                catch (OverflowException)
                {
                    TestContext.WriteLine("Overflow - too many segments...");
                    overflows++;
                }

                if (aok)
                {
                    TestContext.WriteLine("{0}", sw.ToString());

                    // // If you want to see the diskNumber for each entry,
                    // // uncomment the following:
                    // TestContext.WriteLine("Checking info...");
                    // sw = new StringWriter();
                    // //string extractDir = String.Format("ex{0}", m);
                    // using (var zip = ZipFile.Read(zipFileToCreate))
                    // {
                    //     zip.StatusMessageTextWriter = sw;
                    //     foreach (string s in zip.Info.Split('\r','\n'))
                    //     {
                    //         Console.WriteLine("{0}", s);
                    //     }
                    //
                    //     // unnecessary - BasicVerify does this
                    //     //foreach (var e in zip)
                    //     //e.Extract(extractDir);
                    // }
                    // TestContext.WriteLine("{0}", sw.ToString());

                    TestContext.WriteLine("Extracting...");
                    string extractDir = BasicVerifyZip(zipFileToCreate);

                    // also verify checksums
                    VerifyChecksums(Path.Combine(extractDir, "files"), filesToZip, checksums);
                }
                _txrx.Send("pb 1 step");
            }

            _txrx.Send("pb 0 step");

            Assert.IsTrue(overflows < 3, "Too many overflows. Check the test.");
        }


        [TestMethod, Timeout(7200000)]  // 3600000 - 1 hour
        public void Create_LargeSegmentedArchive()
        {
            // There was a claim that large archives (around or above
            // 1gb) did not work well with archive splitting.  This test
            // covers that case.

#if REMOTE_FILESYSTEM
            string parentDir = Path.Combine("t:\\tdir", Path.GetFileNameWithoutExtension(TopLevelDir));
            _FilesToRemove.Add(parentDir);
            Directory.CreateDirectory(parentDir);
            string zipFileToCreate = Path.Combine(parentDir,
                                                  "Create_LargeSegmentedArchive.zip");
#else
            string zipFileToCreate = Path.Combine(TopLevelDir, "Create_LargeSegmentedArchive.zip");
#endif
            TestContext.WriteLine("Creating file {0}", zipFileToCreate);


            // int maxSegSize = 4*1024*1024;
            // int sizeBase =   10 * 1024 * 1024;
            // int sizeRandom = 1 * 1024 * 1024;
            // int numFiles = 3;

            int maxSegSize = 120*1024*1024;
            int sizeBase =   420 * 1024 * 1024;
            int sizeRandom = 16 * 1024 * 1024;
            int numFiles = _rnd.Next(5) + 11;
            int numSaving= 0, totalToSave = 0, numSegs= 0;
            bool pb1set = false;

            OpenDelegate opener = (name) =>
                {
                    return new Ionic.Zip.Tests.Utilities.RandomTextInputStream(sizeBase + _rnd.Next(sizeRandom));
                };

            CloseDelegate closer = (name, s) =>
                {
                    var rtg = (Ionic.Zip.Tests.Utilities.RandomTextInputStream) s;
                    rtg.Close();
                };

            System.EventHandler<Ionic.Zip.SaveProgressEventArgs> sp = (sender1, e1) =>
                {
                    switch (e1.EventType)
                    {
                        case ZipProgressEventType.Saving_Started:
                        numSaving= 0;
                        break;

                        case ZipProgressEventType.Saving_BeforeWriteEntry:
                        _txrx.Send("test Large Segmented ZIP");
                        _txrx.Send(String.Format("status saving {0}", e1.CurrentEntry.FileName));
                        pb1set= false;
                        totalToSave = e1.EntriesTotal;
                        numSaving++;
                        break;

                        case ZipProgressEventType.Saving_EntryBytesRead:
                        if (!pb1set)
                        {
                            _txrx.Send(String.Format("pb 1 max {0}", e1.TotalBytesToTransfer));
                            pb1set = true;
                        }
                        _txrx.Send(String.Format("status Saving entry {0}/{1} {2} :: {3}/{4}mb {5:N0}%",
                                                 numSaving, totalToSave,
                                                 e1.CurrentEntry.FileName,
                                                 e1.BytesTransferred/(1024*1024), e1.TotalBytesToTransfer/(1024*1024),
                                                 ((double)e1.BytesTransferred) / (0.01 * e1.TotalBytesToTransfer)));
                        string msg = String.Format("pb 1 value {0}", e1.BytesTransferred);
                        _txrx.Send(msg);
                        break;

                        case ZipProgressEventType.Saving_AfterWriteEntry:
                        TestContext.WriteLine("Saved entry {0}, {1} bytes", e1.CurrentEntry.FileName,
                                              e1.CurrentEntry.UncompressedSize);
                        _txrx.Send("pb 0 step");
                        pb1set = false;
                        break;
                    }
                };

            string progressChannel = "largesegmentedzip";
            StartProgressMonitor(progressChannel);
            StartProgressClient(progressChannel, "Large Segmented ZIP", "Creating files");

            _txrx.Send(String.Format("pb 0 max {0}", numFiles));

            // build a large zip file out of thin air
            var sw = new StringWriter();
            using (ZipFile zip = new ZipFile())
            {
                zip.StatusMessageTextWriter = sw;
                zip.BufferSize = 256 * 1024;
                zip.CodecBufferSize = 128 * 1024;
                zip.MaxOutputSegmentSize = maxSegSize;
                zip.SaveProgress += sp;

                for (int i = 0; i < numFiles; i++)
                {
                    string filename = TestUtilities.GetOneRandomUppercaseAsciiChar() +
                        Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".txt";
                    zip.AddEntry(filename, opener, closer);
                }
                zip.Save(zipFileToCreate);

                numSegs = zip.NumberOfSegmentsForMostRecentSave;
            }

#if REMOTE_FILESYSTEM
            if (((long)numSegs*maxSegSize) < (long)(1024*1024*1024L))
            {
                _FilesToRemove.Remove(parentDir);
                Assert.IsTrue(false, "There were not enough segments in that zip.  numsegs({0}) maxsize({1}).", numSegs, maxSegSize);
            }
#endif
            _txrx.Send("status Verifying that zip (this will take a while)...");

            BasicVerifyZip(zipFileToCreate);
        }



        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.ZipException))]
        public void Create_Split_InvalidSegmentSize()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Create_Split_InvalidSegmentSize.zip");
            Directory.SetCurrentDirectory(TopLevelDir);

            int segSize = 65536/3 + _rnd.Next(65536/2);
            using (var zip = new ZipFile())
            {
                zip.MaxOutputSegmentSize = segSize;
                zip.Save(zipFileToCreate);
            }
        }


    }
}
