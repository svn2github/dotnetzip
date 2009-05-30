// Compatibility.cs
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
// Time-stamp: <2009-May-29 23:56:15>
//
// ------------------------------------------------------------------
//
// This module defines the tests for compatibility testing for DotNetZip.
// The idea is to verify that DotNetZip can read the zip files produced by other tools, and
// that other tools can read the output produced by DotNetZip.
//
// ------------------------------------------------------------------


using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;


using Ionic.Zip;
using Ionic.Zip.Tests.Utilities;
using System.IO;


namespace Ionic.Zip.Tests
{
    /// <summary>
    /// Summary description for Compatibility
    /// </summary>
    [TestClass]
    public class Compatibility
    {
        private System.Random _rnd;

        public Compatibility()
        {
            _rnd = new System.Random();
        }

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

        #region Additional test attributes
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
        // Use TestInitialize to run code before running each test 

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



        private System.Reflection.Assembly _myself;
        private System.Reflection.Assembly myself
        {
            get
            {
                if (_myself == null)
                {
                    _myself = System.Reflection.Assembly.GetExecutingAssembly();
                }
                return _myself;
            }
        }

        
        private void DiagnoseEmbeddedItems()
        {
            string[] x = myself.GetManifestResourceNames();
            TestContext.WriteLine("Listing Embedded Resources:");
            foreach (var s in x)
                TestContext.WriteLine("    {0}", s);
        }
    

    
        private System.IO.Stream GetEmbeddedStream(string name)
        {
            string embeddedName = String.Format("Ionic.Zip.Tests.Resources.{0}", name);
            return myself.GetManifestResourceStream(embeddedName);
        }
        

        private string ExtractEmbeddedStream(string name)
        {
            string fileToCreate = System.IO.Path.Combine(TopLevelDir, name);

            using (Stream s = GetEmbeddedStream(name))
            {
                if (s != null)
                {
                    using (FileStream o = File.Create(fileToCreate))
                    {
                        byte[] buffer = new byte[1024];
                        int n = -1;
                        while (n != 0)
                        {
                            n = s.Read(buffer, 0, buffer.Length);
                            if (n != 0)
                                o.Write(buffer, 0, n);
                        }
                    }
                }
            }

            return fileToCreate;
        }

        
        [TestMethod]
        public void Compat_ShellApplication_Unzip()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Compat_ShellApplication_Unzip.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string ExtractDir = System.IO.Path.Combine(TopLevelDir, "extract");
            string Subdir = System.IO.Path.Combine(TopLevelDir, "files");

            // create a bunch of files
            string[] FilesToZip = TestUtilities.GenerateFilesFlat(Subdir);

            // get checksums for each one
            var checksums = new Dictionary<string, byte[]>();
            foreach (var f in FilesToZip)
            {
                var key = System.IO.Path.GetFileName(f);
                var chk = TestUtilities.ComputeChecksum(f);
                checksums.Add(key, chk);
            }
            
            // Create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (int i = 0; i < FilesToZip.Length; i++)
                    zip1.AddItem(FilesToZip[i], "files");
                zip1.Save(ZipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // run the unzip script
            var script = ExtractEmbeddedStream("VbsUnzip.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = System.IO.Path.Combine(System.IO.Path.Combine(w, "system32"), "cscript.exe");
            p.StartInfo.Arguments=  String.Format("{0} {1} {2}", script, ZipFileToCreate, ExtractDir);
            TestContext.WriteLine("Running cmd: {0} {1}", p.StartInfo.FileName, p.StartInfo.Arguments);
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            // check the files in the extract dir
            foreach (var fqPath in FilesToZip)
            {
                var f = Path.GetFileName(fqPath);
                var extractedFile = Path.Combine("extract", Path.Combine("files", f));
                Assert.IsTrue(File.Exists(extractedFile), "File does not exist ({0})", extractedFile);
                var chk = TestUtilities.ComputeChecksum(extractedFile);
                Assert.AreEqual<String>(TestUtilities.CheckSumToString(checksums[f]),
                                         TestUtilities.CheckSumToString(chk),
                                         String.Format("Checksums for file {0} do not match.", f));
                checksums.Remove(f);
            }

            Assert.AreEqual<Int32>(0, checksums.Count, "Not all of the expected files were found in the extract directory.");
        }




        [TestMethod]
        public void Compat_ShellApplication_Zip()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Compat_ShellApplication_Zip.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            DiagnoseEmbeddedItems();
            
            string Subdir = System.IO.Path.Combine(TopLevelDir, "files");

            // create a bunch of files
            string[] FilesToZip = TestUtilities.GenerateFilesFlat(Subdir);

            // get checksums for each one
            var checksums = new Dictionary<string, byte[]>();
            foreach (var f in FilesToZip)
            {
                var key = System.IO.Path.GetFileName(f);
                var chk = TestUtilities.ComputeChecksum(f);
                checksums.Add(key, chk);
            }
            
            // Create the zip archive via script
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            var script = ExtractEmbeddedStream("VbsCreateZip.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = System.IO.Path.Combine(System.IO.Path.Combine(w, "system32"), "cscript.exe");
            p.StartInfo.Arguments=  String.Format("{0} {1} {2}", script, ZipFileToCreate, Subdir);
            TestContext.WriteLine("Running cmd: {0} {1}", p.StartInfo.FileName, p.StartInfo.Arguments);
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            
            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // unzip
            using (ZipFile zip1 = ZipFile.Read(ZipFileToCreate))
            {
                zip1.ExtractAll("extract");
            }

            
            // check the files in the extract dir
            foreach (var fqPath in FilesToZip)
            {
                var f = Path.GetFileName(fqPath);
                var extractedFile = Path.Combine("extract", f);
                Assert.IsTrue(File.Exists(extractedFile), "File does not exist ({0})", extractedFile);
                var chk = TestUtilities.ComputeChecksum(extractedFile);
                Assert.AreEqual<String>(TestUtilities.CheckSumToString(checksums[f]),
                                         TestUtilities.CheckSumToString(chk),
                                         String.Format("Checksums for file {0} do not match.", f));
                checksums.Remove(f);
            }

            Assert.AreEqual<Int32>(0, checksums.Count, "Not all of the expected files were found in the extract directory.");
        }


        
        [TestMethod]
        public void Compat_VStudio_Zip()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Compat_VStudio_Zip.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string Subdir = Path.Combine(TopLevelDir, "files");

            // create a bunch of files
            string[] FilesToZip = TestUtilities.GenerateFilesFlat(Subdir);

            // get checksums for each one
            var checksums = new Dictionary<string, byte[]>();
            foreach (var f in FilesToZip)
            {
                var key = System.IO.Path.GetFileName(f);
                var chk = TestUtilities.ComputeChecksum(f);
                checksums.Add(key, chk);
            }
            
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            
            String[] a= Array.ConvertAll(FilesToZip,  x => Path.GetFileName(x) );
            Microsoft.VisualStudio.Zip.ZipFileCompressor afc = new Microsoft.VisualStudio.Zip.ZipFileCompressor(ZipFileToCreate, "files", a, true);

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // unzip
            using (ZipFile zip1 = ZipFile.Read(ZipFileToCreate))
            {
                zip1.ExtractAll("extract");
            }

            
            // check the files in the extract dir
            foreach (var fqPath in FilesToZip)
            {
                var f = Path.GetFileName(fqPath);
                var extractedFile = Path.Combine("extract", f);
                Assert.IsTrue(File.Exists(extractedFile), "File does not exist ({0})", extractedFile);
                var chk = TestUtilities.ComputeChecksum(extractedFile);
                Assert.AreEqual<String>(TestUtilities.CheckSumToString(checksums[f]),
                                         TestUtilities.CheckSumToString(chk),
                                         String.Format("Checksums for file {0} do not match.", f));
                checksums.Remove(f);
            }

            Assert.AreEqual<Int32>(0, checksums.Count, "Not all of the expected files were found in the extract directory.");
        }


                
        [TestMethod]
        public void Compat_VStudio_UnZip()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Compat_VStudio_UnZip.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string Subdir = Path.Combine(TopLevelDir, "files");

            // create a bunch of files
            string[] FilesToZip = TestUtilities.GenerateFilesFlat(Subdir);

            // get checksums for each one
            var checksums = new Dictionary<string, byte[]>();
            foreach (var f in FilesToZip)
            {
                var key = System.IO.Path.GetFileName(f);
                var chk = TestUtilities.ComputeChecksum(f);
                checksums.Add(key, chk);
            }
            
            // Create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (int i = 0; i < FilesToZip.Length; i++)
                    zip1.AddItem(FilesToZip[i], "files");
                zip1.Save(ZipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // unzip
            var decompressor = new Microsoft.VisualStudio.Zip.ZipFileDecompressor(ZipFileToCreate, false, true, false);
            decompressor.UncompressToFolder("extract", false);

            
            // check the files in the extract dir
            foreach (var fqPath in FilesToZip)
            {
                var f = Path.GetFileName(fqPath);
                var extractedFile = Path.Combine("extract", Path.Combine("files", f));
                Assert.IsTrue(File.Exists(extractedFile), "File does not exist ({0})", extractedFile);
                var chk = TestUtilities.ComputeChecksum(extractedFile);
                Assert.AreEqual<String>(TestUtilities.CheckSumToString(checksums[f]),
                                         TestUtilities.CheckSumToString(chk),
                                         String.Format("Checksums for file {0} do not match.", f));
                checksums.Remove(f);
            }

            Assert.AreEqual<Int32>(0, checksums.Count, "Not all of the expected files were found in the extract directory.");
        }

    }

    
}
