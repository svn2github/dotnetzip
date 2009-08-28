// SplitArchives.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa.
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
// Time-stamp: <2009-August-28 15:16:22>
//
// ------------------------------------------------------------------
//
// This module defines tests for split (or 'spanned') archives. 
//
// ------------------------------------------------------------------

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
        public Split() : base() { }

        [TestMethod]
        public void Create_SegmentedArchive()
        {
            Directory.SetCurrentDirectory(TopLevelDir);
            string dirToZip = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

            int numFiles = _rnd.Next(9) + 8;
            
            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateLargeFilesWithChecksums(dirToZip, numFiles, out filesToZip, out checksums);
            
            //var filesToZip = TestUtilities.GenerateFilesFlat(dirToZip);
            int n = _rnd.Next(filesToZip.Length);

            int[] segmentSizes = { 0, 64*1024, 128*1024, 512*1024, 1024*1024, 2*1024*1024, 8*1024*1024 };

            for (int m=0; m < segmentSizes.Length; m++)
            {
                //Directory.SetCurrentDirectory(TopLevelDir);
                string trialDir = String.Format("trial{0}", m);
                Directory.CreateDirectory(trialDir);
                //Directory.SetCurrentDirectory(trialDir);

                string zipFileToCreate = Path.Combine(trialDir,
                                                      String.Format("Archive-{0}.zip",m));
                int maxSegSize = segmentSizes[m];

                TestContext.WriteLine("Trial {0}", m);
                if (maxSegSize > 0)
                    TestContext.WriteLine("Creating a segmented zip...segsize({0})", maxSegSize);
                else
                    TestContext.WriteLine("\nCreating a regular zip...");

                var sw = new StringWriter();
                using (var zip = new ZipFile())
                {
                    zip.StatusMessageTextWriter = sw;
                    zip.BufferSize = 0x8000;
                    zip.CodecBufferSize = 0x8000;
                    zip.AddDirectory(dirToZip, "files");
                    zip.MaxOutputSegmentSize = maxSegSize;
                    zip.Save(zipFileToCreate);
                }
                TestContext.WriteLine("{0}", sw.ToString());
        
                TestContext.WriteLine("\nNow, extracting...");
                sw = new StringWriter();
                string extractDir = String.Format("ex{0}", m);
                using (var zip = ZipFile.Read(zipFileToCreate))
                {
                    zip.StatusMessageTextWriter = sw;
                    foreach (string s in zip.Info.Split('\r','\n'))
                    {
                        Console.WriteLine("{0}", s);
                    }
            
                    foreach (var e in zip)
                        e.Extract(extractDir);
                }
                TestContext.WriteLine("{0}", sw.ToString());

                WinzipVerify(zipFileToCreate);

                // also verify checksums
                VerifyChecksums(Path.Combine(extractDir, "files"), filesToZip, checksums);
            }
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
