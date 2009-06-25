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
// Time-stamp: <2009-June-23 21:16:36>
//
// ------------------------------------------------------------------
//
// This module defines the tests for compatibility for DotNetZip.  The
// idea is to verify that DotNetZip can read the zip files produced by
// other tools, and that other tools can read the output produced
// by DotNetZip. The tools and libraries tested are:
//  - WinZip,
//  - 7zip
//  - zipfldr.dll (via script)
//  - the Visual Studio DLL
//  - MS-Word
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
    public class Compatibility : IExec
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
            string output;
            
            int rc = TestUtilities.Exec_NoContext(RegAsm, String.Format("\"{0}\" /codebase /verbose", IonicZipDll), out output);
            if (rc != 0)
            {
                string cmd = String.Format("{0} \"{1}\" /codebase /verbose", RegAsm, IonicZipDll);
                throw new Exception(String.Format("Failed to register DotNetZip with COM rc({0}) cmd({1}) out({2})", rc, cmd, output));
            }
        }


        [ClassCleanup()]
            public static void MyClassCleanup()
        {
            string output;
            // unregister the DLL for COM interop
            int rc = TestUtilities.Exec_NoContext(RegAsm, String.Format("\"{0}\" /unregister /verbose", IonicZipDll), out output);
            if (rc != 0)
                throw new Exception(String.Format("Failed to unregister DotNetZip with COM  rc({0}) ({1})", rc, output));
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



        #if NONSENSE
            private static int StaticShellExec(string program, string args, out string output)
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = program;
                p.StartInfo.Arguments = args;
                p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.UseShellExecute = false;
                p.Start();

                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                return p.ExitCode;
            }

        private string ShellExec(string program, string args)
        {
            if (args == null)
                throw new ArgumentException("args");

            if (program == null)
                throw new ArgumentException("program");

            TestContext.WriteLine("running command: {0} {1}\n    ", program, args);

            string output;
            int rc = StaticShellExec(program, args, out output);

            if (rc != 0)
                throw new Exception(String.Format("Exception running app {0}: {1}", program, output));

            TestContext.WriteLine("output: {0}", output);

            return output;
        }
        #endif

        
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



        [TestMethod]
            public void Compat_ShellApplication_Unzip()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Compat_ShellApplication_Unzip.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

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
            string script = Path.Combine(testBin,"Resources\\VbsUnzip-ShellApp.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);


            this.Exec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
                           String.Format("\"{0}\" {1} {2}", script, ZipFileToCreate, ExtractDir));

            // check the files in the extract dir
            VerifyChecksums(Path.Combine("extract", "files"), FilesToZip, checksums);

        }




        [TestMethod]
            public void Compat_ShellApplication_SelectedFiles_Unzip()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Compat_ShellApplication_SelectedFiles_Unzip.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

            Directory.SetCurrentDirectory(TopLevelDir);

            // cons up the directories
            string extractDir = "extract";
            string dirToZip = "files";
            TestContext.WriteLine("creating dir '{0}' with files", dirToZip);
            Directory.CreateDirectory(dirToZip);

            int numFilesToAdd = _rnd.Next(5) + 6;
            int numFilesAdded = 0;
            int baseSize = _rnd.Next(0x100ff) + 8000;
            int nFilesInSubfolders= 0;
            Dictionary<string, byte[]> checksums= new Dictionary<string, byte[]>();
            var flist = new List<string>();
            for (int i = 0; i < numFilesToAdd && nFilesInSubfolders < 2; i++)
            {
                string fileName = string.Format("Test{0}.txt", i);
                if (i != 0)
                {
                    int x = _rnd.Next(4);
                    if (x != 0)
                    {
                        string folderName = string.Format("folder{0}", x);
                        fileName = Path.Combine(folderName, fileName);
                        if (!Directory.Exists(Path.Combine(dirToZip, folderName)))
                            Directory.CreateDirectory(Path.Combine(dirToZip, folderName));
                        nFilesInSubfolders++;
                    }
                }
                fileName = Path.Combine(dirToZip, fileName);
                TestUtilities.CreateAndFillFileBinary(fileName, baseSize + _rnd.Next(28000));
                var key = Path.GetFileName(fileName);
                var chk = TestUtilities.ComputeChecksum(fileName);
                checksums.Add(key, chk);
                flist.Add(fileName);
                numFilesAdded++;
            }

            // Create the zip archive
            var sw = new System.IO.StringWriter();
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.StatusMessageTextWriter = sw;
                zip1.StatusMessageTextWriter = Console.Out;
                zip1.AddSelectedFiles("*.*", dirToZip, "", true);
                zip1.Save(ZipFileToCreate);
            }
            TestContext.WriteLine(sw.ToString());


            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), numFilesAdded,
                                 "Incorrect number of entries in the zip file.");

            // run the unzip script
            string script = Path.Combine(testBin,"Resources\\VbsUnzip-ShellApp.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);


            this.Exec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
                           String.Format("\"{0}\" {1} {2}", script, ZipFileToCreate, Path.Combine(TopLevelDir, extractDir)));

            // check the files in the extract dir
            foreach (var fqPath in flist)
            {
                var f = Path.GetFileName(fqPath);
                var extractedFile = fqPath.Replace("files","extract");
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
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Compat_ShellApplication_Zip.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string testBin = TestUtilities.GetTestBinDir(CurrentDir);
            
            //DiagnoseEmbeddedItems();

            string Subdir = Path.Combine(TopLevelDir, "files");

            string[] FilesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(Subdir, out FilesToZip, out checksums);

            // Create the zip archive via script
            Directory.SetCurrentDirectory(TopLevelDir);
            string script = Path.Combine(testBin,"Resources\\VbsCreateZip-ShellApp.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);

            this.Exec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
                           String.Format("\"{0}\" {1} {2}", script, ZipFileToCreate, Subdir));

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

            String[] a = Array.ConvertAll(FilesToZip, x => Path.GetFileName(x));
            Microsoft.VisualStudio.Zip.ZipFileCompressor zfc = new Microsoft.VisualStudio.Zip.ZipFileCompressor(ZipFileToCreate, "files", a, true);

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
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

            string Subdir = Path.Combine(TopLevelDir, "files");

            string[] FilesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(Subdir, out FilesToZip, out checksums);

            // run the COM script to create the ZIP archive
            string script = Path.Combine(testBin,"Resources\\VbsCreateZip-DotNetZip.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);

            this.Exec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
                           String.Format("\"{0}\" {1} {2}", script, ZipFileToCreate, Subdir));

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
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

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
                for (int i = 0; i < FilesToZip.Length; i++)
                    zip1.AddItem(FilesToZip[i], "files");
                zip1.Save(ZipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                                 "Incorrect number of entries in the zip file.");


            // run the COM script to unzip the ZIP archive
            string script = Path.Combine(testBin,"Resources\\VbsUnzip-DotNetZip.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);

            this.Exec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
                           String.Format("\"{0}\" {1} {2}", script, ZipFileToCreate, ExtractDir));


            // check the files in the extract dir
            VerifyChecksums(Path.Combine("extract", "files"), FilesToZip, checksums);

        }




        [TestMethod]
            public void Compat_7z_Zip_COM_Unzip()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Compat_7z_Zip_COM_Unzip.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);
            
            Directory.SetCurrentDirectory(TopLevelDir);

            var sevenZip = Path.Combine(testBin,"Resources\\7z.exe");
            Assert.IsTrue(File.Exists(sevenZip), "exe ({0}) does not exist", sevenZip);

            // cons up the directories
            string ExtractDir = Path.Combine(TopLevelDir, "extract");
            string Subdir = Path.Combine(TopLevelDir, "files");

            string[] FilesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(Subdir, out FilesToZip, out checksums);

            // Create the zip archive via 7z.exe
            this.Exec(sevenZip, String.Format("a {0} {1}", ZipFileToCreate, Subdir));

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                                 "Incorrect number of entries in the zip file.");


            // run the COM script to unzip the ZIP archive
            string script = Path.Combine(testBin,"Resources\\VbsUnZip-DotNetZip.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);

            this.Exec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
                           String.Format("\"{0}\" {1} {2}", script, ZipFileToCreate, ExtractDir));


            // check the files in the extract dir
            VerifyChecksums(Path.Combine("extract", "files"), FilesToZip, checksums);

        }



        [TestMethod]
            public void Compat_7z_Zip_DotNetZip_Unzip()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Compat_7z_Zip_DotNetZip_Unzip.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

            // cons up the directories
            string ExtractDir = Path.Combine(TopLevelDir, "extract");
            string Subdir = Path.Combine(TopLevelDir, "files");

            string[] FilesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(Subdir, out FilesToZip, out checksums);

            // Create the zip archive via 7z.exe
            Directory.SetCurrentDirectory(TopLevelDir);
            string sevenZip = Path.Combine(testBin,"Resources\\7z.exe");
            Assert.IsTrue(File.Exists(sevenZip), "exe ({0}) does not exist", sevenZip);

            this.Exec(sevenZip, String.Format("a {0} {1}", ZipFileToCreate, Subdir));

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
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Compat_7z_Unzip_DotNetZip_Zip.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string testBin = TestUtilities.GetTestBinDir(CurrentDir);
            
            // cons up the directories
            string Subdir = Path.Combine(TopLevelDir, "files");

            string[] FilesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(Subdir, out FilesToZip, out checksums);

            // Create the zip archive with DotNetZip
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
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
            string sevenZip = Path.Combine(testBin,"Resources\\7z.exe");
            Assert.IsTrue(File.Exists(sevenZip), "exe ({0}) does not exist", sevenZip);
            this.Exec(sevenZip, String.Format("x {0}", ZipFileToCreate));

            // check the files in the extract dir
            Directory.SetCurrentDirectory(TopLevelDir);

            VerifyChecksums(Path.Combine("extract", "files"), FilesToZip, checksums);
        }


        
        [TestMethod]
            public void Compat_Winzip_Unzip_DotNetZip_Zip()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Compat_Winzip_Unzip_DotNetZip_Zip.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string testBin = TestUtilities.GetTestBinDir(CurrentDir);
            string dirInZip = "files";
            string extractDir = "extract";
            // cons up the directories
            string Subdir = Path.Combine(TopLevelDir, dirInZip);

            string[] FilesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(Subdir, out FilesToZip, out checksums);

            int i=0;
            // set R and S attributes on the first file
            if (!File.Exists(FilesToZip[i])) throw new Exception("Something is berry berry wrong.");
            File.SetAttributes(FilesToZip[i], FileAttributes.ReadOnly | FileAttributes.System);
                    
            // set H attribute on the second file
            i++;
            if (i == FilesToZip.Length) throw new Exception("Not enough files??.");
            if (!File.Exists(FilesToZip[i])) throw new Exception("Something is berry berry wrong.");
            File.SetAttributes(FilesToZip[i], FileAttributes.Hidden);


            // Now, Create the zip archive with DotNetZip
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                for (i = 0; i < FilesToZip.Length; i++)
                    zip1.AddItem(FilesToZip[i], dirInZip);
                zip1.Save(ZipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // examine and unpack the zip archive via WinZip
            var progfiles = System.Environment.GetEnvironmentVariable("ProgramFiles");
            string wzzip = Path.Combine(progfiles, "winzip\\wzzip.exe");
            Assert.IsTrue(File.Exists(wzzip), "exe ({0}) does not exist", wzzip);

            // first, examine the zip entry metadata:
            string wzzipOut = this.Exec(wzzip, String.Format("-vt {0}", ZipFileToCreate));

            string[] expectedAttrStrings = { "s-r-" , "-hw-",  "--w-" };
            
            // example: Filename: folder5\Test8.txt
            for (i=0; i < expectedAttrStrings.Length; i++)
            {
                var f = Path.GetFileName(FilesToZip[i]);
                var fileInZip = Path.Combine(dirInZip, f);
                string textToLookFor = String.Format("Filename: {0}", fileInZip.Replace("/","\\"));
                int x = wzzipOut.IndexOf(textToLookFor);
                Assert.IsTrue(x>0, "Could not find expected text ({0}) in WZZIP output.", textToLookFor);
                textToLookFor = "Attributes: ";
                x = wzzipOut.IndexOf(textToLookFor, x);
                string attrs = wzzipOut.Substring(x+textToLookFor.Length, 4);
                Assert.AreEqual(expectedAttrStrings[i], attrs, "Unexpected attributes on File {0}.", i);
            }

            
            // now, extract the zip
            // eg, wzunzip.exe -d test.zip  <extractdir>
            Directory.CreateDirectory(extractDir);
            Directory.SetCurrentDirectory(extractDir);
            string wzunzip = Path.Combine(progfiles, "winzip\\wzunzip.exe");
            Assert.IsTrue(File.Exists(wzunzip), "exe ({0}) does not exist", wzunzip);

            this.Exec(wzunzip, String.Format("-d {0}", ZipFileToCreate));

            // check the files in the extract dir
            Directory.SetCurrentDirectory(TopLevelDir);
            VerifyChecksums(Path.Combine("extract", dirInZip), FilesToZip, checksums);
        }



    }


}
