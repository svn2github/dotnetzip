using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Utils.Zip;

namespace Library.Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class UnitTest1
    {
        private System.Random _rnd;

        public UnitTest1()
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
        [TestInitialize()]
        public void MyTestInitialize()
        {
        }


        System.Collections.Generic.List<string> _FilesToRemove = new System.Collections.Generic.List<string>();

        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            foreach (string filename in _FilesToRemove)
            {
                if (System.IO.Directory.Exists(filename))
                    System.IO.Directory.Delete(filename, true);

                if (System.IO.File.Exists(filename))
                    System.IO.File.Delete(filename);
            }
        }
        #endregion

        #region Helper methods


        private string CreateUniqueFile(string extension, string ContainingDirectory)
        {
            string fileToCreate = GenerateUniqueFilename(extension, ContainingDirectory);
            System.IO.File.Create(fileToCreate);
            return fileToCreate;
        }
        private string CreateUniqueFile(string extension)
        {
            return CreateUniqueFile(extension, null);
        }

        private string CreateUniqueFile(string extension, int size)
        {
            return CreateUniqueFile(extension, null, size);
        }

        private string CreateUniqueFile(string extension, string ContainingDirectory, int size)
        {
            string fileToCreate = GenerateUniqueFilename(extension, ContainingDirectory);
            Assert.IsTrue(size > 0, "File size should be greater than zero.");
            int bytesRemaining = size;
            byte[] Buffer = new byte[2000];
            using (System.IO.Stream fileStream = new System.IO.FileStream(fileToCreate, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                while (bytesRemaining > 0)
                {
                    int sizeOfChunkToWrite = (bytesRemaining > Buffer.Length) ? Buffer.Length : bytesRemaining;
                    _rnd.NextBytes(Buffer);
                    fileStream.Write(Buffer, 0, sizeOfChunkToWrite);
                    bytesRemaining -= sizeOfChunkToWrite;
                }
            }
            return fileToCreate;
        }

        System.Reflection.Assembly _a = null;
        private System.Reflection.Assembly _MyAssembly
        {
            get
            {
                if (_a == null)
                {
                    _a = System.Reflection.Assembly.GetExecutingAssembly();
                }
                return _a;
            }
        }

        private string GenerateUniqueFilename(string extension)
        {
            return GenerateUniqueFilename(extension, null);
        }
        private string GenerateUniqueFilename(string extension, string ContainingDirectory)
        {
            string candidate = null;
            String AppName = _MyAssembly.GetName().Name;

            string parentDir = (ContainingDirectory == null) ? System.Environment.GetEnvironmentVariable("TEMP") :
                ContainingDirectory;
            if (parentDir == null) return null;

            int index = 0;
            do
            {
                index++;
                string Name = String.Format("{0}-{1}-{2}.{3}",
                    AppName, System.DateTime.Now.ToString("yyyyMMMdd-HHmmss"), index, extension);
                candidate = System.IO.Path.Combine(parentDir, Name);
            } while (System.IO.File.Exists(candidate));

            // this file/path does not exist.  It can now be created, as file or directory. 
            return candidate;
        }

        private bool CheckZip(string zipfile, int fileCount)
        {
            int entries = 0;
            using (ZipFile zip = ZipFile.Read(zipfile))
            {
                foreach (ZipEntry e in zip)
                    if (!e.IsDirectory) entries++;
            }
            return (entries == fileCount);
        }

        #endregion

        [TestMethod]
        public void BasicZipFilesViaAddItem()
        {
            string ZipFileToCreate = GenerateUniqueFilename("zip");
            _FilesToRemove.Add(ZipFileToCreate);

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            int fileCount = _rnd.Next(3) + 3;
            int i;
            string[] FilesToZip = new string[fileCount];
            for (i = 0; i < fileCount; i++)
                FilesToZip[i] =
                    CreateUniqueFile("bin", _rnd.Next(10000) + 5000);

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

            Assert.IsTrue(CheckZip(ZipFileToCreate, FilesToZip.Length),
                    "Zip file created seems to be invalid.");

        }

        [TestMethod]
        public void BasicZipFilesViaAddFile()
        {
            string ZipFileToCreate = GenerateUniqueFilename("zip");
            _FilesToRemove.Add(ZipFileToCreate);
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            int fileCount = _rnd.Next(3) + 3;
            int i;
            string[] FilesToZip = new string[fileCount];
            for (i = 0; i < fileCount; i++)
                FilesToZip[i] =
                    CreateUniqueFile(".bin", _rnd.Next(10000) + 5000);

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

            Assert.IsTrue(CheckZip(ZipFileToCreate, FilesToZip.Length),
                    "Zip file created seems to be invalid.");

        }

        [TestMethod]
        public void ZipDirectoryViaAddDirectory()
        {
            string currentDir = System.IO.Directory.GetCurrentDirectory();
            string DirToCreate = GenerateUniqueFilename("tmp");
            System.IO.Directory.CreateDirectory(DirToCreate);
            _FilesToRemove.Add(DirToCreate);

            string ZipFileToCreate = GenerateUniqueFilename("zip");
            _FilesToRemove.Add(ZipFileToCreate);

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            int fileCount = _rnd.Next(3) + 3;
            int i;
            string[] FilesToZip = new string[fileCount];
            for (i = 0; i < fileCount; i++)
                FilesToZip[i] =
                    CreateUniqueFile(".bin", DirToCreate, _rnd.Next(10000) + 5000);

            _FilesToRemove.AddRange(FilesToZip);

            System.IO.Directory.SetCurrentDirectory(System.IO.Path.GetDirectoryName(DirToCreate));
            string dirToZip = System.IO.Path.GetFileName(DirToCreate);
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(dirToZip);
                zip.Save();
            }

            System.IO.Directory.SetCurrentDirectory(currentDir);

            Assert.IsTrue(CheckZip(ZipFileToCreate, FilesToZip.Length),
                    "Zip file created seems to be invalid.");

        }


        [TestMethod]
        public void CheckStatusMessagesOnZipUp()
        {
            Assert.IsTrue(false);
        }

        [TestMethod]
        public void FileComments()
        {
            string ZipFileToCreate = GenerateUniqueFilename("zip");
            _FilesToRemove.Add(ZipFileToCreate);

            string FileComment = "Comment Added By Test";
            String CommentOnArchive = "Comment added by FileComments() method.";

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            string[] FilesToZip = new string[] {
                CreateUniqueFile(".bin", _rnd.Next(10000) + 5000),
                CreateUniqueFile(".bin", _rnd.Next(10000) + 5000),
                CreateUniqueFile(".bin", _rnd.Next(10000) + 5000),
            };
            _FilesToRemove.AddRange(FilesToZip);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (int i = 0; i < FilesToZip.Length; i++)
                {
                    ZipEntry e = zip.AddFile(FilesToZip[i]);
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
        public void ZipDirectoryWithEmptyRoot()
        {
            int i, j;
            int entries = 0;

            string currentDir = System.IO.Directory.GetCurrentDirectory();

            string TopLevelDir = GenerateUniqueFilename("tmp");
            System.IO.Directory.CreateDirectory(TopLevelDir);
            _FilesToRemove.Add(TopLevelDir);

            int subdirCount = _rnd.Next(2) + 2;
            for (i = 0; i < subdirCount; i++)
            {
                string Subdir = GenerateUniqueFilename("tmp", TopLevelDir);
                System.IO.Directory.CreateDirectory(Subdir);

                int fileCount = _rnd.Next(2) + 2;
                for (j = 0; j < fileCount; j++)
                {
                    CreateUniqueFile("bin", Subdir, _rnd.Next(10000) + 5000);
                    entries++; 
                }
            }

            string ZipFileToCreate = GenerateUniqueFilename("zip");
            _FilesToRemove.Add(ZipFileToCreate);

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            System.IO.Directory.SetCurrentDirectory(System.IO.Path.GetDirectoryName(TopLevelDir));
            string RelativeDir = System.IO.Path.GetFileName(TopLevelDir);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(RelativeDir);
                zip.Save();
            }
            System.IO.Directory.SetCurrentDirectory(currentDir);


            Assert.IsTrue(CheckZip(ZipFileToCreate, entries),
                    "Zip file created seems to be invalid.");

        }

    }
}
