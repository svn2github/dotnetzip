using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Utils.Zip;
using Library.TestUtilities;

namespace Ionic.Utils.Zip.Tests.Update
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class UpdateTests
    {
        private System.Random _rnd;

        public UpdateTests()
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
        public void UpdateZip_AddNewDirectory()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_AddNewDirectory.zip");
            //_FilesToRemove.Add(ZipFileToCreate);
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            String CommentOnArchive = "BasicTests::UpdateZip_AddNewDirectory(): This archive will be overwritten.";

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

            filename = System.IO.Path.Combine(Subdir, "newfile.txt");
            TestUtilities.CreateAndFillFileText(filename, _rnd.Next(12000) + 5000);
            entries++;

            string DirToAdd = System.IO.Path.Combine(RelativeDir,
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


        [TestMethod]
        public void UpdateZip_OpenForUpdate_UpdateFile()
        {
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_OpenForUpdate_UpdateFile.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int fileCount = _rnd.Next(3) + 4;
            string filename = null;
            int entriesAdded = 0;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, "file" + j + ".txt");
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                {
                    ZipEntry e = zip.AddFile(f, "");
                }
                zip.Comment = "UpdateTests::UpdateZip_OpenForUpdate_UpdateFile(): This archive will be updated.";
                zip.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The overwritten Zip file has the wrong number of entries.");

            // create and file a new file with text data
            int FileToUpdate = _rnd.Next(fileCount);
            filename = "file" + FileToUpdate + ".txt";
            int bytesRemaining = _rnd.Next(21567) + 23872;
            string repeatedLine = String.Format("**UPDATED** This file has been updated at {0}.",
                System.DateTime.Now.ToString("G"));
            using (System.IO.StreamWriter sw = System.IO.File.CreateText(filename))
            {
                do
                {
                    sw.WriteLine(repeatedLine);
                    bytesRemaining -= repeatedLine.Length;
                } while (bytesRemaining > 0);
                sw.Close();
            }

            // update that file in the zip archive
            using (ZipFile z = ZipFile.OpenForUpdate(ZipFileToCreate))
            {
                ZipEntry e = z.UpdateFile(filename, "");
                z.Comment = "UpdateTests::UpdateZip_OpenForUpdate_UpdateFile(): This archive has been updated.";
                z.Save();
            }


            // verify the content of the updated file. 
            var sr = new System.IO.StreamReader(filename);
            string sLine = sr.ReadLine();
            sr.Close();

            Assert.AreEqual<string>(sLine, repeatedLine,
                    "The content of the Updated file in the zip archive is incorrect.");
        }


        [TestMethod]
        public void UpdateZip_Read_AllowUpdates_UpdateFile()
        {
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateExistingZip.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int fileCount = _rnd.Next(3) + 4;
            string filename = null;
            int entriesAdded = 0;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, "file" + j + ".txt");
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                {
                    ZipEntry e = zip.AddFile(f, "");
                }
                zip.Comment = "UpdateTests::UpdateExistingZip(): This archive will be updated.";
                zip.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");

            // create and file a new file with text data
            int FileToUpdate = _rnd.Next(fileCount);
            filename = "file" + FileToUpdate + ".txt";
            int bytesRemaining = _rnd.Next(21567) + 23872;
            string repeatedLine = String.Format("**UPDATED** This file has been updated at {0}.",
                System.DateTime.Now.ToString("G"));
            using (System.IO.StreamWriter sw = System.IO.File.CreateText(filename))
            {
                do
                {
                    sw.WriteLine(repeatedLine);
                    bytesRemaining -= repeatedLine.Length;
                } while (bytesRemaining > 0);
                sw.Close();
            }

            // update that file in the zip archive
            using (ZipFile z = ZipFile.Read(ZipFileToCreate))
            {
                z.AllowUpdates = true;
                ZipEntry e = z.UpdateFile(filename, "");
                z.Comment = "UpdateTests::UpdateExistingZip(): This archive has been updated.";
                z.Save();
            }


            // verify the content of the updated file. 
            var sr = new System.IO.StreamReader(filename);
            string sLine = sr.ReadLine();
            sr.Close();

            Assert.AreEqual<string>(sLine, repeatedLine,
                    "The content of the Updated file in the zip archive is incorrect.");
        }


        [TestMethod]
        public void UpdateZip_Read_AllowUpdates_RemoveEntry()
        {
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "RemoveEntry.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int fileCount = _rnd.Next(3) + 4;
            string filename = null;
            int entriesAdded = 0;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, "file" + j + ".txt");
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            int ix = 0;
            using (ZipFile zip = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                {
                    ZipEntry e = zip.AddFile(f, "");
                    e.LastModified = new System.DateTime(2007, 1 + ix, 15, 12, 1, 0);
                    ix++;
                }
                zip.Comment = "UpdateTests::UpdateZip_Read_AllowUpdates_RemoveEntry(): This archive will be updated.";
                zip.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");


            // selectively remove a few files in the zip archive
            var Threshold = new System.DateTime(2007, 4, 15, 12, 1, 0);
            int numRemoved = 0;
            using (ZipFile z = ZipFile.Read(ZipFileToCreate))
            {
                List<ZipEntry> EntriesToRemove = new List<ZipEntry>();
                z.AllowUpdates = true;
                foreach (ZipEntry e in z)
                {
                    if (e.LastModified < Threshold)
                    {
                        // We cannot remove the entry from the list, within the context of 
                        // an enumeration of said list.
                        // So we add the doomed entry to a list to be removed later.
                        EntriesToRemove.Add(e);
                        numRemoved++;
                    }
                }

                // actually remove the entry. 
                foreach (ZipEntry zombie in EntriesToRemove)
                    z.RemoveEntry(zombie);

                z.Comment = "UpdateTests::UpdateZip_Read_AllowUpdates_RemoveEntry(): This archive has been updated.";
                z.Save();
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded - numRemoved),
                "The updated Zip file has the wrong number of entries.");
        }

        [TestMethod]
        public void UpdateZip_OpenForUpdate_Password_RemoveByFilename()
        {
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_OpenForUpdate_Password_RemoveByFilename.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int fileCount = _rnd.Next(13) + 24;

            string filename = null;
            int entriesToBeAdded = 0;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesToBeAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                zip.Password = "Hookahoo";
                foreach (String f in filenames)
                    zip.AddFile(f, "");

                zip.Comment = "UpdateTests::UpdateZip_OpenForUpdate_Password_RemoveByFilename(): This archive will be updated.";
                zip.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesToBeAdded),
                "The Zip file has the wrong number of entries.");

            // selectively remove a few files in the zip archive
            int Threshold = _rnd.Next(fileCount - 1) + 1;
            int numRemoved = 0;
            using (ZipFile z = ZipFile.Read(ZipFileToCreate))
            {
                z.AllowUpdates = true;
                var AllFileNames = z.EntryFilenames;
                foreach (String s in AllFileNames)
                {
                    int fileNum = Int32.Parse(s.Substring(4, 3));
                    if (fileNum < Threshold)
                    {
                        numRemoved++;
                        z.RemoveEntry(s);
                    }
                }

                z.Comment = "UpdateTests::UpdateZip_OpenForUpdate_Password_RemoveByFilename(): This archive has been updated.";
                z.Save();
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesToBeAdded - numRemoved),
                "The updated Zip file has the wrong number of entries.");
        }

        [TestMethod]
        public void UpdateZip_OpenForUpdate_Password_RemoveViaIndexer()
        {
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_OpenForUpdate_Password_RemoveViaIndexer.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int fileCount = _rnd.Next(13) + 24;

            string filename = null;
            int entriesToBeAdded = 0;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesToBeAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                zip.Password = "Wheeee!!";
                foreach (String f in filenames)
                    zip.AddFile(f, "");

                zip.Comment = "UpdateTests::UpdateZip_OpenForUpdate_Password_RemoveViaIndexer(): This archive will be updated.";
                zip.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesToBeAdded),
                "The Zip file has the wrong number of entries.");

            // selectively remove a few files in the zip archive
            int Threshold = _rnd.Next(fileCount - 1) + 1;
            int numRemoved = 0;
            using (ZipFile z = ZipFile.Read(ZipFileToCreate))
            {
                z.AllowUpdates = true;
                var AllFileNames = z.EntryFilenames;
                foreach (String s in AllFileNames)
                {
                    int fileNum = Int32.Parse(s.Substring(4, 3));
                    if (fileNum < Threshold)
                    {
                        // use the indexer to remove a file from the zip archive
                        z[s] = null;
                        numRemoved++;
                    }
                }

                z.Comment = "UpdateTests::UpdateZip_OpenForUpdate_Password_RemoveViaIndexer(): This archive has been updated.";
                z.Save();
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesToBeAdded - numRemoved),
                "The updated Zip file has the wrong number of entries.");
        }


        [TestMethod]
        public void UpdateZip_AddFile_OldEntriesWithPassword()
        {
            string Password = "Secret!";
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_AddFile_OldEntriesWithPassword.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int fileCount = _rnd.Next(10) + 8;
            string filename = null;
            int entriesAdded = 0;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                string Line = String.Format("This line is repeated over and over and over in file {0}",
                    System.IO.Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, Line, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip = new ZipFile())
            {
                zip.Password = Password;
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                {
                    ZipEntry e = zip.AddFile(f, "");
                }
                zip.Comment = "UpdateTests::UpdateZip_AddFile_OldEntriesWithPassword(): This archive will be updated.";
                zip.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");

            // create and fill a new file with text data
            filename = "NewFileToAdd.txt";
            string repeatedLine = String.Format("**UPDATED** This file has been updated at {0}.",
                System.DateTime.Now.ToString("G"));
            TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(21567) + 23872);

            // update that file in the zip archive
            using (ZipFile z = ZipFile.Read(ZipFileToCreate))
            {
                // no password used here.
                z.AllowUpdates = true;
                ZipEntry e = z.AddFile(filename, "");
                z.Comment = "UpdateTests::UpdateZip_AddFile_OldEntriesWithPassword(): This archive has been updated.";
                z.Save();
            }

            // now extract the newly-added file from the archive
            using (ZipFile z2 = ZipFile.Read(ZipFileToCreate))
            {
                // no password used here.
                z2[filename].Extract("extract");
            }

            // verify the content of the extracted file. 
            var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", filename));
            string sLine = sr.ReadLine();
            sr.Close();

            Assert.AreEqual<string>(sLine, repeatedLine,
                    "The content of the Updated file in the zip archive is incorrect.");

            // now extract the newly-added file from the archive
            int FileToUpdate = _rnd.Next(fileCount);
            filename = String.Format("file{0:D3}.txt", FileToUpdate);
            using (ZipFile z3 = ZipFile.Read(ZipFileToCreate))
            {
                // use a password here!               
                z3[filename].ExtractWithPassword("extract", Password);
            }

            // verify the content of the extracted file. 
            sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", filename));
            sLine = sr.ReadLine();
            sr.Close();

            repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                    filename);
            Assert.AreEqual<string>(sLine, repeatedLine,
                    "The content of the Updated file in the zip archive is incorrect.");
        }


        [TestMethod]
        public void UpdateZip_AddFile_NewEntriesWithPassword()
        {
            string Password = "Secret!";
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_AddFile_NewEntriesWithPassword.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int fileCount = _rnd.Next(10) + 8;
            string filename = null;
            int entriesAdded = 0;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                string Line = String.Format("This line is repeated over and over and over in file {0}",
                    System.IO.Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, Line, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                {
                    ZipEntry e = zip.AddFile(f, "");
                }
                zip.Comment = "UpdateTests::UpdateZip_UpdateFile_OldEntriesWithPassword(): This archive will be updated.";
                zip.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");

            // create and fill a new file with text data
            filename = "NewFileToAdd.txt";
            string repeatedLine = String.Format("**UPDATED** This file has been updated at {0}.",
                System.DateTime.Now.ToString("G"));
            TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(21567) + 23872);

            // add that new file to the zip archive
            using (ZipFile z = ZipFile.Read(ZipFileToCreate))
            {
                z.Password = Password;
                z.AllowUpdates = true;
                ZipEntry e = z.AddFile(filename, "");
                z.Comment = "UpdateTests::UpdateZip_AddFile_OldEntriesWithPassword(): This archive has been updated.";
                z.Save();
            }

            // now extract the newly-added file from the archive
            using (ZipFile z2 = ZipFile.Read(ZipFileToCreate))
            {
                z2[filename].ExtractWithPassword("extract", Password);
            }

            // verify the content of the extracted file. 
            var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", filename));
            string sLine = sr.ReadLine();
            sr.Close();

            Assert.AreEqual<string>(sLine, repeatedLine,
                    "The content of the Updated file in the zip archive is incorrect.");

            // now extract the newly-added file from the archive
            int FileToUpdate = _rnd.Next(fileCount);
            filename = String.Format("file{0:D3}.txt", FileToUpdate);
            using (ZipFile z3 = ZipFile.Read(ZipFileToCreate))
            {
                // no password here!               
                z3[filename].Extract("extract");
            }

            // verify the content of the extracted file. 
            sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", filename));
            sLine = sr.ReadLine();
            sr.Close();

            repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                    filename);
            Assert.AreEqual<string>(sLine, repeatedLine,
                    "The content of the Updated file in the zip archive is incorrect.");
        }

        [TestMethod]
        public void UpdateZip_AddFile_DifferentPasswords()
        {
            string Password1 = "Secret1";
            string Password2 = "Secret2";
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_AddFile_DifferentPasswords.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int fileCount = _rnd.Next(10) + 8;
            string filename = null;
            int entriesAdded = 0;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                string Line = String.Format("This line is repeated over and over and over in file {0}",
                    System.IO.Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, Line, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile z1 = new ZipFile())
            {
                z1.Password = Password1;
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                {
                    ZipEntry e = z1.AddFile(f, "");
                }
                z1.Comment = "UpdateTests::UpdateZip_AddFile_DifferentPasswords(): This archive will be updated.";
                z1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");

            // create and fill a new file with text data
            filename = "NewFileToAdd.txt";
            string repeatedLine = String.Format("**UPDATED** This file has been updated at {0}.",
                System.DateTime.Now.ToString("G"));
            TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(21567) + 23872);

            // add that new file to the zip archive
            using (ZipFile z2 = ZipFile.Read(ZipFileToCreate))
            {
                z2.Password = Password2;
                z2.AllowUpdates = true;
                ZipEntry e = z2.AddFile(filename, "");
                z2.Comment = "UpdateTests::UpdateZip_AddFile_DifferentPasswords(): This archive has been updated.";
                z2.Save();
            }

            // now extract the newly-added file from the archive
            using (ZipFile z3 = ZipFile.Read(ZipFileToCreate))
            {
                z3[filename].ExtractWithPassword("extract", Password2);
            }

            // verify the content of the extracted file. 
            var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", filename));
            string sLine = sr.ReadLine();
            sr.Close();

            Assert.AreEqual<string>(sLine, repeatedLine,
                    "The content of the Updated file in the zip archive is incorrect.");

            // now extract the newly-added file from the archive
            int FileToUpdate = _rnd.Next(fileCount);
            filename = String.Format("file{0:D3}.txt", FileToUpdate);
            using (ZipFile z4 = ZipFile.Read(ZipFileToCreate))
            {
                z4[filename].ExtractWithPassword("extract", Password1);
            }

            // verify the content of the extracted file. 
            sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", filename));
            sLine = sr.ReadLine();
            sr.Close();

            repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                    filename);
            Assert.AreEqual<string>(sLine, repeatedLine,
                    "The content of the Updated file in the zip archive is incorrect.");
        }



        [TestMethod]
        public void UpdateZip_UpdateFile_OldEntriesWithPassword()
        {
            string Password = "1234567";

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_OldEntriesWithPassword.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int fileCount = _rnd.Next(23) + 14;
            string filename = null;
            int entriesAdded = 0;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                string Line = String.Format("This line is repeated over and over and over in file {0}",
                    System.IO.Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, Line, _rnd.Next(34000) + 5000);

                entriesAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile z1 = new ZipFile())
            {
                z1.Password = Password;
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                {
                    ZipEntry e = z1.AddFile(f, "");
                }
                z1.Comment = "UpdateTests::UpdateZip_UpdateFile_OldEntriesWithPassword(): This archive will be updated.";
                z1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");

            // create and fill a new file with text data
            int FileToUpdate = _rnd.Next(fileCount);
            filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", FileToUpdate));
            string repeatedLine = String.Format("**UPDATED** This file has been updated at {0}.",
                System.DateTime.Now.ToString("G"));
            TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(34000) + 5000);

            // update that file in the zip archive
            using (ZipFile z2 = ZipFile.Read(ZipFileToCreate))
            {
                // no password used here.
                z2.AllowUpdates = true;
                ZipEntry e = z2.UpdateFile(filename, "");
                z2.Comment = "UpdateTests::UpdateZip_UpdateFile_OldEntriesWithPassword(): This archive has been updated.";
                z2.Save();
            }

            // trim off the leading path: 
            filename= System.IO.Path.GetFileName(filename);

            // now extract the file from the archive
            using (ZipFile z3 = ZipFile.Read(ZipFileToCreate))
            {
                // no password used here.
                z3[filename].Extract("extract");
            }

            // verify the content of the extracted file. 
            var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", filename));
            string sLine = sr.ReadLine();
            sr.Close();

            Assert.AreEqual<string>(sLine, repeatedLine,
                    "The content of the Updated file in the zip archive is incorrect.");

            // extract an encrypted file and check its contents
            int FileToRead = FileToUpdate;
            while (FileToRead == FileToUpdate) { FileToRead = _rnd.Next(fileCount); }
            filename = String.Format("file{0:D3}.txt", FileToRead);
            // now extract the file from the archive
            using (ZipFile z4 = ZipFile.Read(ZipFileToCreate))
            {
                z4[filename].ExtractWithPassword("extract", Password);
            }

            sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", filename));
            sLine = sr.ReadLine();
            sr.Close();
            repeatedLine = String.Format("This line is repeated over and over and over in file {0}", filename);
            Assert.AreEqual<string>(sLine, repeatedLine,
                    "The content of the Updated file in the zip archive is incorrect ({0})."
                );
        }


        [TestMethod]
        public void UpdateZip_UpdateFile_NewEntriesWithPassword()
        {
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_NewEntriesWithPassword.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int fileCount = _rnd.Next(3) + 4;
            string filename = null;
            int entriesAdded = 0;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, "file" + j + ".txt");
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip = new ZipFile())
            {
                // no password used here.
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                {
                    ZipEntry e = zip.AddFile(f, "");
                }
                zip.Comment = "UpdateTests::UpdateZip_UpdateFile_NewEntriesWithPassword(): This archive will be updated.";
                zip.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");

            // create and file a new file with text data
            int FileToUpdate = _rnd.Next(fileCount);
            filename = "file" + FileToUpdate + ".txt";
            int bytesRemaining = _rnd.Next(21567) + 23872;
            string repeatedLine = String.Format("**UPDATED** This file has been updated at {0}.",
                System.DateTime.Now.ToString("G"));
            using (System.IO.StreamWriter sw = System.IO.File.CreateText(filename))
            {
                do
                {
                    sw.WriteLine(repeatedLine);
                    bytesRemaining -= repeatedLine.Length;
                } while (bytesRemaining > 0);
                sw.Close();
            }

            // update that file in the zip archive
            using (ZipFile z = ZipFile.Read(ZipFileToCreate))
            {
                z.Password = "Secret!";
                z.AllowUpdates = true;
                ZipEntry e = z.UpdateFile(filename, "");
                z.Comment = "UpdateTests::UpdateZip_UpdateFile_NewEntriesWithPassword(): This archive has been updated.";
                z.Save();
            }

            using (ZipFile z2 = ZipFile.Read(ZipFileToCreate))
            {
                // no password used here.
                z2[filename].ExtractWithPassword("extract", "Secret!");
            }

            // verify the content of the updated file. 
            var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", filename));
            string sLine = sr.ReadLine();
            sr.Close();

            Assert.AreEqual<string>(sLine, repeatedLine,
                    "The content of the Updated file in the zip archive is incorrect.");
        }


        [TestMethod]
        public void UpdateZip_UpdateFile_DifferentPasswords()
        {
            string Password1 = "Whoofy1";
            string Password2 = "Furbakl1";

            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_UpdateFile_DifferentPasswords.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int fileCount = _rnd.Next(13) + 14;
            string filename = null;
            int entriesAdded = 0;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt",j));
                string Line = String.Format("This line is repeated over and over and over in file {0}",
                     System.IO.Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, Line, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile z1 = new ZipFile())
            {
                z1.Password = Password1;
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                {
                    ZipEntry e = z1.AddFile(f, "");
                }
                z1.Comment = "UpdateTests::UpdateZip_UpdateFile_DifferentPasswords(): This archive will be updated.";
                z1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                "The Zip file has the wrong number of entries.");


            // create and fill a new file with text data
            int FileToUpdate = _rnd.Next(fileCount);
            filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", FileToUpdate));
            string repeatedLine = String.Format("**UPDATED** This file has been updated at {0}.",
                System.DateTime.Now.ToString("G"));
            TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(34000) + 5000);

            // update that file in the zip archive
            using (ZipFile z2 = ZipFile.Read(ZipFileToCreate))
            {
                z2.Password = Password2;
                z2.AllowUpdates = true;
                ZipEntry e = z2.UpdateFile(filename, "");
                z2.Comment = "UpdateTests::UpdateZip_UpdateFile_DifferentPasswords(): This archive has been updated.";
                z2.Save();
            }

            filename = System.IO.Path.GetFileName(filename);
            using (ZipFile z3 = ZipFile.Read(ZipFileToCreate))
            {
                z3[filename].ExtractWithPassword("extract", Password2);
            }

            // verify the content of the updated file. 
            var sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", filename));
            string sLine = sr.ReadLine();
            sr.Close();

            Assert.AreEqual<string>(sLine, repeatedLine,
                    "The content of the Updated file in the zip archive is incorrect.");

            // extract an encrypted file and check its contents
            int FileToRead = FileToUpdate;
            while (FileToRead == FileToUpdate) { FileToRead = _rnd.Next(fileCount); }
            filename = String.Format("file{0:D3}.txt", FileToRead);
            // now extract the file from the archive
            using (ZipFile z4 = ZipFile.Read(ZipFileToCreate))
            {
                z4[filename].ExtractWithPassword("extract", Password1);
            }

            // verify the content of the NOT updated file. 
            sr = new System.IO.StreamReader(System.IO.Path.Combine("extract", filename));
            sLine = sr.ReadLine();
            sr.Close();
            repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                    System.IO.Path.GetFileName(filename));
            Assert.AreEqual<string>(sLine, repeatedLine,
                    "The content of the Updated file in the zip archive is incorrect.");

        }


        [TestMethod]
        [ExpectedException(typeof(System.InvalidOperationException))]
        public void UpdateZip_Read_Error()
        {
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateExistingZip.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int fileCount = _rnd.Next(3) + 4;
            string filename = null;
            int entriesAdded = 0;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, "file" + j + ".txt");
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                foreach (String f in filenames)
                {
                    ZipEntry e = zip.AddFile(f, "");
                }
                zip.Comment = "UpdateTests::UpdateExistingZip(): This archive will be updated.";
                zip.Save(ZipFileToCreate);
            }

            // create and file a new file with text data
            int FileToUpdate = _rnd.Next(fileCount);
            filename = "file" + FileToUpdate + ".txt";
            int bytesRemaining = _rnd.Next(21567) + 23872;
            string repeatedLine = String.Format("**UPDATED** This file has been updated at {0}.",
                System.DateTime.Now.ToString("G"));
            using (System.IO.StreamWriter sw = System.IO.File.CreateText(filename))
            {
                do
                {
                    sw.WriteLine(repeatedLine);
                    bytesRemaining -= repeatedLine.Length;
                } while (bytesRemaining > 0);
                sw.Close();
            }

            // update that file in the zip archive
            using (ZipFile z = ZipFile.Read(ZipFileToCreate))
            {
                ZipEntry e = z.UpdateFile(filename, "");
                z.Comment = "UpdateTests::UpdateExistingZip(): This archive has been updated.";
                z.Save();
            }

        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void UpdateZip_OpenForUpdate_SetIndexer_Error()
        {
            // select the name of the zip file
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "UpdateZip_OpenForUpdate_SetIndexer_Error.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            // create the files
            int fileCount = _rnd.Next(13) + 24;

            string filename = null;
            int entriesToBeAdded = 0;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesToBeAdded++;
            }

            // Add the files to the zip, save the zip
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip = new ZipFile())
            {
                String[] filenames = System.IO.Directory.GetFiles("A");
                zip.Password = "Wheeee!!";
                foreach (String f in filenames)
                    zip.AddFile(f, "");

                zip.Comment = "UpdateTests::UpdateZip_OpenForUpdate_SetIndexer_Error(): This archive will be updated.";
                zip.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesToBeAdded),
                "The Zip file has the wrong number of entries.");

            // selectively remove a few files in the zip archive
            int Threshold = _rnd.Next(fileCount - 1) + 1;
            int numRemoved = 0;
            using (ZipFile z = ZipFile.Read(ZipFileToCreate))
            {
                z.AllowUpdates = true;
                var AllFileNames = z.EntryFilenames;
                foreach (String s in AllFileNames)
                {
                    int fileNum = Int32.Parse(s.Substring(4, 3));
                    if (fileNum < Threshold)
                    {
                        // try setting the indexer to a non-null value
                        // This should fail.
                        z[s] = z[AllFileNames[0]];
                        numRemoved++;
                    }
                }

                z.Comment = "UpdateTests::UpdateZip_OpenForUpdate_SetIndexer_Error(): This archive has been updated.";
                z.Save();
            }

        }
    }
}

