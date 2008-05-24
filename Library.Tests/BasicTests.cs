using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Utils.Zip;
using Library.TestUtilities;

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
        public void BasicZipFilesViaAddItem()
        {
            string ZipFileToCreate = TestUtilities.GenerateUniqueFilename("zip");
            _FilesToRemove.Add(ZipFileToCreate);

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            int fileCount = _rnd.Next(3) + 3;
            int i;
            string[] FilesToZip = new string[fileCount];
            for (i = 0; i < fileCount; i++)
                FilesToZip[i] =
                    TestUtilities.CreateUniqueFile("bin", _rnd.Next(10000) + 5000);

            _FilesToRemove.AddRange(FilesToZip);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (i = 0; i < FilesToZip.Length; i++)
                {
                    zip.AddItem(FilesToZip[i]);
                }
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, FilesToZip.Length),
                    "Zip file created seems to be invalid.");

        }

        [TestMethod]
        public void BasicZipFilesViaAddFile()
        {
            string ZipFileToCreate = TestUtilities.GenerateUniqueFilename("zip");
            _FilesToRemove.Add(ZipFileToCreate);
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            int fileCount = _rnd.Next(3) + 3;
            int i;
            string[] FilesToZip = new string[fileCount];
            for (i = 0; i < fileCount; i++)
                FilesToZip[i] =
                    TestUtilities.CreateUniqueFile("bin", _rnd.Next(10000) + 5000);

            _FilesToRemove.AddRange(FilesToZip);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (i = 0; i < FilesToZip.Length; i++)
                {
                    zip.AddFile(FilesToZip[i]);
                }
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, FilesToZip.Length),
                    "Zip file created seems to be invalid.");

        }

        [TestMethod]
        public void OnlyZeroLengthFilesViaAddFile()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "ZeroLengthFiles.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            int fileCount = _rnd.Next(3) + 3;
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

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, FilesToZip.Length),
                    "Zip file created seems to be invalid.");

        }

        [TestMethod]
        public void LargeNumberOfSmallFilesInADirectory()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "LargeNumberOfSmallFiles.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "zipthis");
            System.IO.Directory.CreateDirectory(DirToZip);

            int entries = 0;
            int subdirCount = _rnd.Next(117) + 192;
            TestContext.WriteLine("LargeNumberOfFiles: Creating {0} subdirs.", subdirCount);
            for (int i = 0; i < subdirCount; i++)
            {
                string SubDir = System.IO.Path.Combine(DirToZip, String.Format("dir{0:D4}", i));
                System.IO.Directory.CreateDirectory(SubDir);

                int filecount = _rnd.Next(317) + 37;
                TestContext.WriteLine("LargeNumberOfFiles: Subdir {0}, Creating {1} files.", i, filecount);
                for (int j = 0; j < filecount; j++)
                {
                    string filename = String.Format("file{0:D4}.x", j);
                    TestUtilities.CreateAndFillFile(System.IO.Path.Combine(SubDir, filename),
                        _rnd.Next(1000) + 100);
                    entries++;
                }
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(System.IO.Path.GetFileName(DirToZip));
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries),
                    "Zip file created seems to be invalid.");

        }

        [TestMethod]
        public void OnlyZeroLengthFilesViaAddDirectory()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "ZeroLengthFiles.zip");
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

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries),
                    "Zip file created seems to be invalid.");
        }

        [TestMethod]
        public void OnlyEmptyDirectories()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "EmptyDirectories.zip");
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

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries),
                    "Zip file created seems to be invalid.");
        }

        [TestMethod]
        public void CheckStatusTextWriter()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Test.zip");
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

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries),
                    "Zip file created seems to be invalid.");
        }

        [TestMethod]
        public void ZipDirectoryViaAddDirectory()
        {
            string ZipFileToCreate = TestUtilities.GenerateUniqueFilename("zip");
            _FilesToRemove.Add(ZipFileToCreate);

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            int fileCount = _rnd.Next(3) + 3;
            for (int i = 0; i < fileCount; i++)
                TestUtilities.CreateUniqueFile("bin", TopLevelDir, _rnd.Next(10000) + 5000);

            string dirToZip = System.IO.Path.GetFileName(TopLevelDir);
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(dirToZip);
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, fileCount),
                    "Zip file created seems to be invalid.");
        }

        [TestMethod]
        public void VerifyThatStreamRemainsOpenAfterSave()
        {
            int filesAdded = _rnd.Next(3) + 3;
            for (int i = 0; i < filesAdded; i++)
                TestUtilities.CreateUniqueFile("bin", TopLevelDir, _rnd.Next(10000) + 5000);

            string dirToZip = System.IO.Path.GetFileName(TopLevelDir);
            var ms = new System.IO.MemoryStream();
            Assert.IsTrue(ms.CanSeek, "The MemoryStream does not do Seek.");
            using (ZipFile zip = new ZipFile(ms))
            {
                zip.AddDirectory(dirToZip);
                zip.Save();
            }

            Assert.IsTrue(ms.CanSeek, "After writing, the OutputStream does not do Seek.");
            Assert.IsTrue(ms.CanRead, "The OutputStream cannot be Read.");

            // seek to the beginning
            ms.Seek(0, System.IO.SeekOrigin.Begin);
            int filesFound = 0;
            using (ZipFile z2 = ZipFile.Read(ms))
            {
                foreach (ZipEntry e in z2)
                {
                    if (!e.IsDirectory)
                        filesFound++;
                }
            }

            Assert.AreEqual<int>(filesFound, filesAdded, "Found an incorrect number of files.");
        }

        [TestMethod]
        public void FileComments()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "FileComments.zip");
            _FilesToRemove.Add(ZipFileToCreate);

            string FileComment = "Comment Added By Test";
            String CommentOnArchive = "Comment added by FileComments() method.";

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

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
                    e.Comment = String.Format(FileComment, i);
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
                    Assert.AreEqual<string>(FileComment, e.Comment, "Unexpected comment on ZipEntry.");
                    entries++;
                }
            }
            Assert.AreEqual<int>(entries, FilesToZip.Length, "Unexpected file count. Expected {0}, got {1}.",
                    FilesToZip.Length, entries);
        }

        [TestMethod]
        public void ZipDirectoryWithNoFilesInRoot()
        {
            int i, j;
            int entries = 0;

            int subdirCount = _rnd.Next(4) + 4;
            for (i = 0; i < subdirCount; i++)
            {
                string Subdir = System.IO.Path.Combine(TopLevelDir,"DirectoryToZip.test."+i);
                System.IO.Directory.CreateDirectory(Subdir);

                int fileCount = _rnd.Next(3) + 3;
                for (j = 0; j < fileCount; j++)
                {
                    String file = System.IO.Path.Combine(Subdir, "file" + j);
                    TestUtilities.CreateAndFillFile(file, _rnd.Next(100) + 500);
                    entries++;
                }
            }

            string ZipFileToCreate = TestUtilities.GenerateUniqueFilename("zip");
            _FilesToRemove.Add(ZipFileToCreate);

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string RelativeDir = System.IO.Path.GetFileName(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(RelativeDir);
                zip.Save();
            }
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries),
                    "Zip file created seems to be invalid.");
        }

        [TestMethod]
        public void OverwriteExistingZip()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "ZipFileToOverwrite.zip");
            _FilesToRemove.Add(ZipFileToCreate);
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            String CommentOnArchive = "BasicTests::OverwriteExistingZip(): This archive will be overwritten.";

            int i, j;
            int entries = 0;
            string Subdir = null;
            String filename = null; 
            int subdirCount = _rnd.Next(4) + 4;
            for (i = 0; i < subdirCount; i++)
            {
                Subdir = System.IO.Path.Combine(TopLevelDir, "Directory." + i);
                System.IO.Directory.CreateDirectory(Subdir);

                int fileCount = _rnd.Next(3) + 3;
                for (j = 0; j < fileCount; j++)
                {
                    filename = System.IO.Path.Combine(Subdir, "file" + j + ".txt");
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(12000) + 5000);
                    entries++;
                }
            }


            string RelativeDir = System.IO.Path.GetFileName(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(RelativeDir);
                zip.Comment = CommentOnArchive;
                zip.Save();
            }
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries),
                    "The created Zip file has an unexpected number of entries.");


            // Now create a new subdirectory and add that one

                            Subdir = System.IO.Path.Combine(TopLevelDir, "NewSubDirectory");
                System.IO.Directory.CreateDirectory(Subdir);

                    filename= System.IO.Path.Combine(Subdir, "newfile.txt");
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(12000) + 5000);
                    entries++;


                    string DirToAdd= System.IO.Path.Combine(RelativeDir,
                        System.IO.Path.GetFileName(Subdir));

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(DirToAdd);
                zip.Comment = "OVERWRITTEN";
                // this will overwrite the existing zip file
                
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entries),
                    "The overwritten Zip file has the wrong number of entries.");

            using (ZipFile readzip = new ZipFile(ZipFileToCreate))
            {
                Assert.AreEqual<string>(readzip.Comment, "OVERWRITTEN", "The zip comment in the overwritten archive is incorrect.");
            }
        }


    }
}
