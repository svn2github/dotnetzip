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
// Time-stamp: <2009-May-30 08:25:49>
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

        private static string StaticShellExec(string program, string args)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = program;
            p.StartInfo.Arguments=  args;
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            
            return output;
        }




        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {            
            // get the path to the DotNetZip DLL
            string SourceDir = System.IO.Directory.GetCurrentDirectory();
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            IonicZipDll = Path.Combine(SourceDir, "Zip Full DLL\\bin\\Debug\\Ionic.Zip.dll");

            Assert.IsTrue(File.Exists(IonicZipDll), "DLL ({0}) does not exist", IonicZipDll);

            // register it for COM interop
            StaticShellExec(RegAsm, String.Format("/codebase {0}", IonicZipDll));
        }


        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            // unregister the DLL for COM interop
            StaticShellExec(RegAsm, String.Format("/unregister {0}", IonicZipDll));
        }

        
        private string CurrentDir;
        private string TopLevelDir;
        private static string IonicZipDll;
        private static string RegAsm = "c:\\windows\\Microsoft.NET\\Framework\\v2.0.50727\\regasm.exe";

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


        private string ShellExec(string program, string args)
        {
            if (args== null)
                throw new ArgumentException("args");
            
            if (program== null)
                throw new ArgumentException("program");
            
            TestContext.WriteLine("running command: {0} {1}\n    ", program, args);
            
            string output = StaticShellExec(program, args);
            
            TestContext.WriteLine("output: {0}", output);
            
            return output;
        }

        private static void CreateFilesAndChecksums(string Subdir, out string[] FilesToZip, out Dictionary<string, byte[]> checksums)
        {
            // create a bunch of files
            FilesToZip = TestUtilities.GenerateFilesFlat(Subdir);

            // get checksums for each one
            checksums = new Dictionary<string, byte[]>();
            foreach (var f in FilesToZip)
            {
                var key = Path.GetFileName(f);
                var chk = TestUtilities.ComputeChecksum(f);
                checksums.Add(key, chk);
            }
        }


        private static void VerifyChecksums(string extractDir, string[] FilesToZip, Dictionary<string, byte[]> checksums)
        {
            foreach (var fqPath in FilesToZip)
            {
                var f = Path.GetFileName(fqPath);
                var extractedFile = Path.Combine(extractDir, f);
                Assert.IsTrue(File.Exists(extractedFile), "File does not exist ({0})", extractedFile);
                var chk = TestUtilities.ComputeChecksum(extractedFile);
                Assert.AreEqual<String>(TestUtilities.CheckSumToString(checksums[f]),
                                        TestUtilities.CheckSumToString(chk),
                                        String.Format("Checksums for file {0} do not match.", f));
                checksums.Remove(f);
            }

            Assert.AreEqual<Int32>(0, checksums.Count, "Not all of the expected files were found in the extract directory.");
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
            string fileToCreate = Path.Combine(TopLevelDir, name);

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
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Compat_ShellApplication_Unzip.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // cons up the directories
            string ExtractDir = Path.Combine(TopLevelDir, "extract");
            string Subdir = Path.Combine(TopLevelDir, "files");

            string[] FilesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(Subdir, out FilesToZip, out checksums);
            
            // Create the zip archive
            Directory.SetCurrentDirectory(TopLevelDir);
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
            var script = ExtractEmbeddedStream("VbsUnzip-ShellApp.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);


            ShellExec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
                      String.Format("{0} {1} {2}", script, ZipFileToCreate, ExtractDir));
            
            // check the files in the extract dir
            VerifyChecksums(Path.Combine("extract", "files"), FilesToZip, checksums);

        }



        [TestMethod]
        public void Compat_ShellApplication_Zip()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Compat_ShellApplication_Zip.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            DiagnoseEmbeddedItems();
            
            string Subdir = Path.Combine(TopLevelDir, "files");

            string[] FilesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(Subdir, out FilesToZip, out checksums);
            
            // Create the zip archive via script
            Directory.SetCurrentDirectory(TopLevelDir);
            var script = ExtractEmbeddedStream("VbsCreateZip-ShellApp.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);

            ShellExec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
                                   String.Format("{0} {1} {2}", script, ZipFileToCreate, Subdir));
            
            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // unzip
            using (ZipFile zip1 = ZipFile.Read(ZipFileToCreate))
            {
                zip1.ExtractAll("extract");
            }

            
            // check the files in the extract dir
            VerifyChecksums("extract", FilesToZip, checksums);
        }


        
        [TestMethod]
        public void Compat_VStudio_Zip()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Compat_VStudio_Zip.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string Subdir = Path.Combine(TopLevelDir, "files");

            string[] FilesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(Subdir, out FilesToZip, out checksums);
            
            Directory.SetCurrentDirectory(TopLevelDir);
            
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
            VerifyChecksums("extract", FilesToZip, checksums);
            
        }


                
        [TestMethod]
        public void Compat_VStudio_UnZip()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Compat_VStudio_UnZip.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string Subdir = Path.Combine(TopLevelDir, "files");

            string[] FilesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(Subdir, out FilesToZip, out checksums);
            
            // Create the zip archive
            Directory.SetCurrentDirectory(TopLevelDir);
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
            VerifyChecksums(Path.Combine("extract", "files"), FilesToZip, checksums);
        }



        
        [TestMethod]
        public void Compat_COM_Zip()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Compat_COM_Zip.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            
            string Subdir = Path.Combine(TopLevelDir, "files");
            
            string[] FilesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(Subdir, out FilesToZip, out checksums);
            
            // run the COM script to create the ZIP archive
            var script = ExtractEmbeddedStream("VbsCreateZip-DotNetZip.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);

            ShellExec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
                               String.Format("{0} {1} {2}", script, ZipFileToCreate, Subdir));

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // unzip
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = ZipFile.Read(ZipFileToCreate))
            {
                zip1.ExtractAll("extract");
            }

            // check the files in the extract dir
            VerifyChecksums("extract", FilesToZip, checksums);
        }


        [TestMethod]
        public void Compat_COM_Unzip()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Compat_COM_Unzip.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            
            // cons up the directories
            string ExtractDir = Path.Combine(TopLevelDir, "extract");
            string Subdir = Path.Combine(TopLevelDir, "files");

            string[] FilesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(Subdir, out FilesToZip, out checksums);

            // Create the zip archive
            Directory.SetCurrentDirectory(TopLevelDir);
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


            // run the COM script to unzip the ZIP archive
            var script = ExtractEmbeddedStream("VbsUnZip-DotNetZip.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);

            ShellExec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
                               String.Format("{0} {1} {2}", script, ZipFileToCreate, ExtractDir));


            // check the files in the extract dir
            VerifyChecksums(Path.Combine("extract", "files"), FilesToZip, checksums);
        
        }



        
        [TestMethod]
        public void Compat_7z_Zip_COM_Unzip()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Compat_7z_Zip_COM_Unzip.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            
            // cons up the directories
            string ExtractDir = Path.Combine(TopLevelDir, "extract");
            string Subdir = Path.Combine(TopLevelDir, "files");

            string[] FilesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(Subdir, out FilesToZip, out checksums);

            // Create the zip archive via 7z.exe
            Directory.SetCurrentDirectory(TopLevelDir);
            var sevenZip = ExtractEmbeddedStream("7z.exe");
            Assert.IsTrue(File.Exists(sevenZip), "exe ({0}) does not exist", sevenZip);
            
            ShellExec(sevenZip, String.Format("a {0} {1}", ZipFileToCreate, Subdir));

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            
            // run the COM script to unzip the ZIP archive
            var script = ExtractEmbeddedStream("VbsUnZip-DotNetZip.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);

            ShellExec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
                               String.Format("{0} {1} {2}", script, ZipFileToCreate, ExtractDir));


            // check the files in the extract dir
            VerifyChecksums(Path.Combine("extract", "files"), FilesToZip, checksums);
        
        }


                
        [TestMethod]
        public void Compat_7z_Zip_DotNetZip_Unzip()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Compat_7z_Zip_COM_Unzip.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            
            // cons up the directories
            string ExtractDir = Path.Combine(TopLevelDir, "extract");
            string Subdir = Path.Combine(TopLevelDir, "files");

            string[] FilesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(Subdir, out FilesToZip, out checksums);

            // Create the zip archive via 7z.exe
            Directory.SetCurrentDirectory(TopLevelDir);
            var sevenZip = ExtractEmbeddedStream("7z.exe");
            Assert.IsTrue(File.Exists(sevenZip), "exe ({0}) does not exist", sevenZip);
             
            ShellExec(sevenZip, String.Format("a {0} {1}", ZipFileToCreate, Subdir));

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            
            // unzip
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = ZipFile.Read(ZipFileToCreate))
            {
                zip1.ExtractAll("extract");
            }

            // check the files in the extract dir
            VerifyChecksums(Path.Combine("extract", "files"), FilesToZip, checksums);
        }

        
        [TestMethod]
        public void Compat_7z_Unzip_DotNetZip_Zip()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Compat_7z_Zip_COM_Unzip.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            
            // cons up the directories
            string Subdir = Path.Combine(TopLevelDir, "files");

            string[] FilesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(Subdir, out FilesToZip, out checksums);
            
            // Create the zip archive with DotNetZip
            Directory.SetCurrentDirectory(TopLevelDir);
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

            // unpack the zip archive via 7z.exe
            Directory.CreateDirectory("extract");
            Directory.SetCurrentDirectory("extract");
            var sevenZip = ExtractEmbeddedStream("7z.exe");
            Assert.IsTrue(File.Exists(sevenZip), "exe ({0}) does not exist", sevenZip);
            ShellExec(sevenZip, String.Format("x {0}", ZipFileToCreate));


            // check the files in the extract dir
            Directory.SetCurrentDirectory(TopLevelDir);

            VerifyChecksums(Path.Combine("extract", "files"), FilesToZip, checksums);
        }

    }

    
}
