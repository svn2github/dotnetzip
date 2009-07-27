// SelfExtractor.cs
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
// Time-stamp: <2009-July-26 23:55:44>
//
// ------------------------------------------------------------------
//
// This module defines the tests for the self-extracting archive capability
// within DotNetZip: creating, reading, updating, and running SFX's. 
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
    /// Summary description for Self extracting archives (SFX)
    /// </summary>
    [TestClass]
    public class SelfExtractor : IonicTestClass
    {
        public SelfExtractor() : base() { }

        [TestMethod]
        public void SelfExtractor_CanRead()
        {
            SelfExtractorFlavor[] trials = { SelfExtractorFlavor.ConsoleApplication, SelfExtractorFlavor.WinFormsApplication };
            for (int k = 0; k < trials.Length; k++)
            {
                string SfxFileToCreate = Path.Combine(TopLevelDir, String.Format("SelfExtractor_{0}.exe", trials[k].ToString()));
                string UnpackDirectory = Path.Combine(TopLevelDir, "unpack");
                if (Directory.Exists(UnpackDirectory))
                    Directory.Delete(UnpackDirectory, true);
                string ReadmeString = "Hey there!  This zipfile entry was created directly from a string in application code.";

                int entriesAdded = 0;
                String filename = null;

                string Subdir = Path.Combine(TopLevelDir, String.Format("A{0}", k));
                Directory.CreateDirectory(Subdir);
                var checksums = new Dictionary<string, string>();

                int fileCount = _rnd.Next(50) + 30;
                for (int j = 0; j < fileCount; j++)
                {
                    filename = Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                    entriesAdded++;
                    var chk = TestUtilities.ComputeChecksum(filename);
                    checksums.Add(filename.Replace(TopLevelDir + "\\", "").Replace('\\', '/'), TestUtilities.CheckSumToString(chk));
                }

                using (ZipFile zip1 = new ZipFile())
                {
                    zip1.AddDirectory(Subdir, Path.GetFileName(Subdir));
                    zip1.Comment = "This will be embedded into a self-extracting exe";
                    MemoryStream ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ReadmeString));
                    zip1.AddEntry("Readme.txt", "", ms1);
                    zip1.SaveSelfExtractor(SfxFileToCreate, trials[k]);
                }

                TestContext.WriteLine("---------------Reading {0}...", SfxFileToCreate);
                using (ZipFile zip2 = ZipFile.Read(SfxFileToCreate))
                {
                    //string extractDir = String.Format("extract{0}", j);
                    foreach (var e in zip2)
                    {
                        TestContext.WriteLine(" Entry: {0}  c({1})  u({2})", e.FileName, e.CompressedSize, e.UncompressedSize);
                        e.Extract(UnpackDirectory);
                        if (!e.IsDirectory)
                        {
                            if (checksums.ContainsKey(e.FileName))
                            {
                                filename = Path.Combine(UnpackDirectory, e.FileName);
                                string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filename));
                                Assert.AreEqual<string>(checksums[e.FileName], actualCheckString, "In trial {0}, Checksums for ({1}) do not match.", k, e.FileName);
                                //TestContext.WriteLine("     Checksums match ({0}).\n", actualCheckString);
                            }
                            else
                            {
                                Assert.AreEqual<string>("Readme.txt", e.FileName, String.Format("trial {0}", k));
                            }
                        }
                    }
                }
            }
        }



        [TestMethod]
        public void SelfExtractor_Update_Console()
        {
            SelfExtractor_Update(SelfExtractorFlavor.ConsoleApplication);
        }

        [TestMethod]
        public void SelfExtractor_Update_Winforms()
        {
            SelfExtractor_Update(SelfExtractorFlavor.WinFormsApplication);
        }

        private void SelfExtractor_Update(SelfExtractorFlavor flavor)
        {
            string SfxFileToCreate = Path.Combine(TopLevelDir,
                                                  String.Format("SelfExtractor_Update{0}.exe",
                                                                flavor.ToString()));
            string UnpackDirectory = Path.Combine(TopLevelDir, "unpack");
            if (Directory.Exists(UnpackDirectory))
                Directory.Delete(UnpackDirectory, true);

            string ReadmeString = "Hey there!  This zipfile entry was created directly from a string in application code.";

            // create a file and compute the checksum
            string Subdir = Path.Combine(TopLevelDir, "files");
            Directory.CreateDirectory(Subdir);
            var checksums = new Dictionary<string, string>();

            string filename = Path.Combine(Subdir, "file1.txt");
            TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
            var chk = TestUtilities.ComputeChecksum(filename);
            checksums.Add(filename.Replace(TopLevelDir + "\\", "").Replace('\\', '/'), TestUtilities.CheckSumToString(chk));

            // create the SFX
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddFile(filename, Path.GetFileName(Subdir));
                zip1.Comment = "This will be embedded into a self-extracting exe";
                MemoryStream ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ReadmeString));
                zip1.AddEntry("Readme.txt", "", ms1);
                zip1.SaveSelfExtractor(SfxFileToCreate, flavor, UnpackDirectory);
            }

            // verify count
            Assert.AreEqual<int>(TestUtilities.CountEntries(SfxFileToCreate), 2, "The Zip file has the wrong number of entries.");

            // create another file
            filename = Path.Combine(Subdir, "file2.txt");
            TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
            chk = TestUtilities.ComputeChecksum(filename);
            checksums.Add(filename.Replace(TopLevelDir + "\\", "").Replace('\\', '/'), TestUtilities.CheckSumToString(chk));
            string password = "ABCDEFG"; 
            // update the SFX
            using (ZipFile zip1 = ZipFile.Read(SfxFileToCreate))
            {
                zip1.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                zip1.Encryption = EncryptionAlgorithm.WinZipAes256;
                zip1.Comment = "The password is: " + password;
                zip1.Password = password;
                zip1.AddFile(filename, Path.GetFileName(Subdir));
                zip1.SaveSelfExtractor(SfxFileToCreate, flavor, UnpackDirectory);
            }

            // verify count
            Assert.AreEqual<int>(TestUtilities.CountEntries(SfxFileToCreate), 3, "The Zip file has the wrong number of entries.");


            // read the SFX
            TestContext.WriteLine("---------------Reading {0}...", SfxFileToCreate);
            using (ZipFile zip2 = ZipFile.Read(SfxFileToCreate))
            {
                zip2.Password = password;
                //string extractDir = String.Format("extract{0}", j);
                foreach (var e in zip2)
                {
                    TestContext.WriteLine(" Entry: {0}  c({1})  u({2})", e.FileName, e.CompressedSize, e.UncompressedSize);
                    e.Extract(UnpackDirectory);
                    if (!e.IsDirectory)
                    {
                        if (checksums.ContainsKey(e.FileName))
                        {
                            filename = Path.Combine(UnpackDirectory, e.FileName);
                            string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filename));
                            Assert.AreEqual<string>(checksums[e.FileName], actualCheckString, "Checksums for ({1}) do not match.", e.FileName);
                            //TestContext.WriteLine("     Checksums match ({0}).\n", actualCheckString);
                        }
                        else
                        {
                            Assert.AreEqual<string>("Readme.txt", e.FileName);
                        }
                    }
                }
            }

            int N = (flavor == SelfExtractorFlavor.ConsoleApplication) ? 2 : 1;
            for (int j = 0; j < N; j++)
            {
                // run the SFX
                TestContext.WriteLine("Running the SFX... ");
                var psi = new System.Diagnostics.ProcessStartInfo(SfxFileToCreate);
                if (flavor == SelfExtractorFlavor.ConsoleApplication)
                {
                    if (j == 0)
                        psi.Arguments = "-o -p " + password; // overwrite
                    else
                        psi.Arguments = "-p " + password;
                }
                psi.WorkingDirectory = TopLevelDir;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);
                process.WaitForExit();
                int rc = process.ExitCode;
                TestContext.WriteLine("SFX exit code: ({0})", rc);

                if (j == 0)
                {
                    Assert.AreEqual<Int32>(0, rc, "The exit code from the SFX was nonzero ({0}).", rc);
                }
                else
                {
                    Assert.AreNotEqual<Int32>(0, rc, "The exit code from the SFX was zero ({0}).");
                }
            }

            // verify the unpacked files?


        }



        [TestMethod]
        public void SelfExtractor_Console()
        {
            string ExeFileToCreate = Path.Combine(TopLevelDir, "SelfExtractor_Console.exe");
            string UnpackDirectory = Path.Combine(TopLevelDir, "unpack");
            string ReadmeString = "Hey there!  This zipfile entry was created directly from a string in application code.";

            int entriesAdded = 0;
            String filename = null;

            string Subdir = Path.Combine(TopLevelDir, "A");
            Directory.CreateDirectory(Subdir);
            var checksums = new Dictionary<string, string>();

            int fileCount = _rnd.Next(10) + 10;
            for (int j = 0; j < fileCount; j++)
            {
                filename = Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
                var chk = TestUtilities.ComputeChecksum(filename);
                checksums.Add(filename, TestUtilities.CheckSumToString(chk));
            }

            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(Subdir, Path.GetFileName(Subdir));
                zip.Comment = "This will be embedded into a self-extracting exe";
                MemoryStream ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ReadmeString));
                zip.AddEntry("Readme.txt", "", ms1);
                zip.SaveSelfExtractor(ExeFileToCreate, Ionic.Zip.SelfExtractorFlavor.ConsoleApplication,
                                      UnpackDirectory);
            }

            var psi = new System.Diagnostics.ProcessStartInfo(ExeFileToCreate);
            psi.WorkingDirectory = TopLevelDir;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);
            process.WaitForExit();

            // now, compare the output in UnpackDirectory with the original
            string DirToCheck = Path.Combine(UnpackDirectory, "A");
            // verify the checksum of each file matches with its brother
            foreach (string fname in Directory.GetFiles(DirToCheck))
            {
                string originalName = fname.Replace("\\unpack", "");
                if (checksums.ContainsKey(originalName))
                {
                    string expectedCheckString = checksums[originalName];
                    string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(fname));
                    Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", fname);
                }
                else
                    Assert.AreEqual<string>("Readme.txt", originalName);

            }
        }


        [TestMethod]
        public void SelfExtractor_WinForms()
        {
            string[] Passwords = { null, "12345" };
            for (int k = 0; k < Passwords.Length; k++)
            {
                string ExeFileToCreate = Path.Combine(TopLevelDir, String.Format("SelfExtractor_WinForms-{0}.exe", k));
                string DesiredUnpackDirectory = Path.Combine(TopLevelDir, String.Format("unpack{0}", k));

                String filename = null;

                string Subdir = Path.Combine(TopLevelDir, String.Format("A{0}", k));
                Directory.CreateDirectory(Subdir);
                var checksums = new Dictionary<string, string>();

                int fileCount = _rnd.Next(10) + 10;
                for (int j = 0; j < fileCount; j++)
                {
                    filename = Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                    var chk = TestUtilities.ComputeChecksum(filename);
                    checksums.Add(filename, TestUtilities.CheckSumToString(chk));
                }

                using (ZipFile zip = new ZipFile())
                {
                    zip.Password = Passwords[k];
                    zip.AddDirectory(Subdir, Path.GetFileName(Subdir));
                    zip.Comment = "For testing purposes, please extract to:  " + DesiredUnpackDirectory;
                    if (Passwords[k] != null) zip.Comment += String.Format("\r\n\r\nThe password for all entries is:  {0}\n", Passwords[k]);
                    zip.SaveSelfExtractor(ExeFileToCreate,
                                          Ionic.Zip.SelfExtractorFlavor.WinFormsApplication,
                                          DesiredUnpackDirectory);
                }

                // run the self-extracting EXE we just created 
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(ExeFileToCreate);
                psi.Arguments = DesiredUnpackDirectory;
                psi.WorkingDirectory = TopLevelDir;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);
                process.WaitForExit();

                // now, compare the output in TargetDirectory with the original
                string DirToCheck = Path.Combine(DesiredUnpackDirectory, String.Format("A{0}", k));
                // verify the checksum of each file matches with its brother
                var fileList = Directory.GetFiles(DirToCheck);
                Assert.AreEqual<Int32>(checksums.Keys.Count, fileList.Length, "Trial {0}: Inconsistent results.", k);

                foreach (string fname in fileList)
                {
                    string expectedCheckString = checksums[fname.Replace(String.Format("\\unpack{0}", k), "")];
                    string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(fname));
                    Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Trial {0}: Unexpected checksum on extracted filesystem file ({1}).", k, fname);
                }
            }
        }



        string programCode =

            "using System;\n" +
            "namespace Ionic.Tests.Zip.SelfExtractor\n" +
            "{\n" +
            "\n" +
            "    public class TestDriver\n" +
            "    {\n" +
            "        static int Main(String[] args)\n" +
            "        {\n" +
            "            int rc = @@XXX@@;\n" +
            "            Console.WriteLine(\"Hello from the post-extract command.\\nThis app will return {0}.\", rc);\n" +
            "            return rc;\n" +
            "        }\n" +
            "    }\n" +
            "}\n";


        private void CompileApp(int rc, string pathToExe)
        {
            Microsoft.CSharp.CSharpCodeProvider csharp = new Microsoft.CSharp.CSharpCodeProvider();

            //System.CodeDom.Compiler.ICodeCompiler csharpCompiler = csharp.CreateCompiler();

            var cp = new System.CodeDom.Compiler.CompilerParameters();
            cp.GenerateInMemory = false;
            cp.GenerateExecutable = true;
            cp.IncludeDebugInformation = false;
            cp.OutputAssembly = pathToExe;

            // set the return code in the app
            var cr = csharp.CompileAssemblyFromSource(cp, programCode.Replace("@@XXX@@", rc.ToString()));
            if (cr == null)
                throw new Exception("Errors compiling!");

            foreach (string s in cr.Output)
                TestContext.WriteLine(s);

            if (cr.Errors.Count != 0)
                throw new Exception("Errors compiling!");
        }


        [TestMethod]
        public void SelfExtractor_Console_RunOnExit()
        {
            string ExeFileToCreate = Path.Combine(TopLevelDir, "SelfExtractor_Console_RunOnExit.exe");
            string ReadmeString = "Hey there!  This zipfile entry was created directly from a string in application code.";

            TestContext.WriteLine("==============================");
            TestContext.WriteLine("SelfExtractor_Console_RunOnExit.exe");

            int entriesAdded = 0;
            String filename = null;
            string postExtractExe = String.Format("post-extract-run-on-exit-{0:D4}.exe", _rnd.Next(3000));
            int expectedReturnCode = _rnd.Next(1024) + 20;
            TestContext.WriteLine("The post-extract command ({0}) will return {1}", postExtractExe, expectedReturnCode);
            string Subdir = Path.Combine(TopLevelDir, "A");
            Directory.CreateDirectory(Subdir);
            var checksums = new Dictionary<string, string>();

            int fileCount = _rnd.Next(10) + 10;
            for (int j = 0; j < fileCount; j++)
            {
                filename = Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
                var chk = TestUtilities.ComputeChecksum(filename);
                checksums.Add(filename, TestUtilities.CheckSumToString(chk));
                TestContext.WriteLine("checksum({0})= ({1})", filename, checksums[filename]);
            }

            Directory.SetCurrentDirectory(TopLevelDir);
            for (int k = 0; k < 2; k++)
            {
                TestContext.WriteLine("----------------------");
                TestContext.WriteLine("Trial {0}", k);
                string UnpackDirectory = "unpack-" + k.ToString();

                if (k != 0)
                    CompileApp(expectedReturnCode, postExtractExe);

                var sw = new System.IO.StringWriter();
                using (ZipFile zip = new ZipFile())
                {
                    zip.StatusMessageTextWriter = sw;
                    zip.AddDirectory(Subdir, Path.GetFileName(Subdir));
                    zip.Comment = "This will be embedded into a self-extracting exe";
                    MemoryStream ms1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ReadmeString));
                    zip.AddEntry("Readme.txt", "", ms1);
                    if (k != 0) zip.AddFile(postExtractExe);

                    zip.SaveSelfExtractor(ExeFileToCreate,
                                          Ionic.Zip.SelfExtractorFlavor.ConsoleApplication,
                                          UnpackDirectory,
                                          postExtractExe);
                }

                TestContext.WriteLine("status output: " + sw.ToString());

                TestContext.WriteLine("Running the SFX... ");
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(ExeFileToCreate);
                psi.WorkingDirectory = TopLevelDir;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);
                process.WaitForExit();
                int rc = process.ExitCode;
                TestContext.WriteLine("SFX exit code: ({0})", rc);

                if (k != 0)
                    Assert.AreEqual<Int32>(expectedReturnCode, rc, "In trial {0}, the exit code did not match.", k);
                else
                    Assert.AreEqual<Int32>(5, rc, "In trial {0}, the exit code was unexpected.", k);

                // now, compare the output in UnpackDirectory with the original
                string DirToCheck = Path.Combine(TopLevelDir, Path.Combine(UnpackDirectory, "A"));
                // verify the checksum of each file matches with its brother
                foreach (string fname in Directory.GetFiles(DirToCheck))
                {
                    string originalName = fname.Replace("\\" + UnpackDirectory, "");
                    if (checksums.ContainsKey(originalName))
                    {
                        string expectedCheckString = checksums[originalName];
                        string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(fname));
                        Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", fname);
                    }
                    else
                        Assert.AreEqual<string>("Readme.txt", originalName);
                }
            }
        }



        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.BadStateException))]
        public void SelfExtractor_Save_Zip_As_EXE()
        {
            string SfxFileToCreate = Path.Combine(TopLevelDir, "SelfExtractor_Save_Zip_As_EXE.exe");
            
            Directory.SetCurrentDirectory(TopLevelDir);

            // create a file to zip
            string Subdir = Path.Combine(TopLevelDir, "files");
            Directory.CreateDirectory(Subdir);
            string filename = Path.Combine(Subdir, "file1.txt");
            TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);

            // add an entry to the zipfile, then try saving to a directory. this should fail
            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile(filename, "");
                zip.SaveSelfExtractor(SfxFileToCreate, SelfExtractorFlavor.ConsoleApplication);
            }

            // create another file
            filename = Path.Combine(Subdir, "file2.txt");
            TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);

            // update the SFX, save to a zip format
            using (ZipFile zip = new ZipFile(SfxFileToCreate))
            {
                zip.AddFile(filename, "");
                zip.Save();  // FAIL
            }

        }
        
    }
}