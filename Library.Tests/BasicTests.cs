using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RE = System.Text.RegularExpressions;

using Ionic.Utils.Zip;
using Library.TestUtilities;
using System.IO;

namespace Ionic.Utils.Zip.Tests.Basic
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class BasicTests
    {
        private System.Random _rnd;

        public BasicTests()
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
        public void CreateZip_AddItem()
        {
            int i;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddItem.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "files");

            // create a bunch of files
            string[] FilesToZip = TestUtilities.GenerateFilesFlat(Subdir);

            // Create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (i = 0; i < FilesToZip.Length; i++)
                    zip1.AddItem(FilesToZip[i], "files");
                zip1.Save();
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                    "Incorrect number of entries in the zip file.");
        }




        [TestMethod]
        public void CreateZip_AddFile()
        {
            int i;
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddFile.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "files");

            // create a bunch of files
            string[] FilesToZip = TestUtilities.GenerateFilesFlat(Subdir);

            // Create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (i = 0; i < FilesToZip.Length; i++)
                    zip1.AddFile(FilesToZip[i], "files");
                zip1.Save();
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                    "Incorrect number of entries in the zip file.");
        }


        [TestMethod]
        public void CreateZip_AddFile_LeadingDot()
        {
            int i;
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddFile_LeadingDot.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            // create a bunch of files
            string[] FilesToZip = TestUtilities.GenerateFilesFlat(".");

            // Create the zip archive
            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                for (i = 0; i < FilesToZip.Length; i++)
                {
                    zip1.AddFile(FilesToZip[i]);
                }
                zip1.Save();
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                    "Incorrect number of entries in the zip file.");
        }




        [TestMethod]
        public void CreateZip_AddFile_AddItem()
        {
            int i;
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddFile_AddItem.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "files");

            // create a bunch of files
            string[] FilesToZip = TestUtilities.GenerateFilesFlat(Subdir);

            // Create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                for (i = 0; i < FilesToZip.Length; i++)
                {
                    if (_rnd.Next(2) == 0)
                        zip1.AddFile(FilesToZip[i], "files");
                    else
                        zip1.AddItem(FilesToZip[i], "files");
                }
                zip1.Save();
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                    "Incorrect number of entries in the zip file.");
        }



        [TestMethod]
        public void CreateZip_NoEntries()
        {
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_Basic_NoEntries.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // Create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                zip1.Save();
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), 0,
                    "Incorrect number of entries in the zip file.");
        }



        [TestMethod]
        public void CreateZip_Basic_ParameterizedSave()
        {
            int i;
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_Basic_ParameterizedSave.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "files");
            System.IO.Directory.CreateDirectory(Subdir);

            // create a bunch of files
            int NumFilesToCreate = _rnd.Next(23) + 14;
            string[] FilesToZip = new string[NumFilesToCreate];
            for (i = 0; i < NumFilesToCreate; i++)
                FilesToZip[i] =
                    TestUtilities.CreateUniqueFile("bin", Subdir, _rnd.Next(10000) + 5000);

            // Create the zip archive
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (i = 0; i < FilesToZip.Length; i++)
                {
                    if (_rnd.Next(2) == 0)
                        zip1.AddFile(FilesToZip[i], "files");
                    else
                        zip1.AddItem(FilesToZip[i], "files");
                }
                zip1.Save(ZipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                    "Incorrect number of entries in the zip file.");
        }


        [TestMethod]
        public void CreateZip_AddFile_OnlyZeroLengthFiles()
        {
            _Internal_ZeroLengthFiles(_rnd.Next(3) + 3, "CreateZip_AddFile_OnlyZeroLengthFiles");
        }

        [TestMethod]
        public void CreateZip_AddFile_OneZeroLengthFile()
        {
            _Internal_ZeroLengthFiles(1, "CreateZip_AddFile_OneZeroLengthFile");
        }


        private void _Internal_ZeroLengthFiles(int fileCount, string nameStub)
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, nameStub + ".zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            int i;
            string[] FilesToZip = new string[fileCount];
            for (i = 0; i < fileCount; i++)
                FilesToZip[i] = TestUtilities.CreateUniqueFile("zerolength", TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (i = 0; i < FilesToZip.Length; i++)
                {
                    string pathToUse = System.IO.Path.Combine(System.IO.Path.GetFileName(TopLevelDir),
                        System.IO.Path.GetFileName(FilesToZip[i]));
                    zip.AddFile(pathToUse);
                }
                zip.Save();
            }

            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                    "The zip file created has the wrong number of entries.");
        }

        [TestMethod]
        public void CreateZip_UpdateDirectory()
        {
            int i, j;

            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_UpdateDirectory.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "zipthis");
            System.IO.Directory.CreateDirectory(DirToZip);

            TestContext.WriteLine("\n------------\nCreating files ...");

            int entries = 0;
            //int subdirCount = _rnd.Next(17) + 14;
            int subdirCount = _rnd.Next(3) + 2;
            var FileCount = new Dictionary<string, int>();
            var checksums = new Dictionary<string, byte[]>();

            for (i = 0; i < subdirCount; i++)
            {
                string SubdirShort = String.Format("dir{0:D4}", i);
                string Subdir = System.IO.Path.Combine(DirToZip, SubdirShort);
                System.IO.Directory.CreateDirectory(Subdir);

                //int filecount = _rnd.Next(11) + 17;
                int filecount = _rnd.Next(2) + 2;
                FileCount[SubdirShort] = filecount;
                for (j = 0; j < filecount; j++)
                {
                    string filename = String.Format("file{0:D4}.x", j);
                    string fqFilename = System.IO.Path.Combine(Subdir, filename);
                    TestUtilities.CreateAndFillFile(fqFilename, _rnd.Next(1000) + 100);

                    var chk = TestUtilities.ComputeChecksum(fqFilename);
                    var t1 = System.IO.Path.GetFileName(DirToZip);
                    var t2 = System.IO.Path.Combine(t1, SubdirShort);
                    var key = System.IO.Path.Combine(t2, filename);
                    key = TestUtilities.TrimVolumeAndSwapSlashes(key);
                    TestContext.WriteLine("chk[{0}]= {1}", key, TestUtilities.CheckSumToString(chk));
                    checksums.Add(key, chk);
                    entries++;
                }
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            TestContext.WriteLine("\n------------\nAdding files into the Zip...");

            // add all the subdirectories into a new zip
            var sw = new System.IO.StringWriter();
            using (ZipFile zip1 = new ZipFile(ZipFileToCreate, sw))
            {
                zip1.AddDirectory(DirToZip, "zipthis");
                //String[] dirs = System.IO.Directory.GetDirectories(DirToZip);
                //foreach (String d in dirs)
                //{
                //    string dir = System.IO.Path.Combine(System.IO.Path.GetFileName(DirToZip), System.IO.Path.GetFileName(d));
                //    zip1.AddDirectory(dir, System.IO.Path.Combine("zipthis", dir));
                //}
                zip1.Save();
            }
            TestContext.WriteLine(sw.ToString());

            TestContext.WriteLine("\n");

            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entries,
              "The Zip file has an unexpected number of entries.");

            TestContext.WriteLine("\n------------\nExtracting and validating checksums...");

            // validate all the checksums
            using (ZipFile zip2 = new ZipFile(ZipFileToCreate))
            {
                foreach (ZipEntry e in zip2)
                {
                    e.Extract("unpack");
                    string PathToExtractedFile = System.IO.Path.Combine("unpack", e.FileName);

                    // if it is a file.... 
                    if (checksums.ContainsKey(e.FileName))
                    {
                        // verify the checksum of the file is correct
                        string expectedCheckString = TestUtilities.CheckSumToString(checksums[e.FileName]);
                        string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(PathToExtractedFile));
                        Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", PathToExtractedFile);
                    }
                }
            }


            TestContext.WriteLine("\n------------\nCreating some new files ...");

            // now, update some of the existing files
            DirToZip = System.IO.Path.Combine(TopLevelDir, "updates");
            System.IO.Directory.CreateDirectory(DirToZip);

            for (i = 0; i < subdirCount; i++)
            {
                string SubdirShort = String.Format("dir{0:D4}", i);
                string Subdir = System.IO.Path.Combine(DirToZip, SubdirShort);
                System.IO.Directory.CreateDirectory(Subdir);

                int filecount = FileCount[SubdirShort];
                for (j = 0; j < filecount; j++)
                {
                    if (_rnd.Next(2) == 1)
                    {
                        string filename = String.Format("file{0:D4}.x", j);
                        TestUtilities.CreateAndFillFile(System.IO.Path.Combine(Subdir, filename),
                            _rnd.Next(1000) + 100);
                        string fqFilename = System.IO.Path.Combine(Subdir, filename);

                        var chk = TestUtilities.ComputeChecksum(fqFilename);
                        //var t1 = System.IO.Path.GetFileName(DirToZip);
                        var t2 = System.IO.Path.Combine("zipthis", SubdirShort);
                        var key = System.IO.Path.Combine(t2, filename);
                        key = TestUtilities.TrimVolumeAndSwapSlashes(key);

                        TestContext.WriteLine("chk[{0}]= {1}", key, TestUtilities.CheckSumToString(chk));

                        checksums.Remove(key);
                        checksums.Add(key, chk);
                    }
                }
            }


            TestContext.WriteLine("\n------------\nUpdating some of the files in the zip...");
            // add some new content
            using (ZipFile zip3 = new ZipFile(ZipFileToCreate))
            {
                zip3.UpdateDirectory(DirToZip, "zipthis");
                //String[] dirs = System.IO.Directory.GetDirectories(DirToZip);

                //foreach (String d in dirs)
                //{
                //    string dir = System.IO.Path.Combine(System.IO.Path.GetFileName(DirToZip), System.IO.Path.GetFileName(d));
                //    //string root = System.IO.Path.Combine("zipthis", System.IO.Path.GetFileName(d));
                //    zip3.UpdateDirectory(dir, "zipthis");
                //}
                zip3.Save();
            }

            TestContext.WriteLine("\n------------\nValidating the checksums for all of the files ...");

            // validate all the checksums again
            using (ZipFile zip4 = new ZipFile(ZipFileToCreate))
            {
                foreach (ZipEntry e in zip4)
                    TestContext.WriteLine("Found entry: {0}", e.FileName);

                foreach (ZipEntry e in zip4)
                {
                    e.Extract("unpack2");
                    if (!e.IsDirectory)
                    {
                        string PathToExtractedFile = System.IO.Path.Combine("unpack2", e.FileName);

                        // verify the checksum of the file is correct
                        string expectedCheckString = TestUtilities.CheckSumToString(checksums[e.FileName]);
                        string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(PathToExtractedFile));
                        Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", PathToExtractedFile);
                    }
                }
            }
        }



        [TestMethod]
        public void CreateZip_AddDirectory_LargeNumberOfSmallFiles()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddDirectory_LargeNumberOfSmallFiles.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "zipthis");
            System.IO.Directory.CreateDirectory(DirToZip);

            int[] settings = { 71, 21, 97, 27 };
            int subdirCount = 0;
            int entries = TestUtilities.GenerateFilesOneLevelDeep(TestContext, "LargeNumberOfFiles", DirToZip, settings, out subdirCount);

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(System.IO.Path.GetFileName(DirToZip));
                zip.Save();
            }

            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entries,
                    "The zip file created has the wrong number of entries.");

        }




        [TestMethod]
        public void CreateZip_AddDirectory_OnlyZeroLengthFiles()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddDirectory_OnlyZeroLengthFiles.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "zipthis");
            System.IO.Directory.CreateDirectory(DirToZip);

            int entries = 0;
            int subdirCount = _rnd.Next(8) + 8;
            for (int i = 0; i < subdirCount; i++)
            {
                string SubDir = System.IO.Path.Combine(DirToZip, "dir" + i);
                System.IO.Directory.CreateDirectory(SubDir);

                // one empty file per subdir
                string file = TestUtilities.CreateUniqueFile("bin", SubDir);
                entries++;
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(System.IO.Path.GetFileName(DirToZip));
                zip.Save();
            }

            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entries,
                    "The zip file created has the wrong number of entries.");
        }



        [TestMethod]
        public void CreateZip_AddDirectory_OneZeroLengthFile()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddDirectory_OneZeroLengthFile.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "zipthis");
            System.IO.Directory.CreateDirectory(DirToZip);

            // one empty file
            string file = TestUtilities.CreateUniqueFile("ZeroLengthFile.txt", DirToZip);

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(System.IO.Path.GetFileName(DirToZip));
                zip.Save();
            }

            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), 1,
                    "The zip file created has the wrong number of entries.");
        }


        [TestMethod]
        public void CreateZip_AddDirectory_OnlyEmptyDirectories()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddDirectory_OnlyEmptyDirectories.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "zipthis");
            System.IO.Directory.CreateDirectory(DirToZip);

            int entries = 0;
            int subdirCount = _rnd.Next(8) + 8;
            for (int i = 0; i < subdirCount; i++)
            {
                string SubDir = System.IO.Path.Combine(DirToZip, "EmptyDir" + i);
                System.IO.Directory.CreateDirectory(SubDir);
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(System.IO.Path.GetFileName(DirToZip));
                zip.Save();
            }

            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entries,
                    "The zip file created has the wrong number of entries.");
        }

        [TestMethod]
        public void CreateZip_AddDirectory_OneEmptyDirectory()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddDirectory_OneEmptyDirectory.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "zipthis");
            System.IO.Directory.CreateDirectory(DirToZip);

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(System.IO.Path.GetFileName(DirToZip));
                zip.Save();
            }

            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), 0,
                    "The zip file created has the wrong number of entries.");
        }


        [TestMethod]
        public void CreateZip_AddDirectory_CheckStatusTextWriter()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddDirectory_CheckStatusTextWriter.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "zipthis");
            System.IO.Directory.CreateDirectory(DirToZip);

            int entries = 0;
            int subdirCount = _rnd.Next(8) + 8;
            for (int i = 0; i < subdirCount; i++)
            {
                string SubDir = System.IO.Path.Combine(DirToZip, "Dir" + i);
                System.IO.Directory.CreateDirectory(SubDir);
                // a few files per subdir
                int fileCount = _rnd.Next(12) + 4;
                for (int j = 0; j < fileCount; j++)
                {
                    string file = System.IO.Path.Combine(SubDir, "File" + j);
                    TestUtilities.CreateAndFillFile(file, 100);
                    entries++;
                }
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            var sw = new System.IO.StringWriter();
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.StatusMessageTextWriter = sw;
                zip.AddDirectory(System.IO.Path.GetFileName(DirToZip));
                zip.Save();
            }

            string status = sw.ToString();

            TestContext.WriteLine("status output: " + status);

            Assert.IsTrue(status.Length > 24 * entries, "Insufficient status messages on the StatusTexWriter?");

            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entries,
                    "The zip file created has the wrong number of entries.");
        }


        struct TestTrial
        {
            public string arg;
            public string re;
        }


        [TestMethod]
        public void CreateZip_AddDirectory()
        {
            TestTrial[] trials = { 
                               new TestTrial { arg=null, re="^file(\\d+).ext$"},
                               new TestTrial { arg="", re="^file(\\d+).ext$"},
                               new TestTrial { arg=null, re="^file(\\d+).ext$"},
                               new TestTrial { arg="Xabf", re="(?s)^Xabf/(file(\\d+).ext)?$"},
                               new TestTrial { arg="AAAA/BBB", re="(?s)^AAAA/BBB/(file(\\d+).ext)?$"}
                               };

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            for (int k = 0; k < trials.Length; k++)
            {
                TestContext.WriteLine("\n--------------------------------\n\n\n");
                string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("CreateZip_AddDirectory-{0}.zip", k));

                Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

                string dirToZip = String.Format("DirectoryToZip.{0}.test", k);
                System.IO.Directory.CreateDirectory(dirToZip);

                int fileCount = _rnd.Next(5) + 4;
                for (int i = 0; i < fileCount; i++)
                {
                    String file = System.IO.Path.Combine(dirToZip, String.Format("file{0:D3}.ext", i));
                    TestUtilities.CreateAndFillFile(file, _rnd.Next(2000) + 500);
                }

                var sw = new System.IO.StringWriter();
                using (ZipFile zip = new ZipFile(ZipFileToCreate, sw))
                {
                    if (k == 0)
                        zip.AddDirectory(dirToZip);
                    else
                        zip.AddDirectory(dirToZip, trials[k].arg);
                    zip.Save();
                }
                TestContext.WriteLine(sw.ToString());
                Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), fileCount,
                        String.Format("The zip file created in cycle {0} has the wrong number of entries.", k));

                //TestContext.WriteLine("");
                // verify that the entries in the zip are in the top level directory!!
                using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
                {
                    foreach (ZipEntry e in zip2)
                        TestContext.WriteLine("found entry: {0}", e.FileName);
                    foreach (ZipEntry e in zip2)
                    {
                        //Assert.IsFalse(e.FileName.StartsWith("dir"),
                        //       String.Format("The Zip entry '{0}' is not rooted in top level directory.", e.FileName));

                        // check the filename: 
                        //RE.Match m0 = RE.Regex.Match(e.FileName, fnameRegex[k]);
                        // Assert.IsTrue(m0 != null, "No match");
                        // Assert.AreEqual<int>(m0.Groups.Count, 2,
                        //    String.Format("In cycle {0}, Matching {1} against {2}, Wrong number of matches ({3})",
                        //        k, e.FileName, fnameRegex[k], m0.Groups.Count));

                        Assert.IsTrue(RE.Regex.IsMatch(e.FileName, trials[k].re),
                            String.Format("In cycle {0}, Matching {1} against {2}", k, e.FileName, trials[k].re));
                    }
                }
            }
        }


        [TestMethod]
        public void CreateZip_AddDirectory_Nested()
        {
            TestTrial[] trials = { 
                               new TestTrial { arg=null, re="^dir(\\d){3}/(file(\\d+).ext)?$"},
                               new TestTrial { arg="", re="^dir(\\d){3}/(file(\\d+).ext)?$"},
                               new TestTrial { arg=null, re="^dir(\\d){3}/(file(\\d+).ext)?$"},
                               new TestTrial { arg="rtdha", re="(?s)^rtdha/(dir(\\d){3}/(file(\\d+).ext)?)?$"},
                               new TestTrial { arg="sdfjk/BBB", re="(?s)^sdfjk/BBB/(dir(\\d){3}/(file(\\d+).ext)?)?$"}
                               };

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            for (int k = 0; k < trials.Length; k++)
            {
                TestContext.WriteLine("\n--------------------------------\n\n\n");
                string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("CreateZip_AddDirectory_Nested-{0}.zip", k));

                Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

                string dirToZip = String.Format("DirectoryToZip.{0}.test", k);
                System.IO.Directory.CreateDirectory(dirToZip);

                int i, j;
                int entries = 0;

                int subdirCount = _rnd.Next(3) + 2;
                for (i = 0; i < subdirCount; i++)
                {
                    string Subdir = System.IO.Path.Combine(dirToZip, String.Format("dir{0:D3}", i));
                    System.IO.Directory.CreateDirectory(Subdir);

                    int fileCount = _rnd.Next(4);  // sometimes zero
                    for (j = 0; j < fileCount; j++)
                    {
                        String file = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.ext", j));
                        TestUtilities.CreateAndFillFile(file, _rnd.Next(750) + 500);
                        entries++;
                    }
                }


                //string dirToZip = System.IO.Path.GetFileName(TopLevelDir);
                var sw = new System.IO.StringWriter();
                using (ZipFile zip = new ZipFile(ZipFileToCreate, sw))
                {
                    if (k == 0)
                        zip.AddDirectory(dirToZip);
                    else
                        zip.AddDirectory(dirToZip, trials[k].arg);
                    zip.Save();
                }
                TestContext.WriteLine(sw.ToString());

                Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entries,
                        String.Format("The zip file created in cycle {0} has the wrong number of entries.", k));

                //TestContext.WriteLine("");
                // verify that the entries in the zip are in the top level directory!!
                using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
                {
                    foreach (ZipEntry e in zip2)
                        TestContext.WriteLine("found entry: {0}", e.FileName);
                    foreach (ZipEntry e in zip2)
                    {
                        Assert.IsTrue(RE.Regex.IsMatch(e.FileName, trials[k].re),
                            String.Format("In cycle {0}, Matching {1} against {2}", k, e.FileName, trials[k].re));
                    }

                }
            }
        }



        [TestMethod]
        public void CreateZip_VerifyThatStreamRemainsOpenAfterSave()
        {
            bool[] ForceCompressionOptions = { true, false };
            string[] Passwords = { null, System.IO.Path.GetRandomFileName() };

            for (int j = 0; j < Passwords.Length; j++)
            {
                for (int k = 0; k < ForceCompressionOptions.Length; k++)
                {
                    TestContext.WriteLine("\n\n---------------------------------\nTrial ({0},{1}):  Password='{2}' Compression={3}\n", j, k, Passwords[j], ForceCompressionOptions[k]);
                    System.IO.Directory.SetCurrentDirectory(TopLevelDir);
                    string dirToZip = System.IO.Path.GetRandomFileName();
                    System.IO.Directory.CreateDirectory(dirToZip);

                    int filesAdded = _rnd.Next(3) + 3;
                    for (int i = 0; i < filesAdded; i++)
                    {
                        var s = System.IO.Path.Combine(dirToZip, String.Format("tempfile-{0}-{1}-{2}.bin", j, k, i));
                        int sz = _rnd.Next(10000) + 5000;
                        TestContext.WriteLine("  Creating file: {0} sz({1})", s, sz);
                        TestUtilities.CreateAndFillFileBinary(s, sz);
                    }

                    TestContext.WriteLine("\n");

                    //string dirToZip = System.IO.Path.GetFileName(TopLevelDir);
                    var ms = new System.IO.MemoryStream();
                    Assert.IsTrue(ms.CanSeek, String.Format("Trial {0}: The output MemoryStream does not do Seek.", k));
                    using (ZipFile zip1 = new ZipFile(ms))
                    {
                        zip1.ForceNoCompression = ForceCompressionOptions[k];
                        zip1.Password = Passwords[j];
                        zip1.Comment = String.Format("Trial ({0},{1}):  Password='{2}' Compression={3}\n", j, k, Passwords[j], ForceCompressionOptions[k]);
                        zip1.AddDirectory(dirToZip);
                        zip1.Save();
                    }

                    Assert.IsTrue(ms.CanSeek, String.Format("Trial {0}: After writing, the OutputStream does not do Seek.", k));
                    Assert.IsTrue(ms.CanRead, String.Format("Trial {0}: The OutputStream cannot be Read.", k));

                    // seek to the beginning
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    int filesFound = 0;
                    using (ZipFile zip2 = ZipFile.Read(ms))
                    {
                        foreach (ZipEntry e in zip2)
                        {
                            TestContext.WriteLine("  Found entry: {0} isDir({1}) sz_c({2}) sz_unc({3})", e.FileName, e.IsDirectory, e.CompressedSize, e.UncompressedSize);
                            if (!e.IsDirectory)
                                filesFound++;
                        }
                    }
                    Assert.AreEqual<int>(filesFound, filesAdded, String.Format("Trial {0}, Found an incorrect number of files.", k));
                }
            }
        }


        [TestMethod]
        public void CreateZip_AddFile_VerifyCrcAndContents()
        {
            string filename = null;
            int entriesAdded = 0;
            string repeatedLine = null;
            int j;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddFile_VerifyCrcAndContents.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int NumFilesToCreate = _rnd.Next(10) + 8;
            for (j = 0; j < NumFilesToCreate; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                    System.IO.Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Create the zip file
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "UpdateTests::CreateZip_AddFile_VerifyCrcAndContents(): This archive will be updated.";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entriesAdded,
                "The Zip file has the wrong number of entries.");

            // now extract the files and verify their contents
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in zip2.EntryFileNames)
                {
                    repeatedLine = String.Format("This line is repeated over and over and over in file {0}", s);
                    zip2[s].Extract("extract");

                    // verify the content of the updated file. 
                    var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", s));
                    string actualLine = sr.ReadLine();
                    sr.Close();

                    Assert.AreEqual<string>(repeatedLine, actualLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }
        }


        [TestMethod]
        public void CreateZip_WithEmptyDirectory()
        {
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Create_WithEmptyDirectory.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "EmptyDirectory");
            System.IO.Directory.CreateDirectory(Subdir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(Subdir, "");
                zip.Save();
            }

            // Verify the files are in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), 0,
                "The Zip file has the wrong number of entries.");

        }



        [TestMethod]
        public void Extract_IntoMemoryStream()
        {
            string filename = null;
            int entriesAdded = 0;
            string repeatedLine = null;
            int j;

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Extract_IntoMemoryStream.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int NumFilesToCreate = _rnd.Next(10) + 8;
            for (j = 0; j < NumFilesToCreate; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                    System.IO.Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Create the zip file
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "BasicTests::Extract_IntoMemoryStream()";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entriesAdded,
                "The Zip file has the wrong number of entries.");

            // now extract the files into memory streams, checking only the length of the file.
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string s in zip2.EntryFileNames)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        zip2[s].Extract(ms);
                        byte[] a = ms.ToArray();
                        string f = System.IO.Path.Combine(Subdir, s);
                        var fi = new System.IO.FileInfo(f);
                        Assert.AreEqual<int>((int)(fi.Length), a.Length, "Unequal file lengths.");
                    }
                }
            }
        }


        [TestMethod]
        public void Retrieve_ViaIndexer()
        {
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Retrieve_ViaIndexer.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string filename = null;
            int entriesAdded = 0;
            string repeatedLine = null;
            int j;

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int NumFilesToCreate = _rnd.Next(10) + 8;
            for (j = 0; j < NumFilesToCreate; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("File{0:D3}.txt", j));
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                    System.IO.Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(23000) + 4000);
                entriesAdded++;
            }

            // Create the zip file
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "BasicTests::Retrieve_ViaIndexer()";
                zip1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entriesAdded,
                "The Zip file has the wrong number of entries.");

            // now extract the files into memory streams, checking only the length of the file.
            // We do 4 combinations:  case-sensitive on or off, and filename conversion on or off.
            for (int m = 0; m < 2; m++)
            {
                for (int n = 0; n < 2; n++)
                {
                    using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
                    {
                        if (n == 1) zip2.CaseSensitiveRetrieval = true;
                        foreach (string s in zip2.EntryFileNames)
                        {
                            var s2 = (m == 1) ? s.ToUpper() : s;
                            using (MemoryStream ms = new MemoryStream())
                            {
                                try
                                {
                                    zip2[s2].Extract(ms);
                                    byte[] a = ms.ToArray();
                                    string f = System.IO.Path.Combine(Subdir, s2);
                                    var fi = new System.IO.FileInfo(f);
                                    Assert.AreEqual<int>((int)(fi.Length), a.Length, "Unequal file lengths.");
                                }
                                catch
                                {
                                    Assert.AreEqual<int>(1, n * m, "Indexer retrieval failed unexpectedly.");
                                }
                            }
                        }
                    }
                }
            }
        }




        [TestMethod]
        public void CreateZip_SetFileComments()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "FileComments.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string FileCommentFormat = "Comment Added By Test to file '{0}'";
            string CommentOnArchive = "Comment added by FileComments() method.";


            int fileCount = _rnd.Next(3) + 3;
            string[] FilesToZip = new string[fileCount];
            for (int i = 0; i < fileCount; i++)
            {
                FilesToZip[i] = System.IO.Path.Combine(TopLevelDir, String.Format("file{0:D3}.bin", i));
                TestUtilities.CreateAndFillFile(FilesToZip[i], _rnd.Next(10000) + 5000);
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (int i = 0; i < FilesToZip.Length; i++)
                {
                    // use the local filename (not fully qualified)
                    ZipEntry e = zip.AddFile(System.IO.Path.GetFileName(FilesToZip[i]));
                    e.Comment = String.Format(FileCommentFormat, e.FileName);
                }
                zip.Comment = CommentOnArchive;
                zip.Save();
            }

            int entries = 0;
            using (ZipFile z2 = ZipFile.Read(ZipFileToCreate))
            {
                Assert.AreEqual<String>(CommentOnArchive, z2.Comment, "Unexpected comment on ZipFile.");
                foreach (ZipEntry e in z2)
                {
                    string expectedComment = String.Format(FileCommentFormat, e.FileName);
                    Assert.AreEqual<string>(expectedComment, e.Comment, "Unexpected comment on ZipEntry.");
                    entries++;
                }
            }
            Assert.AreEqual<int>(entries, FilesToZip.Length, "Unexpected file count. Expected {0}, got {1}.",
                    FilesToZip.Length, entries);
        }



        [TestMethod]
        public void CreateZip_SetFileLastModified()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_SetFileLastModified.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            int fileCount = _rnd.Next(13) + 23;
            string[] FilesToZip = new string[fileCount];
            for (int i = 0; i < fileCount; i++)
            {
                FilesToZip[i] = System.IO.Path.Combine(TopLevelDir, String.Format("file{0:D3}.bin", i));
                TestUtilities.CreateAndFillFileBinary(FilesToZip[i], _rnd.Next(10000) + 5000);
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            var Timestamp = new System.DateTime(2007, 9, 1, 15, 0, 0);
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (int i = 0; i < FilesToZip.Length; i++)
                {
                    // use the local filename (not fully qualified)
                    ZipEntry e = zip.AddFile(System.IO.Path.GetFileName(FilesToZip[i]));
                    e.LastModified = Timestamp;
                }
                zip.Comment = "All files in this archive have the same timestamp.";
                zip.Save();
            }

            int entries = 0;
            using (ZipFile z2 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (ZipEntry e in z2)
                {
                    Assert.AreEqual<DateTime>(Timestamp, e.LastModified, "Unexpected timestamp on ZipEntry.");
                    entries++;
                    // now verify that the LastMod time on the filesystem file is set correctly
                    e.Extract("unpack");
                    DateTime ActualFilesystemLastMod = System.IO.File.GetLastWriteTime(System.IO.Path.Combine("unpack", e.FileName));
                    Assert.AreEqual<DateTime>(Timestamp, ActualFilesystemLastMod, "Unexpected timestamp on extracted filesystem file.");

                }
            }
            Assert.AreEqual<int>(entries, FilesToZip.Length, "Unexpected file count. Expected {0}, got {1}.",
                    FilesToZip.Length, entries);
        }


        [TestMethod]
        public void CreateZip_VerifyFileLastModified()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_VerifyFileLastModified.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            String[] PotentialFilenames = System.IO.Directory.GetFiles(System.Environment.GetEnvironmentVariable("TEMP"));
            var checksums = new Dictionary<string, byte[]>();
            var timestamps = new Dictionary<string, DateTime>();
            var ActualFilenames = new List<string>();
            var ExcludedFilenames = new List<string>();

            int maxFiles = _rnd.Next(PotentialFilenames.Length / 2) + PotentialFilenames.Length / 3;
            maxFiles = Math.Min(maxFiles, 145);
            TestContext.WriteLine("\n-----------------------------\r\n{1}: Finding files in '{0}'...", 
                System.Environment.GetEnvironmentVariable("TEMP"),
                DateTime.Now.ToString("HH:mm:ss"));
            do
            {
                string filename = null;
                bool foundOne = false;
                while (!foundOne)
                {
                    filename = PotentialFilenames[_rnd.Next(PotentialFilenames.Length)];
                    if (ExcludedFilenames.Contains(filename)) continue;
                    var fi = new System.IO.FileInfo(filename);

                    if (System.IO.Path.GetFileName(filename)[0] == '~'
                            || ActualFilenames.Contains(filename)
                        || fi.Length > 10000000
                        // there are some weird files on my system that cause this test to fail!
                        // the GetLastWrite() method returns the "wrong" time - does not agree with
                        // what is shown in Explorer or in a cmd.exe dir output.  So I exclude those 
                        // files here.
                        //|| filename.EndsWith(".cer")
                        //|| filename.EndsWith(".msrcincident")
                        //|| filename == "MSCERTS.ini"
                        )
                    {
                        ExcludedFilenames.Add(filename);
                    }
                    else
                    {
                        foundOne = true;
                    }
                }

                var key = System.IO.Path.GetFileName(filename);

                // surround this in a try...catch so as to avoid grabbing a file that is open by someone else, or has disappeared
                try
                {
                    var lastWrite = System.IO.File.GetLastWriteTime(filename);
                    var fi = new System.IO.FileInfo(filename);
                    var tm = TestUtilities.RoundToEvenSecond(lastWrite);
                    // hop out of the try block if the file is from TODAY.  (heuristic to avoid currently open files)
                    if ((tm.Year == DateTime.Now.Year) && (tm.Month == DateTime.Now.Month) && (tm.Day == DateTime.Now.Day))
                        throw new Exception();
                    var chk = TestUtilities.ComputeChecksum(filename);
                    checksums.Add(key, chk);
                    TestContext.WriteLine("  {4}:  {1}  {2}  {3,-9}  {0}",
                        System.IO.Path.GetFileName(filename),
                        lastWrite.ToString("yyyy MMM dd HH:mm:ss"),
                        tm.ToString("yyyy MMM dd HH:mm:ss"),
                        fi.Length,
                        DateTime.Now.ToString("HH:mm:ss"));
                    timestamps.Add(key, tm);
                    ActualFilenames.Add(filename);
                }
                catch
                {
                    ExcludedFilenames.Add(filename);
                }
            } while ((ActualFilenames.Count < maxFiles) && (ActualFilenames.Count < PotentialFilenames.Length) &&
                ActualFilenames.Count + ExcludedFilenames.Count < PotentialFilenames.Length);

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            TestContext.WriteLine("{0}: Creating zip...", DateTime.Now.ToString("HH:mm:ss"));

            // create the zip file
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                foreach (string s in ActualFilenames)
                {
                    ZipEntry e = zip.AddFile(s, "");
                    e.Comment = System.IO.File.GetLastWriteTime(s).ToString("yyyyMMMdd HH:mm:ss");
                }
                zip.Comment = "The files in this archive will be checked for LastMod timestamp and checksum.";
                TestContext.WriteLine("{0}: Saving zip....", DateTime.Now.ToString("HH:mm:ss"));
                zip.Save();
            }

            TestContext.WriteLine("{0}: Unpacking zip....", DateTime.Now.ToString("HH:mm:ss"));

            // unpack the zip, and verify contents
            int entries = 0;
            using (ZipFile z2 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (ZipEntry e in z2)
                {
                    TestContext.WriteLine("{0}: Checking entry {1}....", DateTime.Now.ToString("HH:mm:ss"), e.FileName);
                    entries++;
                    // verify that the LastMod time on the filesystem file is set correctly
                    e.Extract("unpack");
                    string PathToExtractedFile = System.IO.Path.Combine("unpack", e.FileName);
                    DateTime ActualFilesystemLastMod = System.IO.File.GetLastWriteTime(PathToExtractedFile);
                    Assert.AreEqual<DateTime>(timestamps[e.FileName], ActualFilesystemLastMod,
                        "Unexpected timestamp on extracted filesystem file ({0}).", PathToExtractedFile);

                    // verify the checksum of the file is correct
                    string expectedCheckString = TestUtilities.CheckSumToString(checksums[e.FileName]);
                    string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(PathToExtractedFile));
                    Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", PathToExtractedFile);
                }
            }
            Assert.AreEqual<int>(entries, ActualFilenames.Count, "Unexpected file count.");
        }



        [TestMethod]
        public void CreateZip_AddDirectory_NoFilesInRoot()
        {
            int i, j;
            int entries = 0;

            int subdirCount = _rnd.Next(4) + 4;
            for (i = 0; i < subdirCount; i++)
            {
                string Subdir = System.IO.Path.Combine(TopLevelDir, "DirectoryToZip.test." + i);
                System.IO.Directory.CreateDirectory(Subdir);

                int fileCount = _rnd.Next(3) + 3;
                for (j = 0; j < fileCount; j++)
                {
                    String file = System.IO.Path.Combine(Subdir, "file" + j);
                    TestUtilities.CreateAndFillFile(file, _rnd.Next(100) + 500);
                    entries++;
                }
            }

            string ZipFileToCreate = TestUtilities.GenerateUniquePathname("zip");
            _FilesToRemove.Add(ZipFileToCreate);

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string RelativeDir = System.IO.Path.GetFileName(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(RelativeDir);
                zip.Save();
            }
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entries,
                    "The Zip file has the wrong number of entries.");
        }


        [TestMethod]
        public void CreateZip_AddDirectory_OneCharOverrideName()
        {
            int entries = 0;
            String filename = null;

            // set the name of the zip file to create
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "CreateZip_AddDirectory_OneCharOverrideName.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            String CommentOnArchive = "BasicTests::CreateZip_AddDirectory_OneCharOverrideName(): This archive override the name of a directory with a one-char name.";

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            int NumFilesToCreate = _rnd.Next(23) + 14;
            var checksums = new Dictionary<string, string>();
            for (int j = 0; j < NumFilesToCreate; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(12000) + 5000);
                var chk = TestUtilities.ComputeChecksum(filename);

                var relativePath = System.IO.Path.Combine(System.IO.Path.GetFileName(Subdir), System.IO.Path.GetFileName(filename));
                //var key = System.IO.Path.Combine("A", filename);
                var key = TestUtilities.TrimVolumeAndSwapSlashes(relativePath);
                checksums.Add(key, TestUtilities.CheckSumToString(chk));

                entries++;
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                zip1.AddDirectory(Subdir, System.IO.Path.GetFileName(Subdir));
                zip1.Comment = CommentOnArchive;
                zip1.Save();
            }

            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entries,
                    "The created Zip file has an unexpected number of entries.");

            // validate all the checksums
            using (ZipFile zip2 = new ZipFile(ZipFileToCreate))
            {
                foreach (ZipEntry e in zip2)
                {
                    e.Extract("unpack");
                    if (checksums.ContainsKey(e.FileName))
                    {
                        string PathToExtractedFile = System.IO.Path.Combine("unpack", e.FileName);

                        // verify the checksum of the file is correct
                        string expectedCheckString = checksums[e.FileName];
                        string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(PathToExtractedFile));
                        Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", PathToExtractedFile);
                    }
                }
            }

        }



        [TestMethod]
        public void CreateZip_ForceNoCompressionAllEntries()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "ForceNoCompression.zip");
            //_FilesToRemove.Add(ZipFileToCreate);
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            String CommentOnArchive = "BasicTests::ForceNoCompression(): This archive override the name of a directory with a one-char name.";

            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            int fileCount = _rnd.Next(10) + 10;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.ForceNoCompression = true;
                zip.AddDirectory(Subdir, System.IO.Path.GetFileName(Subdir));
                zip.Comment = CommentOnArchive;
                zip.Save();
            }

            int entriesFound = 0;
            using (ZipFile zip = ZipFile.Read(ZipFileToCreate))
            {
                foreach (ZipEntry e in zip)
                {
                    if (!e.IsDirectory) entriesFound++;
                    Assert.AreEqual<short>(0, e.CompressionMethod, "Unexpected compression method on zipped entry.");
                }
            }
            Assert.AreEqual<int>(entriesAdded, entriesFound,
             "The created Zip file has an unexpected number of entries.");
        }

        [TestMethod]
        public void CreateZip_ForceNoCompressionSomeEntries()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "ForceNoCompression.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            int fileCount = _rnd.Next(13) + 13;
            for (int j = 0; j < fileCount; j++)
            {
                if (_rnd.Next(2) == 0)
                {
                    filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                }
                else
                {
                    filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.bin", j));
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(34000) + 5000);
                }
                entriesAdded++;
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                {
                    ZipEntry e = zip.AddFile(f, "");
                    if (f.EndsWith(".bin"))
                        e.CompressionMethod = 0x0;
                }
                zip.Comment = "Some of these files do not use compression.";
                zip.Save();
            }

            int entriesFound = 0;
            using (ZipFile zip = ZipFile.Read(ZipFileToCreate))
            {
                foreach (ZipEntry e in zip)
                {
                    if (!e.IsDirectory) entriesFound++;
                    if (e.FileName.EndsWith(".txt"))
                        Assert.AreEqual<short>(0x08, e.CompressionMethod, "Unexpected compression method on zipped text file.");
                    else
                        Assert.AreEqual<short>(0x00, e.CompressionMethod, "Unexpected compression method on zipped binary file.");
                }
            }
            Assert.AreEqual<int>(entriesAdded, entriesFound,
             "The created Zip file has an unexpected number of entries.");
        }


    }
}
