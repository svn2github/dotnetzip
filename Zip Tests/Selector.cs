// Selector.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa.  
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
// Time-stamp: <2009-June-25 23:43:14>
//
// ------------------------------------------------------------------
//
// This module defines tests for the File and Entry Selection stuff in
// DotNetZip. 
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
    /// Summary description for Selector
    /// </summary>
    [TestClass]
    public class Selector
    {
        private System.Random _rnd;
        
        public Selector()
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
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
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


        List<string> _FilesToRemove = new List<string>();

        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            TestUtilities.Cleanup(CurrentDir, _FilesToRemove);
        }
        #endregion



        [TestMethod]
        public void Selector_SelectFiles()
        {
            Directory.SetCurrentDirectory(TopLevelDir);

            string ZipFileToCreate = Path.Combine(TopLevelDir, "Selector_SelectFiles.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The zip file '{0}' already exists.", ZipFileToCreate);

            int count1, count2;
            int entriesAdded = 0;
            String filename = null;

            string Subdir = Path.Combine(TopLevelDir, "A");
            Directory.CreateDirectory(Subdir);

            int fileCount = _rnd.Next(33) + 33;
            for (int j = 0; j < fileCount; j++)
            {
                if (_rnd.Next(2) == 0)
                {
                    filename = Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(5000) + 5000);
                }
                else
                {
                    filename = Path.Combine(Subdir, String.Format("file{0:D3}.bin", j));
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(5000) + 5000);
                }
                entriesAdded++;
            }

            Ionic.FileSelector ff = new Ionic.FileSelector("name = *.txt");
            var list = ff.SelectFiles(Subdir);
            TestContext.WriteLine("=======================================================");
            TestContext.WriteLine("Criteria: " + ff.SelectionCriteria);
            count1 = 0;
            foreach (string s in list)
            {
                TestContext.WriteLine(Path.GetFileName(s));
                Assert.IsTrue(s.EndsWith(".txt"));
                count1++;
            }

            ff = new Ionic.FileSelector("name = *.bin");
            list = ff.SelectFiles(Subdir);
            TestContext.WriteLine("=======================================================");
            TestContext.WriteLine("Criteria: " + ff.SelectionCriteria);
            count2 = 0;
            foreach (string s in list)
            {
                TestContext.WriteLine(Path.GetFileName(s));
                Assert.IsTrue(s.EndsWith(".bin"));
                count2++;
            }
            Assert.AreEqual<Int32>(entriesAdded, count1 + count2);


            // shorthand
            ff = new Ionic.FileSelector("*.txt");
            list = ff.SelectFiles(Subdir);
            TestContext.WriteLine("=======================================================");
            TestContext.WriteLine("Criteria: " + ff.SelectionCriteria);
            count1 = 0;
            foreach (string s in list)
            {
                TestContext.WriteLine(Path.GetFileName(s));
                Assert.IsTrue(s.EndsWith(".txt"));
                count1++;
            }

            ff = new Ionic.FileSelector("*.bin");
            list = ff.SelectFiles(Subdir);
            TestContext.WriteLine("=======================================================");
            TestContext.WriteLine("Criteria: " + ff.SelectionCriteria);
            count2 = 0;
            foreach (string s in list)
            {
                TestContext.WriteLine(Path.GetFileName(s));
                Assert.IsTrue(s.EndsWith(".bin"));
                count2++;
            }
            Assert.AreEqual<Int32>(entriesAdded, count1 + count2);



            ff = new Ionic.FileSelector("size > 7500");
            list = ff.SelectFiles(Subdir);
            TestContext.WriteLine("=======================================================");
            TestContext.WriteLine("Criteria: " + ff.SelectionCriteria);
            count1 = 0;
            foreach (string s in list)
            {
                FileInfo fi = new FileInfo(s);
                TestContext.WriteLine("{0} size({1})", Path.GetFileName(s), fi.Length);
                Assert.IsTrue(fi.Length > 7500);
                count1++;
            }

            ff = new Ionic.FileSelector("size <= 7500");
            list = ff.SelectFiles(Subdir);
            TestContext.WriteLine("=======================================================");
            TestContext.WriteLine("Criteria: " + ff.SelectionCriteria);
            count2 = 0;
            foreach (string s in list)
            {
                FileInfo fi = new FileInfo(s);
                TestContext.WriteLine("{0} size({1})", Path.GetFileName(s), fi.Length);
                Assert.IsTrue(fi.Length <= 7500);
                count2++;
            }
            Assert.AreEqual<Int32>(entriesAdded, count1 + count2);


            ff = new Ionic.FileSelector("name = *.bin AND size > 7500");
            list = ff.SelectFiles(Subdir);
            TestContext.WriteLine("=======================================================");
            TestContext.WriteLine("Criteria: " + ff.SelectionCriteria);
            count1 = 0;
            foreach (string s in list)
            {
                FileInfo fi = new FileInfo(s);
                TestContext.WriteLine("{0} size({1})", Path.GetFileName(s), fi.Length);
                bool x = s.EndsWith(".bin") && fi.Length > 7500;
                Assert.IsTrue(x);
                count1++;
            }

            ff = new Ionic.FileSelector("name != *.bin  OR  size <= 7500");
            list = ff.SelectFiles(Subdir);
            TestContext.WriteLine("=======================================================");
            TestContext.WriteLine("Criteria: " + ff.SelectionCriteria);
            count2 = 0;
            foreach (string s in list)
            {
                FileInfo fi = new FileInfo(s);
                TestContext.WriteLine("{0} size({1})", Path.GetFileName(s), fi.Length);
                bool x = !s.EndsWith(".bin") || fi.Length <= 7500;
                Assert.IsTrue(x);
                count2++;
            }
            Assert.AreEqual<Int32>(entriesAdded, count1 + count2);
        }




        [TestMethod]
        public void Selector_AddSelectedFiles()
        {
            Directory.SetCurrentDirectory(TopLevelDir);

            string[] ZipFileToCreate = {
                Path.Combine(TopLevelDir, "Selector_AddSelectedFiles-1.zip"),
                Path.Combine(TopLevelDir, "Selector_AddSelectedFiles-2.zip")
            };

            Assert.IsFalse(File.Exists(ZipFileToCreate[0]), "The zip file '{0}' already exists.", ZipFileToCreate[0]);
            Assert.IsFalse(File.Exists(ZipFileToCreate[1]), "The zip file '{0}' already exists.", ZipFileToCreate[1]);

            int count1, count2;
            int entriesAdded = 0;
            String filename = null;

            string Subdir = Path.Combine(TopLevelDir, "A");
            Directory.CreateDirectory(Subdir);

            int fileCount = _rnd.Next(95) + 95;
            TestContext.WriteLine("===============================================");
            TestContext.WriteLine("Creating {0} files.", fileCount);
            for (int j = 0; j < fileCount; j++)
            {
                if (_rnd.Next(2) == 0)
                {
                    filename = Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(5000) + 5000);
                }
                else
                {
                    filename = Path.Combine(Subdir, String.Format("file{0:D3}.bin", j));
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(5000) + 5000);
                }

                // mark one third of the files as Hidden
                if (j % 3 == 0)
                {
                    File.SetAttributes(filename, FileAttributes.Hidden);
                }

                // set the last mod time on 1/4th of the files
                if (j % 4 == 0)
                {
                    DateTime x = new DateTime(1998, 4, 29);
                    File.SetLastWriteTime(filename, x);
                }

                entriesAdded++;
            }


            TestContext.WriteLine("===============================================");
            TestContext.WriteLine("Selecting files by name.", fileCount);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("name = *.txt", Subdir);
                zip1.Save(ZipFileToCreate[0]);
            }
            count1 = TestUtilities.CountEntries(ZipFileToCreate[0]);
            TestContext.WriteLine("{0} of those files were *.txt", count1);

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("name = *.bin", Subdir);
                zip1.Save(ZipFileToCreate[1]);
            }
            count2 = TestUtilities.CountEntries(ZipFileToCreate[1]);
            TestContext.WriteLine("{0} of those files were *.bin", count2);
            Assert.AreEqual<Int32>(entriesAdded, count1 + count2);



            TestContext.WriteLine("===============================================");
            TestContext.WriteLine("Selecting files by name (shorthand).", fileCount);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("*.txt", Subdir);
                zip1.Save(ZipFileToCreate[0]);
            }
            count1 = TestUtilities.CountEntries(ZipFileToCreate[0]);
            TestContext.WriteLine("{0} of those files were *.txt", count1);

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("*.bin", Subdir);
                zip1.Save(ZipFileToCreate[1]);
            }
            count2 = TestUtilities.CountEntries(ZipFileToCreate[1]);
            TestContext.WriteLine("{0} of those files were *.bin", count2);
            Assert.AreEqual<Int32>(entriesAdded, count1 + count2);



            TestContext.WriteLine("===============================================");
            TestContext.WriteLine("Selecting files by attribute.", fileCount);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("attributes = H", Subdir);
                zip1.Save(ZipFileToCreate[0]);
            }
            count1 = TestUtilities.CountEntries(ZipFileToCreate[0]);
            TestContext.WriteLine("{0} of those files were Hidden", count1);
            Assert.AreEqual<Int32>((entriesAdded - 1) / 3 + 1, count1);

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("attributes != H", Subdir);
                zip1.Save(ZipFileToCreate[1]);
            }
            count2 = TestUtilities.CountEntries(ZipFileToCreate[1]);
            TestContext.WriteLine("{0} of those files were NOT Hidden", count2);
            Assert.AreEqual<Int32>(entriesAdded, count1 + count2);


            TestContext.WriteLine("===============================================");
            TestContext.WriteLine("Selecting files by time.", fileCount);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("mtime < 2007-01-01", Subdir);
                zip1.Save(ZipFileToCreate[0]);
            }
            count1 = TestUtilities.CountEntries(ZipFileToCreate[0]);
            TestContext.WriteLine("{0} of those files were from prior to 2007-01-01.", count1);
            Assert.AreEqual<Int32>((entriesAdded - 1) / 4 + 1, count1);

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("mtime > 2007-01-01", Subdir);
                zip1.Save(ZipFileToCreate[1]);
            }
            count2 = TestUtilities.CountEntries(ZipFileToCreate[1]);
            TestContext.WriteLine("{0} of those files were from after 2007-01-01.", count2);
            Assert.AreEqual<Int32>(entriesAdded, count1 + count2);



            TestContext.WriteLine("===============================================");
            TestContext.WriteLine("Selecting files by size.", fileCount);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("size <= 7500", Subdir);
                zip1.Save(ZipFileToCreate[0]);
            }
            count1 = TestUtilities.CountEntries(ZipFileToCreate[0]);
            TestContext.WriteLine("{0} of those files were less than 7500 bytes in size", count1);

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("size > 7500", Subdir);
                zip1.Save(ZipFileToCreate[1]);
            }
            count2 = TestUtilities.CountEntries(ZipFileToCreate[1]);
            TestContext.WriteLine("{0} of those files were 7500 bytes or more in size", count2);
            Assert.AreEqual<Int32>(entriesAdded, count1 + count2);



            TestContext.WriteLine("===============================================");
            TestContext.WriteLine("Selecting files by name and size.", fileCount);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("name = *.bin AND size > 7500", Subdir);
                zip1.Save(ZipFileToCreate[0]);
            }
            count1 = TestUtilities.CountEntries(ZipFileToCreate[0]);
            TestContext.WriteLine("{0} of those files were in the first set.", count1);

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("name != *.bin  OR  size <= 7500", Subdir);
                zip1.Save(ZipFileToCreate[1]);
            }
            count2 = TestUtilities.CountEntries(ZipFileToCreate[1]);
            TestContext.WriteLine("{0} of those files were in the second set.", count2);
            Assert.AreEqual<Int32>(entriesAdded, count1 + count2);


            TestContext.WriteLine("===============================================");
            TestContext.WriteLine("Selecting files by name, size and attributes.", fileCount);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("name = *.bin AND size > 7500 and attributes = H", Subdir);
                zip1.Save(ZipFileToCreate[0]);
            }
            count1 = TestUtilities.CountEntries(ZipFileToCreate[0]);
            TestContext.WriteLine("{0} of those files were in the first set.", count1);

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("name != *.bin  OR  size <= 7500 or attributes != H", Subdir);
                zip1.Save(ZipFileToCreate[1]);
            }
            count2 = TestUtilities.CountEntries(ZipFileToCreate[1]);
            TestContext.WriteLine("{0} of those files were in the second set.", count2);
            Assert.AreEqual<Int32>(entriesAdded, count1 + count2);


            TestContext.WriteLine("===============================================");
            TestContext.WriteLine("Selecting files by name, size, time and attributes.", fileCount);
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("name = *.bin AND size > 7500 and mtime < 2007-01-01 and attributes = H", Subdir);
                zip1.Save(ZipFileToCreate[0]);
            }
            count1 = TestUtilities.CountEntries(ZipFileToCreate[0]);
            TestContext.WriteLine("{0} of those files were in the first set.", count1);

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("name != *.bin  OR  size <= 7500 or mtime > 2007-01-01 or attributes != H", Subdir);
                zip1.Save(ZipFileToCreate[1]);
            }
            count2 = TestUtilities.CountEntries(ZipFileToCreate[1]);
            TestContext.WriteLine("{0} of those files were in the second set.", count2);
            Assert.AreEqual<Int32>(entriesAdded, count1 + count2);

        }




        [TestMethod]
        public void Selector_SelectEntries()
        {
            Directory.SetCurrentDirectory(TopLevelDir);

            string ZipFileToCreate = Path.Combine(TopLevelDir, "Selector_SelectEntries.zip");

            Assert.IsFalse(File.Exists(ZipFileToCreate), "The zip file '{0}' already exists.", ZipFileToCreate);

            //int count1, count2;
            int entriesAdded = 0;
            String filename = null;

            string Subdir = Path.Combine(TopLevelDir, "A");
            Directory.CreateDirectory(Subdir);

            int fileCount = _rnd.Next(33) + 33;
            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Files being added to the zip:");
            for (int j = 0; j < fileCount; j++)
            {
                if (_rnd.Next(2) == 0)
                {
                    filename = Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(5000) + 5000);
                }
                else
                {
                    filename = Path.Combine(Subdir, String.Format("file{0:D3}.bin", j));
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(5000) + 5000);
                }
                TestContext.WriteLine(Path.GetFileName(filename));
                entriesAdded++;
            }


            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Creating zip...");
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(Subdir, "");
                zip1.Save(ZipFileToCreate);
            }
            Assert.AreEqual<Int32>(entriesAdded, TestUtilities.CountEntries(ZipFileToCreate));



            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Reading zip...");
            using (ZipFile zip1 = ZipFile.Read(ZipFileToCreate))
            {
                var selected1 = zip1.SelectEntries("name = *.txt");
                var selected2 = zip1.SelectEntries("name = *.bin");
                TestContext.WriteLine("Text files:");
                foreach (ZipEntry e in selected1)
                {
                    TestContext.WriteLine(e.FileName);
                }
                Assert.AreEqual<Int32>(entriesAdded, selected1.Count + selected2.Count);
            }


            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Reading zip, using shorthand filters...");
            using (ZipFile zip1 = ZipFile.Read(ZipFileToCreate))
            {
                var selected1 = zip1.SelectEntries("*.txt");
                var selected2 = zip1.SelectEntries("*.bin");
                TestContext.WriteLine("Text files:");
                foreach (ZipEntry e in selected1)
                {
                    TestContext.WriteLine(e.FileName);
                }
                Assert.AreEqual<Int32>(entriesAdded, selected1.Count + selected2.Count);
            }

        }


        [TestMethod]
        public void Selector_SelectEntries_Spaces()
        {
            Directory.SetCurrentDirectory(TopLevelDir);

            string ZipFileToCreate = Path.Combine(TopLevelDir, "Selector_SelectEntries_Spaces.zip");

            Assert.IsFalse(File.Exists(ZipFileToCreate), "The zip file '{0}' already exists.", ZipFileToCreate);

            //int count1, count2;
            int entriesAdded = 0;
            String filename = null;

            string Subdir = Path.Combine(TopLevelDir, "A");
            Directory.CreateDirectory(Subdir);

            int fileCount = _rnd.Next(44) + 44;
            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Files being added to the zip:");
            for (int j = 0; j < fileCount; j++)
            {
                string space = (_rnd.Next(2) == 0) ? " " : "";
                if (_rnd.Next(2) == 0)
                {
                    filename = Path.Combine(Subdir, String.Format("file{1}{0:D3}.txt", j, space));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(5000) + 5000);
                }
                else
                {
                    filename = Path.Combine(Subdir, String.Format("file{1}{0:D3}.bin", j, space));
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(5000) + 5000);
                }
                TestContext.WriteLine(Path.GetFileName(filename));
                entriesAdded++;
            }


            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Creating zip...");
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(Subdir, "");
                zip1.Save(ZipFileToCreate);
            }
            Assert.AreEqual<Int32>(entriesAdded, TestUtilities.CountEntries(ZipFileToCreate));



            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Reading zip...");
            using (ZipFile zip1 = ZipFile.Read(ZipFileToCreate))
            {
                var selected1 = zip1.SelectEntries("name = *.txt");
                var selected2 = zip1.SelectEntries("name = *.bin");
                TestContext.WriteLine("Text files:");
                foreach (ZipEntry e in selected1)
                {
                    TestContext.WriteLine(e.FileName);
                }
                Assert.AreEqual<Int32>(entriesAdded, selected1.Count + selected2.Count);
            }


            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Reading zip, using name patterns that contain spaces...");
            string[] selectionStrings = { "name = '* *.txt'", 
                                           "name = '* *.bin'", 
                                           "name = *.txt and name != '* *.txt'",
                                           "name = *.bin and name != '* *.bin'",
                                       };
            int count = 0;
            using (ZipFile zip1 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string selectionCriteria in selectionStrings)
                {
                    var selected1 = zip1.SelectEntries(selectionCriteria);
                    count += selected1.Count;
                    TestContext.WriteLine("  For criteria ({0}), found {1} files.", selectionCriteria, selected1.Count);
                }
            }
            Assert.AreEqual<Int32>(entriesAdded, count);

        }

        [TestMethod]
        public void Selector_RemoveSelectedEntries_Spaces()
        {
            Directory.SetCurrentDirectory(TopLevelDir);

            string ZipFileToCreate = Path.Combine(TopLevelDir, "Selector_RemoveSelectedEntries_Spaces.zip");

            Assert.IsFalse(File.Exists(ZipFileToCreate), "The zip file '{0}' already exists.", ZipFileToCreate);

            //int count1, count2;
            int entriesAdded = 0;
            String filename = null;

            string Subdir = Path.Combine(TopLevelDir, "A");
            Directory.CreateDirectory(Subdir);

            int fileCount = _rnd.Next(44) + 44;
            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Files being added to the zip:");
            for (int j = 0; j < fileCount; j++)
            {
                string space = (_rnd.Next(2) == 0) ? " " : "";
                if (_rnd.Next(2) == 0)
                {
                    filename = Path.Combine(Subdir, String.Format("file{1}{0:D3}.txt", j, space));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(5000) + 5000);
                }
                else
                {
                    filename = Path.Combine(Subdir, String.Format("file{1}{0:D3}.bin", j, space));
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(5000) + 5000);
                }
                TestContext.WriteLine(Path.GetFileName(filename));
                entriesAdded++;
            }


            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Creating zip...");
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(Subdir, "");
                zip1.Save(ZipFileToCreate);
            }
            Assert.AreEqual<Int32>(entriesAdded, TestUtilities.CountEntries(ZipFileToCreate));


            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Reading zip, using name patterns that contain spaces...");
            string[] selectionStrings = { "name = '* *.txt'", 
                                           "name = '* *.bin'", 
                                           "name = *.txt and name != '* *.txt'",
                                           "name = *.bin and name != '* *.bin'",
                                       };
            foreach (string selectionCriteria in selectionStrings)
            {
                using (ZipFile zip1 = ZipFile.Read(ZipFileToCreate))
                {
                    var selected1 = zip1.SelectEntries(selectionCriteria);
                    zip1.RemoveEntries(selected1);
                    TestContext.WriteLine("for pattern {0}, Removed {1} entries", selectionCriteria, selected1.Count);
                    zip1.Save();
                }

            }

            Assert.AreEqual<Int32>(0, TestUtilities.CountEntries(ZipFileToCreate));
        }


        [TestMethod]
        public void Selector_RemoveSelectedEntries2()
        {
            Directory.SetCurrentDirectory(TopLevelDir);

            string ZipFileToCreate = Path.Combine(TopLevelDir, "Selector_RemoveSelectedEntries2.zip");

            Assert.IsFalse(File.Exists(ZipFileToCreate), "The zip file '{0}' already exists.", ZipFileToCreate);

            //int count1, count2;
            int entriesAdded = 0;
            String filename = null;

            string Subdir = Path.Combine(TopLevelDir, "A");
            Directory.CreateDirectory(Subdir);

            int fileCount = _rnd.Next(44) + 44;
            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Files being added to the zip:");
            for (int j = 0; j < fileCount; j++)
            {
                string space = (_rnd.Next(2) == 0) ? " " : "";
                if (_rnd.Next(2) == 0)
                {
                    filename = Path.Combine(Subdir, String.Format("file{1}{0:D3}.txt", j, space));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(5000) + 5000);
                }
                else
                {
                    filename = Path.Combine(Subdir, String.Format("file{1}{0:D3}.bin", j, space));
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(5000) + 5000);
                }
                TestContext.WriteLine(Path.GetFileName(filename));
                entriesAdded++;
            }


            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Creating zip...");
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(Subdir, "");
                zip1.Save(ZipFileToCreate);
            }
            Assert.AreEqual<Int32>(entriesAdded, TestUtilities.CountEntries(ZipFileToCreate));


            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Reading zip, using name patterns that contain spaces...");
            string[] selectionStrings = { "name = '* *.txt'", 
                                           "name = '* *.bin'", 
                                           "name = *.txt and name != '* *.txt'",
                                           "name = *.bin and name != '* *.bin'",
                                       };
            foreach (string selectionCriteria in selectionStrings)
            {
                using (ZipFile zip1 = ZipFile.Read(ZipFileToCreate))
                {
                    var selected1 = zip1.SelectEntries(selectionCriteria);
                    ZipEntry[] entries = new ZipEntry[selected1.Count];
                    selected1.CopyTo(entries, 0);
                    string[] names = Array.ConvertAll(entries, x => x.FileName);
                    zip1.RemoveEntries(names);
                    TestContext.WriteLine("for pattern {0}, Removed {1} entries", selectionCriteria, selected1.Count);
                    zip1.Save();
                }

            }

            Assert.AreEqual<Int32>(0, TestUtilities.CountEntries(ZipFileToCreate));
        }



        [TestMethod]
        public void Selector_SelectEntries_Subdirs()
        {
            Directory.SetCurrentDirectory(TopLevelDir);

            string ZipFileToCreate = Path.Combine(TopLevelDir, "Selector_SelectFiles_Subdirs.zip");

            Assert.IsFalse(File.Exists(ZipFileToCreate), "The zip file '{0}' already exists.", ZipFileToCreate);

            int count1, count2;

            string Fodder = Path.Combine(TopLevelDir, "fodder");
            Directory.CreateDirectory(Fodder);


            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Creating files...");
            int entries = 0;
            int i = 0;
            int subdirCount = _rnd.Next(17) + 9;
            //int subdirCount = _rnd.Next(3) + 2;
            var FileCount = new Dictionary<string, int>();

            var checksums = new Dictionary<string, string>();
            // I don't actually verify the checksums in this method...


            for (i = 0; i < subdirCount; i++)
            {
                string SubdirShort = new System.String(new char[] { (char)(i + 65) });
                string Subdir = Path.Combine(Fodder, SubdirShort);
                Directory.CreateDirectory(Subdir);

                int filecount = _rnd.Next(8) + 8;
                //int filecount = _rnd.Next(2) + 2;
                FileCount[SubdirShort] = filecount;
                for (int j = 0; j < filecount; j++)
                {
                    string filename = String.Format("file{0:D4}.x", j);
                    string fqFilename = Path.Combine(Subdir, filename);
                    TestUtilities.CreateAndFillFile(fqFilename, _rnd.Next(1000) + 1000);

                    var chk = TestUtilities.ComputeChecksum(fqFilename);
                    var s = TestUtilities.CheckSumToString(chk);
                    var t1 = Path.GetFileName(Fodder);
                    var t2 = Path.Combine(t1, SubdirShort);
                    var key = Path.Combine(t2, filename);
                    key = TestUtilities.TrimVolumeAndSwapSlashes(key);
                    TestContext.WriteLine("chk[{0}]= {1}", key, s);
                    checksums.Add(key, s);
                    entries++;
                }
            }

            Directory.SetCurrentDirectory(TopLevelDir);

            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Creating zip ({0} entries in {1} subdirs)...", entries, subdirCount);
            // add all the subdirectories into a new zip
            using (ZipFile zip1 = new ZipFile())
            {
                // add all of those subdirectories (A, B, C...) into the root in the zip archive
                zip1.AddDirectory(Fodder, "");
                zip1.Save(ZipFileToCreate);
            }
            Assert.AreEqual<Int32>(entries, TestUtilities.CountEntries(ZipFileToCreate));


            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Selecting entries by directory...");
            count1 = 0;
            using (ZipFile zip1 = ZipFile.Read(ZipFileToCreate))
            {
                for (i = 0; i < subdirCount; i++)
                {
                    string dirInArchive = new System.String(new char[] { (char)(i + 65) });
                    var selected1 = zip1.SelectEntries("*.*", dirInArchive);
                    count1 += selected1.Count;
                    TestContext.WriteLine("--------------\nfiles in dir {0} ({1}):",
                      dirInArchive, selected1.Count);
                    foreach (ZipEntry e in selected1)
                        TestContext.WriteLine(e.FileName);
                }
                Assert.AreEqual<Int32>(entries, count1);
            }


            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Selecting entries by directory and size...");
            count1 = 0;
            count2 = 0;
            using (ZipFile zip1 = ZipFile.Read(ZipFileToCreate))
            {
                for (i = 0; i < subdirCount; i++)
                {
                    string dirInArchive = new System.String(new char[] { (char)(i + 65) });
                    var selected1 = zip1.SelectEntries("size > 1500", dirInArchive);
                    count1 += selected1.Count;
                    TestContext.WriteLine("--------------\nfiles in dir {0} ({1}):",
                      dirInArchive, selected1.Count);
                    foreach (ZipEntry e in selected1)
                        TestContext.WriteLine(e.FileName);
                }

                var selected2 = zip1.SelectEntries("size <= 1500");
                count2 = selected2.Count;
                Assert.AreEqual<Int32>(entries, count1 + count2 - subdirCount);
            }

        }



        [TestMethod]
        public void Selector_SelectEntries_Fullpath()
        {
            Directory.SetCurrentDirectory(TopLevelDir);

            string ZipFileToCreate = Path.Combine(TopLevelDir, "Selector_SelectFiles_Fullpath.zip");

            Assert.IsFalse(File.Exists(ZipFileToCreate), "The zip file '{0}' already exists.", ZipFileToCreate);

            int count1, count2;

            string Fodder = Path.Combine(TopLevelDir, "fodder");
            Directory.CreateDirectory(Fodder);


            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Creating files...");
            int entries = 0;
            int i = 0;
            int subdirCount = _rnd.Next(17) + 9;
            //int subdirCount = _rnd.Next(3) + 2;
            var FileCount = new Dictionary<string, int>();

            var checksums = new Dictionary<string, string>();
            // I don't actually verify the checksums in this method...


            for (i = 0; i < subdirCount; i++)
            {
                string SubdirShort = new System.String(new char[] { (char)(i + 65) });
                string Subdir = Path.Combine(Fodder, SubdirShort);
                Directory.CreateDirectory(Subdir);

                int filecount = _rnd.Next(8) + 8;
                //int filecount = _rnd.Next(2) + 2;
                FileCount[SubdirShort] = filecount;
                for (int j = 0; j < filecount; j++)
                {
                    string filename = String.Format("file{0:D4}.x", j);
                    string fqFilename = Path.Combine(Subdir, filename);
                    TestUtilities.CreateAndFillFile(fqFilename, _rnd.Next(1000) + 1000);

                    var chk = TestUtilities.ComputeChecksum(fqFilename);
                    var s = TestUtilities.CheckSumToString(chk);
                    var t1 = Path.GetFileName(Fodder);
                    var t2 = Path.Combine(t1, SubdirShort);
                    var key = Path.Combine(t2, filename);
                    key = TestUtilities.TrimVolumeAndSwapSlashes(key);
                    TestContext.WriteLine("chk[{0}]= {1}", key, s);
                    checksums.Add(key, s);
                    entries++;
                }
            }

            Directory.SetCurrentDirectory(TopLevelDir);

            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Creating zip ({0} entries in {1} subdirs)...", entries, subdirCount);
            // add all the subdirectories into a new zip
            using (ZipFile zip1 = new ZipFile())
            {
                // add all of those subdirectories (A, B, C...) into the root in the zip archive
                zip1.AddDirectory(Fodder, "");
                zip1.Save(ZipFileToCreate);
            }
            Assert.AreEqual<Int32>(entries, TestUtilities.CountEntries(ZipFileToCreate));


            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Selecting entries by full path...");
            count1 = 0;
            using (ZipFile zip1 = ZipFile.Read(ZipFileToCreate))
            {
                for (i = 0; i < subdirCount; i++)
                {
                    string dirInArchive = new System.String(new char[] { (char)(i + 65) });
                    var selected1 = zip1.SelectEntries(Path.Combine(dirInArchive, "*.*"));
                    count1 += selected1.Count;
                    TestContext.WriteLine("--------------\nfiles in dir {0} ({1}):",
                      dirInArchive, selected1.Count);
                    foreach (ZipEntry e in selected1)
                        TestContext.WriteLine(e.FileName);
                }
                Assert.AreEqual<Int32>(entries, count1);
            }


            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Selecting entries by directory and size...");
            count1 = 0;
            count2 = 0;
            using (ZipFile zip1 = ZipFile.Read(ZipFileToCreate))
            {
                for (i = 0; i < subdirCount; i++)
                {
                    string dirInArchive = new System.String(new char[] { (char)(i + 65) });
                    string pathCriterion = String.Format("name = {0}",
                             Path.Combine(dirInArchive, "*.*"));
                    string combinedCriterion = String.Format("size > 1500  AND {0}", pathCriterion);

                    var selected1 = zip1.SelectEntries(combinedCriterion, dirInArchive);
                    count1 += selected1.Count;
                    TestContext.WriteLine("--------------\nfiles in ({0}) ({1} entries):",
                      combinedCriterion,
                      selected1.Count);
                    foreach (ZipEntry e in selected1)
                        TestContext.WriteLine(e.FileName);
                }

                var selected2 = zip1.SelectEntries("size <= 1500");
                count2 = selected2.Count;
                Assert.AreEqual<Int32>(entries, count1 + count2 - subdirCount);
            }

        }


        [TestMethod]
        public void Selector_SelectFiles_GoodSyntax01()
        {
            string[] criteria = {
                                    "name = *.txt  OR (size > 7800)",
                                    "name = *.harvey  OR  (size > 7800  and attributes = H)",
                                    "(name = *.harvey)  OR  (size > 7800  and attributes = H)",
                                    "(name = *.xls)  and (name != *.xls)  OR  (size > 7800  and attributes = H)",
                                    "(name = '*.xls')",
                                };

            foreach (string s in criteria)
            {
                var ff = new Ionic.FileSelector(s);
            }
        }

    }
}
