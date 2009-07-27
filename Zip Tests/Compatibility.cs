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
// Time-stamp: <2009-July-26 23:45:53>
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





        internal static void CreateFilesAndChecksums(string subdir, out string[] filesToZip, out Dictionary<string, byte[]> checksums)
        {
            // create a bunch of files
            filesToZip = TestUtilities.GenerateFilesFlat(subdir);
            DateTime atMidnight = new DateTime(DateTime.Now.Year,
                                               DateTime.Now.Month,
                                               DateTime.Now.Day);
            DateTime fortyFiveDaysAgo = atMidnight - new TimeSpan(45, 0, 0, 0);

            // get checksums for each one
            checksums = new Dictionary<string, byte[]>();
            //int count = 0;
            System.Random rnd = new System.Random();
            foreach (var f in filesToZip)
            {
                if (rnd.Next(3) == 0)
                    File.SetLastWriteTime(f, fortyFiveDaysAgo);
                else
                    File.SetLastWriteTime(f, atMidnight);

                var key = Path.GetFileName(f);
                var chk = TestUtilities.ComputeChecksum(f);
                checksums.Add(key, chk);
            }
        }


        
        private static void VerifyChecksums(string extractDir, 
            System.Collections.Generic.IEnumerable<String> filesToCheck, 
            Dictionary<string, byte[]> checksums)
        {
            int count= 0;
            foreach (var fqPath in filesToCheck)
            {
                var f = Path.GetFileName(fqPath);
                var extractedFile = Path.Combine(extractDir, f);
                Assert.IsTrue(File.Exists(extractedFile), "File does not exist ({0})", extractedFile);
                var chk = TestUtilities.ComputeChecksum(extractedFile);
                Assert.AreEqual<String>(TestUtilities.CheckSumToString(checksums[f]),
                                        TestUtilities.CheckSumToString(chk),
                                        String.Format("Checksums for file {0} do not match.", f));
                count++;
            }

            Assert.AreEqual<Int32>(count, checksums.Count, "Not all of the expected files were found in the extract directory.");
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
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);
            string notaZipFile = Path.Combine(testBin, "Resources\\VbsUnzip-ShellApp.vbs");

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

            // check existence of script and script engine
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);
            string script = Path.Combine(testBin, "Resources\\VbsUnzip-ShellApp.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);

            var compressionLevels = Enum.GetValues(typeof(Ionic.Zlib.CompressionLevel));
            int i=0;
            foreach (var compLevel in compressionLevels)
            {
                // cons up the directories
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
                this.Exec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
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

            // check existence of script and script engine
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);
            string script = Path.Combine(testBin, "Resources\\VbsUnzip-ShellApp.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);

            var compressionLevels = Enum.GetValues(typeof(Ionic.Zlib.CompressionLevel));
            int i=0;
            foreach (var compLevel in compressionLevels)
            {
                // cons up the directories
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
                this.Exec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
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
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

            // cons up the directories
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

            // run the unzip script
            string script = Path.Combine(testBin, "Resources\\VbsUnzip-ShellApp.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);


            this.Exec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
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
            string script = Path.Combine(testBin, "Resources\\VbsUnzip-ShellApp.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);


            this.Exec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
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
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

            string subdir = Path.Combine(TopLevelDir, "files");
            string extractDir = "extract";

            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            // Create the zip archive via script
            Directory.SetCurrentDirectory(TopLevelDir);
            string script = Path.Combine(testBin, "Resources\\VbsCreateZip-ShellApp.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);

            this.Exec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
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
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

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
            string script = Path.Combine(testBin, "Resources\\VbsCreateZip-ShellApp.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);

            this.Exec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
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
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

            string subdir = Path.Combine(TopLevelDir, "files");
            string extractDir = "extract";
            
            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            // run the COM script to create the ZIP archive
            string script = Path.Combine(testBin, "Resources\\VbsCreateZip-DotNetZip.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);

            this.Exec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
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
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

            // cons up the directories
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
            string script = Path.Combine(testBin, "Resources\\VbsUnzip-DotNetZip.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);

            this.Exec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
                      String.Format("\"{0}\" {1} {2}", script, zipFileToCreate, extractDir));


            // check the files in the extract dir
            VerifyChecksums(Path.Combine(extractDir, "files"), filesToZip, checksums);

            VerifyNtfsTimes(Path.Combine(extractDir, "files"), filesToZip);
        }




        [TestMethod]
        public void Compat_7z_Zip_1()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_7z_Zip_COM_Unzip.zip");
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

            Directory.SetCurrentDirectory(TopLevelDir);

            var sevenZip = Path.Combine(testBin, "Resources\\7z.exe");
            Assert.IsTrue(File.Exists(sevenZip), "exe ({0}) does not exist", sevenZip);

            // cons up the directories
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
            string script = Path.Combine(testBin, "Resources\\VbsUnZip-DotNetZip.vbs");
            Assert.IsTrue(File.Exists(script), "script ({0}) does not exist", script);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);

            this.Exec(Path.Combine(Path.Combine(w, "system32"), "cscript.exe"),
                      String.Format("\"{0}\" {1} {2}", script, zipFileToCreate, extractDir));

            // check the files in the extract dir
            VerifyChecksums(Path.Combine(extractDir, "files"), filesToZip, checksums);

            VerifyNtfsTimes(Path.Combine(extractDir, "files"), filesToZip);
            
        }



        [TestMethod]
        public void Compat_7z_Zip_2()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_7z_Zip_2.zip");
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

            // cons up the directories
            string extractDir = "extract";
            string subdir = Path.Combine(TopLevelDir, "files");

            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            // Create the zip archive via 7z.exe
            Directory.SetCurrentDirectory(TopLevelDir);
            string sevenZip = Path.Combine(testBin, "Resources\\7z.exe");
            Assert.IsTrue(File.Exists(sevenZip), "exe ({0}) does not exist", sevenZip);

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

            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

            // cons up the directories
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
            string sevenZip = Path.Combine(testBin, "Resources\\7z.exe");
            Assert.IsTrue(File.Exists(sevenZip), "exe ({0}) does not exist", sevenZip);
            this.Exec(sevenZip, String.Format("x {0}", zipFileToCreate));

            // check the files in the extract dir
            Directory.SetCurrentDirectory(TopLevelDir);

            VerifyChecksums(Path.Combine("extract", "files"), filesToZip, checksums);
        }


        [TestMethod]
        public void Compat_7z_Unzip_SFX()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_7z_Unzip_SFX.exe");

            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

            // cons up the directories
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
            string sevenZip = Path.Combine(testBin, "Resources\\7z.exe");
            Assert.IsTrue(File.Exists(sevenZip), "exe ({0}) does not exist", sevenZip);
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
        

        
        public void Compat_Winzip_Zip_Variable(string compressionString)
        {
            // compressionString:
            // -ep - PPMd compression.
            // -el - LZMA compression
            // -eb - bzip2 compression
            // -ee - "enhanced" compression.
            // -en - normal compression.
            // -ef - fast compression.
            // -es - superfast compression.
            // -ez - select best method at runtime. Requires WinZip12 to extract.
            // empty string = default
            string zipFileToCreate = Path.Combine(TopLevelDir,
                                                  String.Format("Compat_Winzip_Zip{0}.zip", compressionString));

            string testBin = TestUtilities.GetTestBinDir(CurrentDir);
            string dirInZip = "files";
            string extractDir = "extract";
            string subdir = Path.Combine(TopLevelDir, dirInZip);

            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            CreateFilesAndChecksums(subdir, out filesToZip, out checksums);

            // find the winzip command-line program
            var progfiles = System.Environment.GetEnvironmentVariable("ProgramFiles");
            string wzzip = Path.Combine(progfiles, "winzip\\wzzip.exe");
            Assert.IsTrue(File.Exists(wzzip), "exe ({0}) does not exist", wzzip);

            // delay between file creation and unzip
            System.Threading.Thread.Sleep(1200);
            
            // exec wzzip.exe to create the zip file
            string formatString = "-a -p " + compressionString + " -yx {0} {1}\\*.*";
            
            string wzzipOut = this.Exec(wzzip, String.Format(formatString, zipFileToCreate, subdir));

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



        [TestMethod]
        public void Compat_Winzip_Unzip_2()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_Winzip_Unzip_2.zip");
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

            // cons up the directories
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
            var progfiles = System.Environment.GetEnvironmentVariable("ProgramFiles");
            Directory.CreateDirectory(extractDir);
            Directory.SetCurrentDirectory(extractDir);
            string wzunzip = Path.Combine(progfiles, "winzip\\wzunzip.exe");
            Assert.IsTrue(File.Exists(wzunzip), "exe ({0}) does not exist", wzunzip);

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
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

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
            var progfiles = System.Environment.GetEnvironmentVariable("ProgramFiles");
            string wzunzip = Path.Combine(progfiles, "winzip\\wzunzip.exe");
            Assert.IsTrue(File.Exists(wzunzip), "exe ({0}) does not exist", wzunzip);

            string wzunzipOut= this.Exec(wzunzip, String.Format("-t {0}", zipFileToCreate));

            TestContext.WriteLine("{0}", wzunzipOut);
            Assert.IsTrue(wzunzipOut.Contains("No errors"));
            Assert.IsFalse(wzunzipOut.Contains("At least one error was detected"));
        }


        [TestMethod]
        public void Compat_Winzip_Unzip_SFX()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Compat_Winzip_Unzip_SFX.exe");
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);

            // cons up the directories
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
            var progfiles = System.Environment.GetEnvironmentVariable("ProgramFiles");
            Directory.CreateDirectory(extractDir);
            Directory.SetCurrentDirectory(extractDir);
            string wzunzip = Path.Combine(progfiles, "winzip\\wzunzip.exe");
            Assert.IsTrue(File.Exists(wzunzip), "exe ({0}) does not exist", wzunzip);

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

            string testBin = TestUtilities.GetTestBinDir(CurrentDir);
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
            var progfiles = System.Environment.GetEnvironmentVariable("ProgramFiles");
            string wzzip = Path.Combine(progfiles, "winzip\\wzzip.exe");
            Assert.IsTrue(File.Exists(wzzip), "exe ({0}) does not exist", wzzip);

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
            string wzunzip = Path.Combine(progfiles, "winzip\\wzunzip.exe");
            Assert.IsTrue(File.Exists(wzunzip), "exe ({0}) does not exist", wzunzip);

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
