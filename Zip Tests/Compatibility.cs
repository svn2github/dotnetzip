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
// Time-stamp: <2009-September-08 23:12:22>
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
    public class Compatibility : IonicTestClass
    {
        public Compatibility() : base()
        {
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


        private static string IonicZipDll;
        private static string RegAsm = "c:\\windows\\Microsoft.NET\\Framework\\v2.0.50727\\regasm.exe";



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




        private string _sevenZip = null;
        private string sevenZip
        {
            get
            {
               if (_sevenZip == null)
               {
                   string testBin = TestUtilities.GetTestBinDir(CurrentDir);
                   _sevenZip = Path.Combine(testBin, "Resources\\7z.exe");
                   Assert.IsTrue(File.Exists(_sevenZip), "exe ({0}) does not exist", _sevenZip);
               }
               return _sevenZip;
            }
        }


        
        private string _wzzip = null;
        private string wzzip
        {
            get
            {
               if (_wzzip == null)
               {
                string progfiles = System.Environment.GetEnvironmentVariable("ProgramFiles");
                _wzzip = Path.Combine(progfiles, "winzip\\wzzip.exe");
                Assert.IsTrue(File.Exists(_wzzip), "exe ({0}) does not exist", _wzzip);
               }
               return _wzzip;
            }
        }
        
        
        private string _wzunzip = null;
        private string wzunzip
        {
            get
            {
               if (_wzunzip == null)
               {
                string progfiles = System.Environment.GetEnvironmentVariable("ProgramFiles");
                _wzunzip = Path.Combine(progfiles, "winzip\\wzunzip.exe");
                Assert.IsTrue(File.Exists(_wzunzip), "exe ({0}) does not exist", _wzunzip);
               }
               return _wzunzip;
            }
        }
        
        
        private string _windir = null;
        private string windir
        {
            get
            {
               if (_windir == null)
               {
                   _windir = System.Environment.GetEnvironmentVariable("Windir");
                   Assert.IsTrue(Directory.Exists(_windir), "%windir% does not exist ({0})", _windir);
               }
               return _windir;
            }
        }

        private string _cscriptExe = null;
        private string cscriptExe
        {
            get
            {
               if (_cscriptExe == null)
               {
                   _cscriptExe = Path.Combine(Path.Combine(windir, "system32"), "cscript.exe");
                   Assert.IsTrue(File.Exists(_cscriptExe), "cscript.exe does not exist ({0})", _cscriptExe);
               }
               return _cscriptExe;
            }
        }
        

        private string GetScript(string scriptName)
        {
            // check existence of script and script engine
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);
            string script = Path.Combine(testBin, String.Format("Resources\\{0}", scriptName));
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            return script;
        }

        private void VerifyFileTimes(string extractDir, 
                                            System.Collections.Generic.IEnumerable<String> filesToCheck,
                                            bool useLowThreshold)
        {
            TimeSpan threshold = (useLowThreshold)
                ? new TimeSpan(10000)
                : new TimeSpan(0,0,2);
            
            TestContext.WriteLine("Using threshold: ({0})", threshold.ToString());

            foreach (var fqPath in filesToCheck)
            {
                var f = Path.GetFileName(fqPath);
                var extractedFile = Path.Combine(extractDir, f);
                Assert.IsTrue(File.Exists(extractedFile), "File does not exist ({0})", extractedFile);

                // check times
                DateTime t1 = File.GetLastWriteTimeUtc(fqPath);
                DateTime t2 = File.GetLastWriteTimeUtc(extractedFile);
                TestContext.WriteLine("{0} lastwrite orig({1})  extracted({2})",
                                      Path.GetFileName(fqPath),
                                      t1.ToString("G"),
                                      t2.ToString("G"));
                //TestContext.WriteLine("{0} lastwrite({1})", extractedFile, t2.ToString("G"));
                TimeSpan delta = t1 - t2;
                if (useLowThreshold)
                {
                    Assert.AreEqual<DateTime>(t1, t2, "LastWriteTime ({0})", delta.ToString());
                    t1 = File.GetCreationTimeUtc(fqPath);
                    t2 = File.GetCreationTimeUtc(extractedFile);
                    delta = t1 - t2;
                    //Assert.AreEqual<DateTime>(t1, t2, "CreationTime ({0})", delta.ToString());
                    Assert.IsTrue(delta<=threshold, "LastWriteTime ({0})", delta.ToString());
                }
                else
                    Assert.IsTrue(delta<=threshold, "LastWriteTime ({0})", delta.ToString());

            }
        }


        private void VerifyNtfsTimes(string extractDir, 
            System.Collections.Generic.IEnumerable<String> filesToCheck)
        {
            VerifyFileTimes(extractDir, filesToCheck, true);
        }
                
        private void VerifyDosTimes(string extractDir, 
            System.Collections.Generic.IEnumerable<String> filesToCheck)
        {
            VerifyFileTimes(extractDir, filesToCheck, false);
        }
                

        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.ZipException))]
        public void Compat_ZipFile_Initialize_Error()
        {
            string notaZipFile = GetScript("VbsUnzip-ShellApp.vbs");

            // try to read a bogus zip archive
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Initialize(notaZipFile);
            }
        }



        
        [TestMethod]
        public void Compat_ShellApplication_Unzip()
        {
            // get a set of files to zip up 
            string subdir = Path.Combine(TopLevelDir, "files");
            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            var script = GetScript("VbsUnzip-ShellApp.vbs");
            
            var compressionLevels = Enum.GetValues(typeof(Ionic.Zlib.CompressionLevel));
            int i=0;
            foreach (var compLevel in compressionLevels)
            {
                // create and fill the directories
                string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("Compat_ShellApplication_Unzip.{0}.zip", i));
                string extractDir = Path.Combine(TopLevelDir, String.Format("extract.{0}",i));

                // Create the zip archive
                Directory.SetCurrentDirectory(TopLevelDir);
                using (ZipFile zip1 = new ZipFile())
                {
                    zip1.CompressionLevel = (Ionic.Zlib.CompressionLevel) compLevel;
                    //zip.StatusMessageTextWriter = System.Console.Out;
                    for (int j = 0; j < filesToZip.Length; j++)
                        zip1.AddItem(filesToZip[j], "files");
                    zip1.Save(zipFileToCreate);
                }

                // Verify the number of files in the zip
                Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                     "Incorrect number of entries in the zip file.");

                // run the unzip script
                this.Exec(cscriptExe,
                          String.Format("\"{0}\" {1} {2}", script, zipFileToCreate, extractDir));

                // check the files in the extract dir
                VerifyChecksums(Path.Combine(extractDir, "files"), filesToZip, checksums);

                // verify the file times
                VerifyDosTimes(Path.Combine(extractDir, "files"), filesToZip);
                i++;
            }
        }


        [TestMethod]
        public void Compat_ShellApplication_Unzip_NonSeekableOutput()
        {
            // get a set of files to zip up 
            string subdir = Path.Combine(TopLevelDir, "files");
            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            var script = GetScript("VbsUnzip-ShellApp.vbs");

            var compressionLevels = Enum.GetValues(typeof(Ionic.Zlib.CompressionLevel));
            int i=0;
            foreach (var compLevel in compressionLevels)
            {
                // create and fill the directories
                string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("Compat_ShellApplication_Unzip_NonSeekableOutput.{0}.zip", i));
                string extractDir = Path.Combine(TopLevelDir, String.Format("extract.{0}",i));

                // Create the zip archive
                Directory.SetCurrentDirectory(TopLevelDir);

                // Want to test the library when saving to non-seekable output streams.  Like
                // stdout or ASPNET's Response.OutputStream.  This simulates it. 
                using (var rawOut = System.IO.File.Create(zipFileToCreate))
                {
                    using (var nonSeekableOut = new Ionic.Zip.Tests.NonSeekableOutputStream(rawOut))
                    {
                        using (ZipFile zip1 = new ZipFile())
                        {
                            zip1.CompressionLevel = (Ionic.Zlib.CompressionLevel) compLevel;
                            for (int j = 0; j < filesToZip.Length; j++)
                                zip1.AddItem(filesToZip[j], "files");
                            zip1.Save(nonSeekableOut);
                        }
                    }
                }
                
                // Verify the number of files in the zip
                Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                     "Incorrect number of entries in the zip file.");

                // run the unzip script
                this.Exec(cscriptExe,
                          String.Format("\"{0}\" {1} {2}", script, zipFileToCreate, extractDir));

                // check the files in the extract dir
                VerifyChecksums(Path.Combine(extractDir, "files"), filesToZip, checksums);

                // verify the file times
                VerifyDosTimes(Path.Combine(extractDir, "files"), filesToZip);
                i++;
            }
        }

        
#if SHELLAPP_UNZIP_SFX

        [TestMethod]
        public void Compat_ShellApplication_Unzip_SFX()
        {
            // get a set of files to zip up 
            string subdir = Path.Combine(TopLevelDir, "files");
            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            var script = GetScript("VbsUnzip-ShellApp.vbs");

            var compressionLevels = Enum.GetValues(typeof(Ionic.Zlib.CompressionLevel));
            int i=0;
            foreach (var compLevel in compressionLevels)
            {
                // create and fill the directories
                string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("Compat_ShellApp_Unzip_SFX.{0}.exe", i));
                string extractDir = Path.Combine(TopLevelDir, String.Format("extract.{0}",i));

                // Create the zip archive
                Directory.SetCurrentDirectory(TopLevelDir);
                using (ZipFile zip1 = new ZipFile())
                {
                    zip1.CompressionLevel = (Ionic.Zlib.CompressionLevel) compLevel;
                    //zip.StatusMessageTextWriter = System.Console.Out;
                    for (int j = 0; j < filesToZip.Length; j++)
                        zip1.AddItem(filesToZip[j], "files");
                    zip1.SaveSelfExtractor(zipFileToCreate, SelfExtractorFlavor.ConsoleApplication);
                }

                // Verify the number of files in the zip
                Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                     "Incorrect number of entries in the zip file.");

                // run the unzip script
                this.Exec(cscriptExe,
                          String.Format("\"{0}\" {1} {2}", script, zipFileToCreate, extractDir));

                // check the files in the extract dir
                VerifyChecksums(Path.Combine(extractDir, "files"), filesToZip, checksums);

                // verify the file times
                VerifyDosTimes(Path.Combine(extractDir, "files"), filesToZip);
                i++;
            }
        }
#endif

        

        [TestMethod]
        public void Compat_ShellApplication_Unzip_2()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_ShellApplication_Unzip-2.zip");

            // create and fill the directories
            string extractDir = Path.Combine(TopLevelDir, "extract");
            string subdir = Path.Combine(TopLevelDir, "files");

            var checksums = new Dictionary<string, byte[]> ();
            var filesToZip = GetSelectionOfTempFiles(_rnd.Next(13) + 8, checksums);

            // Create the zip archive
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddFiles(filesToZip, "files");
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Count,
                                 "Incorrect number of entries in the zip file.");

            // run the unzip script
            string script = GetScript("VbsUnzip-ShellApp.vbs");

            this.Exec(cscriptExe,
                      String.Format("\"{0}\" {1} {2}", script, zipFileToCreate, extractDir));

            // check the files in the extract dir
            VerifyChecksums(Path.Combine(extractDir, "files"), filesToZip, checksums);

            // verify the file times
            VerifyDosTimes(Path.Combine(extractDir, "files"), filesToZip);
        }



        [TestMethod]
        public void Compat_ShellApplication_SelectedFiles_Unzip()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_ShellApplication_SelectedFiles_Unzip.zip");

            Directory.SetCurrentDirectory(TopLevelDir);

            // create and fill the directories
            string extractDir = "extract";
            string dirToZip = "files";
            TestContext.WriteLine("creating dir '{0}' with files", dirToZip);
            Directory.CreateDirectory(dirToZip);

            int numFilesToAdd = _rnd.Next(5) + 6;
            int numFilesAdded = 0;
            int baseSize = _rnd.Next(0x100ff) + 8000;
            int nFilesInSubfolders = 0;
            Dictionary<string, byte[]> checksums = new Dictionary<string, byte[]>();
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
                zip1.Save(zipFileToCreate);
            }
            TestContext.WriteLine(sw.ToString());


            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), numFilesAdded,
                                 "Incorrect number of entries in the zip file.");

            // run the unzip script
            string script = GetScript("VbsUnzip-ShellApp.vbs");

            this.Exec(cscriptExe,
                      String.Format("\"{0}\" {1} {2}", script, zipFileToCreate, Path.Combine(TopLevelDir, extractDir)));

            // check the files in the extract dir
            foreach (var fqPath in flist)
            {
                var f = Path.GetFileName(fqPath);
                var extractedFile = fqPath.Replace("files", "extract");
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
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_ShellApplication_Zip.zip");
            string subdir = Path.Combine(TopLevelDir, "files");
            string extractDir = "extract";

            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            // Create the zip archive via script
            Directory.SetCurrentDirectory(TopLevelDir);
            
            string script = GetScript("VbsCreateZip-ShellApp.vbs");

            this.Exec(cscriptExe,
                      String.Format("\"{0}\" {1} {2}", script, zipFileToCreate, subdir));

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // unzip
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                zip1.ExtractAll(extractDir);
            }

            // check the files in the extract dir
            VerifyChecksums(extractDir, filesToZip, checksums);

            VerifyDosTimes(extractDir, filesToZip);
        }


        [TestMethod]
        public void Compat_ShellApplication_Zip_2()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_ShellApplication_Zip.zip");

            string subdir = Path.Combine(TopLevelDir, "files");
            string extractDir = "extract";

            Dictionary<string, byte[]> checksums = new Dictionary<string, byte[]>();
            var filesToZip = GetSelectionOfTempFiles(_rnd.Next(33) + 11, checksums);

                Directory.CreateDirectory(subdir);
                Directory.SetCurrentDirectory(subdir);
                var w = System.Environment.GetEnvironmentVariable("Windir");
                Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);
                var fsutil = Path.Combine(Path.Combine(w, "system32"), "fsutil.exe");
                Assert.IsTrue(File.Exists(fsutil), "fsutil.exe does not exist ({0})", fsutil);
                string ignored;
                foreach (var f in filesToZip)
                {
                    Assert.IsTrue(File.Exists(f));
                    string cmd = String.Format("hardlink create \"{0}\" \"{1}\"", Path.GetFileName(f), f);
                    TestUtilities.Exec_NoContext(fsutil, cmd, out ignored);
                }
            
            // Create the zip archive via script
            Directory.SetCurrentDirectory(TopLevelDir);
            string script = GetScript("VbsCreateZip-ShellApp.vbs");

            this.Exec(cscriptExe,
                      String.Format("\"{0}\" {1} {2}", script, zipFileToCreate, subdir));

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Count,
                                 "Incorrect number of entries in the zip file.");

            // unzip
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                zip1.ExtractAll(extractDir);
            }

            // check the files in the extract dir
            VerifyChecksums(extractDir, filesToZip, checksums);

            VerifyDosTimes(extractDir, filesToZip);
        }



        [TestMethod]
        public void Compat_VStudio_Zip()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_VStudio_Zip.zip");
            string subdir = Path.Combine(TopLevelDir, "files");
            string extractDir = "extract";
            
            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            Directory.SetCurrentDirectory(TopLevelDir);

            String[] a = Array.ConvertAll(filesToZip, x => Path.GetFileName(x));
            Microsoft.VisualStudio.Zip.ZipFileCompressor zfc = new Microsoft.VisualStudio.Zip.ZipFileCompressor(zipFileToCreate, "files", a, true);

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // unzip
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                zip1.ExtractAll(extractDir);
            }

            // check the files in the extract dir
            VerifyChecksums(extractDir, filesToZip, checksums);

            // visual Studio's ZIP library doesn't bother with times...
            //VerifyNtfsTimes(extractDir, filesToZip);
        }



        [TestMethod]
        public void Compat_VStudio_UnZip()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_VStudio_UnZip.zip");
            string subdir = Path.Combine(TopLevelDir, "files");
            string extractDir = "extract";

            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            // Create the zip archive
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                for (int i = 0; i < filesToZip.Length; i++)
                    zip1.AddItem(filesToZip[i], "files");
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // unzip
            var decompressor = new Microsoft.VisualStudio.Zip.ZipFileDecompressor(zipFileToCreate, false, true, false);
            decompressor.UncompressToFolder(extractDir, false);

            // check the files in the extract dir
            VerifyChecksums(Path.Combine(extractDir, "files"), filesToZip, checksums);
            
            // visual Studio's ZIP library doesn't bother with times...
            //VerifyNtfsTimes(Path.Combine(extractDir, "files"), filesToZip);
        }




        [TestMethod]
        public void Compat_COM_Zip()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_COM_Zip.zip");
            string subdir = Path.Combine(TopLevelDir, "files");
            string extractDir = "extract";
            
            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            // run the COM script to create the ZIP archive
            string script = GetScript("VbsCreateZip-DotNetZip.vbs");

            this.Exec(cscriptExe,
                      String.Format("\"{0}\" {1} {2}", script, zipFileToCreate, subdir));

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // unzip
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                zip1.ExtractAll(extractDir);
            }

            // check the files in the extract dir
            VerifyChecksums(extractDir, filesToZip, checksums);
            
            VerifyNtfsTimes(extractDir, filesToZip);            
        }



        [TestMethod]
        public void Compat_COM_Unzip()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_COM_Unzip.zip");

            // construct the directories
            //string ExtractDir = Path.Combine(TopLevelDir, "extract");
            string extractDir = "extract";
            string subdir = Path.Combine(TopLevelDir, "files");

            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            // Create the zip archive
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                for (int i = 0; i < filesToZip.Length; i++)
                    zip1.AddItem(filesToZip[i], "files");
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                 "Incorrect number of entries in the zip file.");


            // run the COM script to unzip the ZIP archive
            string script = GetScript("VbsUnzip-DotNetZip.vbs");
            this.Exec(cscriptExe,
                      String.Format("\"{0}\" {1} {2}", script, zipFileToCreate, extractDir));


            // check the files in the extract dir
            VerifyChecksums(Path.Combine(extractDir, "files"), filesToZip, checksums);

            VerifyNtfsTimes(Path.Combine(extractDir, "files"), filesToZip);
        }


        [TestMethod]
        public void Compat_COM_Check()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_COM_Check.zip");

            // create and fill the directories
            string subdir = Path.Combine(TopLevelDir, "files");
            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            // Create the zip archive
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                for (int i = 0; i < filesToZip.Length; i++)
                    zip1.AddItem(filesToZip[i], "files");
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // run the COM script to check the ZIP archive
            string script = GetScript("TestCheckZip.js");

            string testOut= this.Exec(cscriptExe,
                                      String.Format("\"{0}\" {1}", script, zipFileToCreate));

            Assert.IsTrue(testOut.StartsWith("That zip is OK"));
        }
        

        [TestMethod]
        public void Compat_COM_CheckWithExtract()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_COM_CheckWithExtract.zip");

            // create and fill the directories
            string subdir = Path.Combine(TopLevelDir, "files");
            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            // Create the zip archive
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                for (int i = 0; i < filesToZip.Length; i++)
                    zip1.AddItem(filesToZip[i], "files");
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // run the COM script to check and test-extract the ZIP archive
            string script = GetScript("TestCheckZip.js");

            string testOut= this.Exec(cscriptExe,
                                      String.Format("\"{0}\" -x {1}", script, zipFileToCreate));

            Assert.IsTrue(testOut.StartsWith("That zip is OK"), "output: {0}", testOut);
        }
        

        [TestMethod]
        public void Compat_COM_CheckError()
        {
            Directory.SetCurrentDirectory(TopLevelDir);

            // run the COM script to check the (not) ZIP archive
            string script = GetScript("TestCheckZip.js");

            string testOut= this.Exec(cscriptExe,
                                      String.Format("\"{0}\" {1}", script, cscriptExe));

            Assert.IsTrue(testOut.StartsWith("That zip is not OK"));
        }
        

        [TestMethod]
        public void Compat_7z_Zip_1()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_7z_Zip_COM_Unzip.zip");

            Directory.SetCurrentDirectory(TopLevelDir);


            // create and fill the directories
            //string ExtractDir = Path.Combine(TopLevelDir, "extract");
            string extractDir = "extract";
            string subdir = Path.Combine(TopLevelDir, "files");

            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            // Create the zip archive via 7z.exe
            this.Exec(sevenZip, String.Format("a {0} {1}", zipFileToCreate, subdir));

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                 "Incorrect number of entries in the zip file.");


            // run the COM script to unzip the ZIP archive
            string script = GetScript("VbsUnZip-DotNetZip.vbs");

            this.Exec(cscriptExe,
                      String.Format("\"{0}\" {1} {2}", script, zipFileToCreate, extractDir));

            // check the files in the extract dir
            VerifyChecksums(Path.Combine(extractDir, "files"), filesToZip, checksums);

            VerifyNtfsTimes(Path.Combine(extractDir, "files"), filesToZip);
            
        }



        [TestMethod]
        public void Compat_7z_Zip_2()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_7z_Zip_2.zip");

            // create and fill the directories
            string extractDir = "extract";
            string subdir = Path.Combine(TopLevelDir, "files");

            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            // Create the zip archive via 7z.exe
            Directory.SetCurrentDirectory(TopLevelDir);

            this.Exec(sevenZip, String.Format("a {0} {1}", zipFileToCreate, subdir));

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // unzip
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                zip1.ExtractAll(extractDir);
            }

            // check the files in the extract dir
            VerifyChecksums(Path.Combine(extractDir, "files"), filesToZip, checksums);

            VerifyNtfsTimes(Path.Combine(extractDir, "files"), filesToZip);
        }

        

        [TestMethod]
        public void Compat_7z_Unzip()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_7z_Unzip.zip");

            // create and fill the directories
            string subdir = Path.Combine(TopLevelDir, "files");

            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            // Create the zip archive with DotNetZip
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                for (int i = 0; i < filesToZip.Length; i++)
                    zip1.AddItem(filesToZip[i], "files");
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // unpack the zip archive via 7z.exe
            Directory.CreateDirectory("extract");
            Directory.SetCurrentDirectory("extract");
            this.Exec(sevenZip, String.Format("x {0}", zipFileToCreate));

            // check the files in the extract dir
            Directory.SetCurrentDirectory(TopLevelDir);

            VerifyChecksums(Path.Combine("extract", "files"), filesToZip, checksums);
        }

        
        [TestMethod]
        public void Compat_7z_Unzip_Password()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_7z_Unzip_Password.zip");
            string password=  Path.GetRandomFileName();
            string subdir = Path.Combine(TopLevelDir, "files");

            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            // Create the zip archive with DotNetZip
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Password = password;
                zip1.AddFiles(filesToZip, "files");
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // unpack the zip archive via 7z.exe
            Directory.CreateDirectory("extract");
            Directory.SetCurrentDirectory("extract");
            this.Exec(sevenZip, String.Format("x -p{0} {1}", password, zipFileToCreate));

            // check the files in the extract dir
            Directory.SetCurrentDirectory(TopLevelDir);

            VerifyChecksums(Path.Combine("extract", "files"), filesToZip, checksums);
        }



                
        [TestMethod]
        public void Compat_7z_Unzip_Password_NonSeekableOutput()
        {
            string subdir = Path.Combine(TopLevelDir, "files");

            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            var compressionLevels = Enum.GetValues(typeof(Ionic.Zlib.CompressionLevel));
            int i=0;
            foreach (var compLevel in compressionLevels)
            {
                string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("Compat_7z_Unzip_Password_NonSeekableOutput.{0}.zip",i));
                string password=  Path.GetRandomFileName();
                string extractDir = Path.Combine(TopLevelDir, String.Format("extract.{0}",i));

                // Create the zip archive with DotNetZip
                Directory.SetCurrentDirectory(TopLevelDir);
                // Want to test the library when saving to non-seekable output streams.  Like
                // stdout or ASPNET's Response.OutputStream.  This simulates it. 
                using (var rawOut = System.IO.File.Create(zipFileToCreate))
                {
                    using (var nonSeekableOut = new Ionic.Zip.Tests.NonSeekableOutputStream(rawOut))
                    {
                        using (ZipFile zip1 = new ZipFile())
                        {
                            zip1.CompressionLevel = (Ionic.Zlib.CompressionLevel) compLevel;
                            zip1.Password = password;
                            zip1.AddFiles(filesToZip, "files");
                            zip1.Save(nonSeekableOut);
                        }
                    }
                }

                // Verify the number of files in the zip
                Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                     "Incorrect number of entries in the zip file.");

                // unpack the zip archive via 7z.exe
                Directory.CreateDirectory(extractDir);
                this.Exec(sevenZip, String.Format("x -o{0} -p{1} {2}",
                                                  Path.GetFileName(extractDir),
                                                  password,
                                                  zipFileToCreate));

                // check the files in the extract dir
                Directory.SetCurrentDirectory(TopLevelDir);

                VerifyChecksums(Path.Combine(extractDir, "files"), filesToZip, checksums);
                i++;
            }
        }



        [TestMethod]
        public void Compat_7z_Unzip_SFX()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_7z_Unzip_SFX.exe");

            // create and fill the directories
            string subdir = Path.Combine(TopLevelDir, "files");

            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            // Create the zip archive with DotNetZip
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                for (int i = 0; i < filesToZip.Length; i++)
                    zip1.AddItem(filesToZip[i], "files");
                zip1.SaveSelfExtractor(zipFileToCreate, SelfExtractorFlavor.ConsoleApplication);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                 "Incorrect number of entries in the zip file.");

            // unpack the zip archive via 7z.exe
            Directory.CreateDirectory("extract");
            Directory.SetCurrentDirectory("extract");
            this.Exec(sevenZip, String.Format("x {0}", zipFileToCreate));

            // check the files in the extract dir
            Directory.SetCurrentDirectory(TopLevelDir);

            VerifyChecksums(Path.Combine("extract", "files"), filesToZip, checksums);
        }



        [TestMethod]
        public void Compat_Winzip_Zip()
        {
            Compat_Winzip_Zip_Variable("");
        }

        
        [TestMethod]
        public void Compat_Winzip_Zip_Password()
        {
            string password = Path.GetRandomFileName().Replace(".","@");
            TestContext.WriteLine("creating zip with password ({0})", password );
            string zipfile = Compat_Winzip_Zip_Variable("-s"+password, false);
            string extractDir = "extract";
            
            // unzip with DotNetZip
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = ZipFile.Read(zipfile))
            {
                zip1.Password = password;
                zip1.ExtractAll(extractDir);
            }
        }

        
        [TestMethod]
        public void Compat_Winzip_Zip_Normal()
        {
            Compat_Winzip_Zip_Variable("-en");
        }

        [TestMethod]
        public void Compat_Winzip_Zip_Fast()
        {
            Compat_Winzip_Zip_Variable("-ef");
        }

        [TestMethod]
        public void Compat_Winzip_Zip_SuperFast()
        {
            Compat_Winzip_Zip_Variable("-es");
        }

        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.ZipException))]
        public void Compat_Winzip_Zip_EZ()
        {
            // Unsupported compression method
            Compat_Winzip_Zip_Variable("-ez");
        }

        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.ZipException))]
        public void Compat_Winzip_Zip_PPMd()
        {
            // Unsupported compression method
            Compat_Winzip_Zip_Variable("-ep");
        }

        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.ZipException))]
        public void Compat_Winzip_Zip_Bzip2()
        {
            // Unsupported compression method
            Compat_Winzip_Zip_Variable("-eb");
        }

        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.ZipException))]
        public void Compat_Winzip_Zip_Enhanced()
        {
            // Unsupported compression method
            Compat_Winzip_Zip_Variable("-ee");
        }

        
        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.ZipException))]
        public void Compat_Winzip_Zip_LZMA()
        {
            // Unsupported compression method
            Compat_Winzip_Zip_Variable("-el");
        }
        

        
        public string Compat_Winzip_Zip_Variable(string options)
        {
            return Compat_Winzip_Zip_Variable(options, true);
        }
        
        public string Compat_Winzip_Zip_Variable(string options, bool wantVerify)
        {
            // options:
            // -sPassword 
            // -ep - PPMd compression.
            // -el - LZMA compression
            // -eb - bzip2 compression
            // -ee - "enhanced" compression.
            // -en - normal compression.
            // -ef - fast compression.
            // -es - superfast compression.
            // -ez - select best method at runtime. Requires WinZip12 to extract.
            // empty string = default
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_Winzip_Zip.zip");

            string dirInZip = "files";
            string subdir = Path.Combine(TopLevelDir, dirInZip);

            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            // delay between file creation and unzip
            System.Threading.Thread.Sleep(1200);
            
            // exec wzzip.exe to create the zip file
            string formatString = "-a -p " + options + " -yx {0} {1}\\*.*";
            string wzzipOut = this.Exec(wzzip, String.Format(formatString, zipFileToCreate, subdir));

            if (wantVerify)
            {
                string extractDir = "extract";
                // unzip with DotNetZip
                Directory.SetCurrentDirectory(TopLevelDir);
                using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
                {
                    zip1.ExtractAll(extractDir);
                }

                // check the files in the extract dir
                VerifyChecksums(extractDir, filesToZip, checksums);

                // verify the file times.
                VerifyNtfsTimes(extractDir, filesToZip);
            }

            return zipFileToCreate;
        }



        [TestMethod]
        public void Compat_Winzip_Unzip_2()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_Winzip_Unzip_2.zip");

            // create and fill the directories
            string extractDir = Path.Combine(TopLevelDir, "extract");
            string subdir = Path.Combine(TopLevelDir, "files");

            Dictionary<string, byte[]> checksums = new Dictionary<string, byte[]>();
            var filesToZip = GetSelectionOfTempFiles(_rnd.Next(13) + 8, checksums);

            // Create the zip archive
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddFiles(filesToZip, "files");
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Count,
                                 "Incorrect number of entries in the zip file.");

            // now, extract the zip
            // eg, wzunzip.exe -d test.zip  <extractdir>
            Directory.CreateDirectory(extractDir);
            Directory.SetCurrentDirectory(extractDir);
            this.Exec(wzunzip, String.Format("-d -yx {0}", zipFileToCreate));

            // check the files in the extract dir
            VerifyChecksums(Path.Combine(extractDir, "files"), filesToZip, checksums);

            // verify the file times
            VerifyDosTimes(Path.Combine(extractDir, "files"), filesToZip);
        }



        
        [TestMethod]
        public void Compat_Winzip_Unzip_ZeroLengthFile()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_Winzip_Unzip_ZeroLengthFile.zip");

            // create an empty file
            string filename = Path.Combine(TopLevelDir, Path.GetRandomFileName());
            using (StreamWriter sw = File.CreateText(filename)) { }
             
            // Create the zip archive
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddFile(filename, "");
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(1,TestUtilities.CountEntries(zipFileToCreate),
                                 "Incorrect number of entries in the zip file.");

            // now, test the zip
            // eg, wzunzip.exe -t test.zip  <extractdir>
            string wzunzipOut= this.Exec(wzunzip, String.Format("-t {0}", zipFileToCreate));

            TestContext.WriteLine("{0}", wzunzipOut);
            Assert.IsTrue(wzunzipOut.Contains("No errors"));
            Assert.IsFalse(wzunzipOut.Contains("At least one error was detected"));
        }


        
        [TestMethod]
        public void Compat_Winzip_Unzip_Password()
        {
            Directory.SetCurrentDirectory(TopLevelDir);
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_Winzip_Unzip_Password.zip");
            string extractDir = "extract";
            string subdir = "fodder";
            // create a bunch of files
            var filesToZip = TestUtilities.GenerateFilesFlat(subdir);
            string password=  Path.GetRandomFileName();
             
            // Create the zip archive
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Password = password;
                zip1.AddFiles(filesToZip, "");
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(filesToZip.Length,TestUtilities.CountEntries(zipFileToCreate),
                                 "Incorrect number of entries in the zip file.");

            // now, test the zip
            // eg, wzunzip.exe -t test.zip  <extractdir>
            string wzunzipOut= this.Exec(wzunzip, String.Format("-t -s{0} {1}", password, zipFileToCreate));
            TestContext.WriteLine("{0}", wzunzipOut);
            
            Assert.IsTrue(wzunzipOut.Contains("No errors"));
            Assert.IsFalse(wzunzipOut.Contains("At least one error was detected"));

            // extract the zip
            Directory.CreateDirectory(extractDir);
            // eg, wzunzip.exe -d -yx -sPassword  test.zip  <extractdir>
            wzunzipOut= this.Exec(wzunzip, String.Format("-d -yx -s{0} {1} {2}",
                                                                password, zipFileToCreate, extractDir));
            
            Assert.IsFalse(wzunzipOut.Contains("skipping"));
            Assert.IsFalse(wzunzipOut.Contains("incorrect"));
        }


        
        [TestMethod]
        public void Compat_Winzip_Unzip_Password_NonSeekableOutput()
        {
            // create a bunch of files
            Directory.SetCurrentDirectory(TopLevelDir);
            string subdir = "fodder";
            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);
            
            var compressionLevels = Enum.GetValues(typeof(Ionic.Zlib.CompressionLevel));
            int i=0;
            foreach (var compLevel in compressionLevels)
            {
                string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("Compat_Winzip_Unzip_Password_NonSeekableOutput.{0}.zip",i));
                string extractDir = "extract"+i.ToString();
                string password=  Path.GetRandomFileName();

                Directory.SetCurrentDirectory(TopLevelDir);
                // Want to test the library when saving to non-seekable output streams.  Like
                // stdout or ASPNET's Response.OutputStream.  This simulates it. 
                using (var rawOut = System.IO.File.Create(zipFileToCreate))
                {
                    using (var nonSeekableOut = new Ionic.Zip.Tests.NonSeekableOutputStream(rawOut))
                    {
                        // Create the zip archive
                        using (ZipFile zip1 = new ZipFile())
                        {
                            zip1.CompressionLevel = (Ionic.Zlib.CompressionLevel) compLevel;
                            zip1.Password = password;
                            zip1.AddFiles(filesToZip, "files");
                            zip1.Save(nonSeekableOut);
                        }
                    }
                }

                // Verify the number of files in the zip
                Assert.AreEqual<int>(filesToZip.Length,TestUtilities.CountEntries(zipFileToCreate),
                                     "Incorrect number of entries in the zip file.");

                // now, test the zip
                // eg, wzunzip.exe -t test.zip  
                string wzunzipOut= this.Exec(wzunzip, String.Format("-t -s{0} {1}", password, zipFileToCreate));
                TestContext.WriteLine("{0}", wzunzipOut);
            
                Assert.IsTrue(wzunzipOut.Contains("No errors"));
                Assert.IsFalse(wzunzipOut.Contains("At least one error was detected"));

                // extract the zip
                Directory.CreateDirectory(extractDir);
                // eg, wzunzip.exe -d -yx -sPassword  test.zip  <extractdir>
                wzunzipOut= this.Exec(wzunzip, String.Format("-d -yx -s{0} {1} {2}",
                                                             password, zipFileToCreate, extractDir));
                Assert.IsFalse(wzunzipOut.Contains("skipping"));
                Assert.IsFalse(wzunzipOut.Contains("incorrect"));

                // check the files in the extract dir
                VerifyChecksums(Path.Combine(extractDir, "files"), filesToZip, checksums);

                i++;
            }
        }

        

        [TestMethod]
        public void Compat_Winzip_Unzip_SFX()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_Winzip_Unzip_SFX.exe");

            // create and fill the directories
            string extractDir = Path.Combine(TopLevelDir, "extract");
            string subdir = Path.Combine(TopLevelDir, "files");

            Dictionary<string, byte[]> checksums = new Dictionary<string, byte[]>();
            var filesToZip = GetSelectionOfTempFiles(_rnd.Next(13) + 8, checksums);

            // Create the zip archive
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddFiles(filesToZip, "files");
                zip1.SaveSelfExtractor(zipFileToCreate, SelfExtractorFlavor.ConsoleApplication);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Count,
                                 "Incorrect number of entries in the zip file.");

            // now, extract the zip
            // eg, wzunzip.exe -d test.zip  <extractdir>
            Directory.CreateDirectory(extractDir);
            Directory.SetCurrentDirectory(extractDir);

            // -d = restore folder structure
            // -yx = restore extended timestamps to extracted files
            this.Exec(wzunzip, String.Format("-d -yx {0}", zipFileToCreate));

            // check the files in the extract dir
            VerifyChecksums(Path.Combine(extractDir, "files"), filesToZip, checksums);

            // verify the file times
            VerifyDosTimes(Path.Combine(extractDir, "files"), filesToZip);
        } 



        
        
        [TestMethod]
        public void Compat_Winzip_Unzip_Basic()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_Winzip_Unzip.zip");

            string dirInZip = "files";
            string extractDir = "extract";
            string subdir = Path.Combine(TopLevelDir, dirInZip);

            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            var additionalFiles = GetSelectionOfTempFiles(checksums);

            int i = 0;
            // set R and S attributes on the first file
            if (!File.Exists(filesToZip[i])) throw new Exception("Something is berry berry wrong.");
            File.SetAttributes(filesToZip[i], FileAttributes.ReadOnly | FileAttributes.System);

            // set H attribute on the second file
            i++;
            if (i == filesToZip.Length) throw new Exception("Not enough files??.");
            if (!File.Exists(filesToZip[i])) throw new Exception("Something is berry berry wrong.");
            File.SetAttributes(filesToZip[i], FileAttributes.Hidden);


            // Now, Create the zip archive with DotNetZip
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddFiles(filesToZip, dirInZip);
                zip1.AddFiles(additionalFiles, dirInZip);
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate),
                                 filesToZip.Length + additionalFiles.Count,
                                 "Incorrect number of entries in the zip file.");

            // examine and unpack the zip archive via WinZip
            // first, examine the zip entry metadata:
            string wzzipOut = this.Exec(wzzip, String.Format("-vt {0}", zipFileToCreate));

            string[] expectedAttrStrings = { "s-r-", "-hw-", "--w-" };

            // example: Filename: folder5\Test8.txt
            for (i = 0; i < expectedAttrStrings.Length; i++)
            {
                var f = Path.GetFileName(filesToZip[i]);
                var fileInZip = Path.Combine(dirInZip, f);
                string textToLookFor = String.Format("Filename: {0}", fileInZip.Replace("/", "\\"));
                int x = wzzipOut.IndexOf(textToLookFor);
                Assert.IsTrue(x > 0, "Could not find expected text ({0}) in WZZIP output.", textToLookFor);
                textToLookFor = "Attributes: ";
                x = wzzipOut.IndexOf(textToLookFor, x);
                string attrs = wzzipOut.Substring(x + textToLookFor.Length, 4);
                Assert.AreEqual(expectedAttrStrings[i], attrs, "Unexpected attributes on File {0}.", i);
            }


            // now, extract the zip
            // eg, wzunzip.exe -d test.zip  <extractdir>
            Directory.CreateDirectory(extractDir);
            Directory.SetCurrentDirectory(extractDir);
            this.Exec(wzunzip, String.Format("-d -yx {0}", zipFileToCreate));

            // check the files in the extract dir
            Directory.SetCurrentDirectory(TopLevelDir);
            String[] filesToCheck = new String[filesToZip.Length + additionalFiles.Count];
            filesToZip.CopyTo(filesToCheck, 0);
            additionalFiles.ToArray().CopyTo(filesToCheck, filesToZip.Length);

            VerifyChecksums(Path.Combine("extract", dirInZip), filesToCheck, checksums);

            // verify the file times
            DateTime atMidnight = new DateTime(DateTime.Now.Year,
                                               DateTime.Now.Month,
                                               DateTime.Now.Day);
            DateTime fortyFiveDaysAgo = atMidnight - new TimeSpan(45, 0, 0, 0);

            string[] extractedFiles = Directory.GetFiles(extractDir);

            foreach (var fqPath in extractedFiles)
            {
                string filename = Path.GetFileName(fqPath);
                DateTime stamp = File.GetLastWriteTime(fqPath);
                if (filename.StartsWith("testfile"))
                {
                    Assert.IsTrue((stamp == atMidnight || stamp == fortyFiveDaysAgo),
                                  "The timestamp on the file {0} is incorrect ({1}).",
                                  fqPath, stamp.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                else
                {
                    var orig = (from f in additionalFiles
                                where Path.GetFileName(f) == filename
                                select f)
                        .First();

                    DateTime t1 = File.GetLastWriteTime(filename);
                    DateTime t2 = File.GetLastWriteTime(orig);
                    Assert.AreEqual<DateTime>(t1, t2);
                    t1 = File.GetCreationTime(filename);
                    t2 = File.GetCreationTime(orig);
                    Assert.AreEqual<DateTime>(t1, t2);
                }

            }

        }

        private List<string> GetSelectionOfTempFiles(Dictionary<string, byte[]> checksums)
        {
            return GetSelectionOfTempFiles(_rnd.Next(23) + 9, checksums);
        }

        private List<string> GetSelectionOfTempFiles(int fileLimit, Dictionary<string, byte[]> checksums)
        {
            string tmpPath = Environment.GetEnvironmentVariable("TEMP");
            String[] candidates = Directory.GetFiles(tmpPath);
            var additionalFiles = new List<String>();
            var excludedFilenames = new List<string>();
            int trials = 0;
            int otherSide = 0;

            do
            {
                if (additionalFiles.Count > fileLimit && otherSide > 4) break;

                // randomly select a candidate
                var f = candidates[_rnd.Next(candidates.Length)];
                if (excludedFilenames.Contains(f)) continue;

                try
                {
                    var fi = new FileInfo(f);
                    if (Path.GetFileName(f)[0] == '~'
                        || additionalFiles.Contains(f)
                        || fi.Length > 10000000
                        || fi.Length < 100)
                    {
                        excludedFilenames.Add(f);
                    }
                    else
                    {
                        DateTime lastwrite = File.GetLastWriteTime(f);
                        bool onOtherSideOfDst =
                            (DateTime.Now.IsDaylightSavingTime() && !lastwrite.IsDaylightSavingTime())
                            ||

                            (!DateTime.Now.IsDaylightSavingTime() && lastwrite.IsDaylightSavingTime());

                        if (onOtherSideOfDst)
                        {
                            var key = Path.GetFileName(f);
                            var chk = TestUtilities.ComputeChecksum(f);
                            checksums.Add(key, chk);
                            additionalFiles.Add(f);
                            otherSide++;
                        }
                    }
                }
                catch { /* gulp */ }
                trials++;
            }
            while (trials < 1000);

            return additionalFiles;
        }


    }


}
