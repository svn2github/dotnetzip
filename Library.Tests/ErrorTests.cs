using System;
//using System.Text;
//using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Utils.Zip;
using Library.TestUtilities;

namespace Ionic.Utils.Zip.Tests.Error
{
    /// <summary>
    /// Summary description for ErrorTests
    /// </summary>
    [TestClass]
    public class ErrorTests
    {
        private System.Random _rnd = null;

        public ErrorTests()
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

        #endregion


        #region Test Init and Cleanup
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
        [ExpectedException(typeof(System.IO.FileNotFoundException))]
        public void AddNonexistentFile()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "FileNotFound.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddFile("ThisFileDoesNotExist.txt");
                zip.Comment = "This is a Comment On the Archive";
                zip.Save();
            }
        }


        [TestMethod]
        [ExpectedException(typeof(System.IO.IOException))]
        public void ExtractExistingFileWithoutOverwrite()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "ExtractWithoutOverwrite.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            Directory.SetCurrentDirectory(TopLevelDir);

            string[] filenames = 
            {
                Path.Combine(SourceDir, "Examples\\Zipit\\bin\\Debug\\Zipit.exe"),
                Path.Combine(SourceDir, "Library\\bin\\Debug\\Ionic.Utils.Zip.dll"),
                Path.Combine(SourceDir, "Library\\bin\\Debug\\Ionic.Utils.Zip.pdb"),
                Path.Combine(SourceDir, "Library\\bin\\Debug\\Ionic.Utils.Zip.xml"),
                Path.Combine(SourceDir, "AppNote.txt")
            };
            int j = 0;
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                    zip.AddFile(filenames[j], "");
                zip.Comment = "This is a Comment On the Archive";
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, filenames.Length),
                "Zip file created seems to be invalid.");

            // extract the first time - this should succeed
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                    zip[Path.GetFileName(filenames[j])].Extract("unpack", false);
            }

            // extract the first time - this should fail
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                    zip[Path.GetFileName(filenames[j])].Extract("unpack", false);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Ionic.Utils.Zip.BadReadException))]
        public void ReadInvalidZip()
        {
            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            Directory.SetCurrentDirectory(TopLevelDir);

            string filename =
                Path.Combine(SourceDir, "Examples\\Zipit\\bin\\Debug\\Zipit.exe");

            // try reading the invalid zipfile - this should fail
            using (ZipFile zip = new ZipFile(filename))
            {
                foreach (ZipEntry e in zip)
                {
                    System.Console.WriteLine("name: {0}  compressed: {1} has password?: {2}",
                        e.FileName, e.CompressedSize, e.UsesEncryption);
                }
            }
        }


        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void SaveToInvalidLocation()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "BadSave.zip");

            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            Directory.SetCurrentDirectory(TopLevelDir);

            string filename =
                Path.Combine(SourceDir, "Examples\\Zipit\\bin\\Debug\\Zipit.exe");

            // add an entry to the zipfile, then try saving to a directory. this should fail
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddFile(filename,"");
                zip.Save("c:\\Windows\\");
            }

        }

        [TestMethod]
        [ExpectedException(typeof(Ionic.Utils.Zip.BadStateException))]
        public void SaveWithNoFilename()
        {
            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            Directory.SetCurrentDirectory(TopLevelDir);

            string filename =
                Path.Combine(SourceDir, "Examples\\Zipit\\bin\\Debug\\Zipit.exe");

            // add an entry to the zipfile, then try saving, never having specified a filename. This should fail.
            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile(filename, "");
                zip.Save(); // don't know where to save!
            }
        }


        [TestMethod]
        [ExpectedException(typeof(System.IO.IOException))]
        public void AddDirectoryAsFile()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "BadAdd.zip");

            Directory.SetCurrentDirectory(TopLevelDir);

            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            string filename = Path.Combine(SourceDir, "Examples\\Zipit\\bin\\Debug\\Zipit.exe");
            File.Copy(filename, "ThisIsAFile");

            string baddirname = Path.Combine(TopLevelDir, "ThisIsAFile");

            // try reading the invalid zipfile - this should fail
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(baddirname);
                zip.Save();
            }
        }


        [TestMethod]
        [ExpectedException(typeof(System.IO.FileNotFoundException))]
        public void AddFileAsDirectory()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "BadAdd.zip");

            Directory.SetCurrentDirectory(TopLevelDir);

            Directory.CreateDirectory("ThisIsADirectory.txt");

            string badfilename = Path.Combine(TopLevelDir, "ThisIsADirectory.txt");

            // try reading the invalid zipfile - this should fail
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddFile(badfilename);
                zip.Save();
            }
        }



        [TestMethod]
        [ExpectedException(typeof(System.SystemException))] // not sure which exception - could be one of several.
        public void ReadCorruptedZip()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "TryReadingCorruptedZip.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            Directory.SetCurrentDirectory(TopLevelDir);

            string[] filenames = 
            {
                Path.Combine(SourceDir, "Examples\\Zipit\\bin\\Debug\\Zipit.exe"),
                Path.Combine(SourceDir, "AppNote.txt")
            };

            string[] passwords = 
            {
                    "12345678",
                    "0987654321",
            };

            int j = 0;
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                {
                    zip.Password = passwords[j];
                    zip.AddFile(filenames[j], "");
                }
                zip.Save();
            }

            // now corrupt the zip.
            using (FileStream fs = File.OpenWrite(ZipFileToCreate))
            {
                byte[] corruption = new byte[_rnd.Next(100) + 12];
                _rnd.NextBytes(corruption);

                int min = 5;
                int max = 12; 
                int offsetForCorruption = _rnd.Next(min, max);
                int length = _rnd.Next(8) + 3;
                fs.Seek(offsetForCorruption, SeekOrigin.Begin);
                fs.Write(corruption, 0, length);

                max = (int)fs.Length - 100;
                offsetForCorruption = _rnd.Next(min, max);
                fs.Seek(offsetForCorruption, SeekOrigin.Begin);

                _rnd.NextBytes(corruption);
                fs.Write(corruption, 0, corruption.Length);
            }

            try
            {
                // read the corrupted zip- this should fail
                using (ZipFile zip = new ZipFile(ZipFileToCreate))
                {
                    for (j = 0; j < filenames.Length; j++)
                    {
                        ZipEntry e = zip[Path.GetFileName(filenames[j])];

                        System.Console.WriteLine("name: {0}  compressed: {1} has password?: {2}",
                            e.FileName, e.CompressedSize, e.UsesEncryption);
                        e.ExtractWithPassword("unpack", passwords[j]);
                    }
                }
            }
            catch (Exception exc1)
            {
                throw new SystemException("expected", exc1);
            }
        }



    }
}
