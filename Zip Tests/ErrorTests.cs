// ErrorTests.cs
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
// Time-stamp: <2009-May-30 06:42:50>
//
// ------------------------------------------------------------------
//
// This module defines some "error tests" - tests that the expected errors
// or exceptions occur in DotNetZip under exceptional conditions.  These conditions include
// corrupted zip files, bad input, and so on.
//
// ------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

//using Ionic.Zip;
using Ionic.Zip.Tests.Utilities;

// disable compile-time warning: "XXX is obsolete: 'YYY'" 
#pragma warning disable 618


namespace Ionic.Zip.Tests.Error
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
        [ExpectedException(typeof(FileNotFoundException))]
        public void Error_AddFile_NonExistentFile()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Error_AddFile_NonExistentFile.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            using (Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile(ZipFileToCreate))
            {
                zip.AddFile("ThisFileDoesNotExist.txt");
                zip.Comment = "This is a Comment On the Archive";
                zip.Save();
            }
        }


        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.ZipException))]
        public void Error_Read_NullStream()
        {
            System.IO.Stream s = null;
            using (var zip = ZipFile.Read(s))
            {
                foreach (var e in zip)
                {
                    Console.WriteLine("entry: {0}", e.FileName);
                }
            }
        }


        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.ZipException))]
        public void CreateZip_AddDirectory_BlankName()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddDirectory_BlankName.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectoryByName("");
                zip.Save();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.ZipException))]
        public void CreateZip_AddFileFromString_BlankName()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddFileFromString_BlankName.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddFileFromString("", "foo", "This is the content.");
                zip.Save();
            }
        }



        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.ZipException))]
        public void Error_Extract_ExistingFileWithoutOverwrite()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Error_Extract_ExistingFileWithoutOverwrite.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            Directory.SetCurrentDirectory(TopLevelDir);

            string[] filenames = 
            {
                Path.Combine(SourceDir, "Examples\\Zipit\\bin\\Debug\\Zipit.exe"),
                Path.Combine(SourceDir, "Zip Full DLL\\bin\\Debug\\Ionic.Zip.dll"),
                Path.Combine(SourceDir, "Zip Full DLL\\bin\\Debug\\Ionic.Zip.pdb"),
                Path.Combine(SourceDir, "Zip Full DLL\\bin\\Debug\\Ionic.Zip.xml"),
                //Path.Combine(SourceDir, "AppNote.txt")
            };
            int j = 0;
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                    zip.AddFile(filenames[j], "");
                zip.Comment = "This is a Comment On the Archive";
                zip.Save();
            }

            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), filenames.Length,
                "The zip file created has the wrong number of entries.");

            // extract the first time - this should succeed
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                    zip[Path.GetFileName(filenames[j])].Extract("unpack", false);
            }

            // extract the second time - this should fail (overwrite exception)
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                    zip[Path.GetFileName(filenames[j])].Extract("unpack", false);
            }
        }



        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.ZipException))]
        public void Error_Extract_ExistingFileWithoutOverwrite_2()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Error_Extract_ExistingFileWithoutOverwrite_2.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            Directory.SetCurrentDirectory(TopLevelDir);

            string[] filenames = 
            {
                Path.Combine(SourceDir, "Examples\\Zipit\\bin\\Debug\\Zipit.exe"),
                Path.Combine(SourceDir, "Zip Full DLL\\bin\\Debug\\Ionic.Zip.dll"),
                Path.Combine(SourceDir, "Zip Full DLL\\bin\\Debug\\Ionic.Zip.pdb"),
                Path.Combine(SourceDir, "Zip Full DLL\\bin\\Debug\\Ionic.Zip.xml"),
                //Path.Combine(SourceDir, "AppNote.txt")
            };
            int j = 0;
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                    zip.AddFile(filenames[j], "");
                zip.Comment = "This is a Comment On the Archive";
                zip.Save();
            }

            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), filenames.Length,
                "The zip file created has the wrong number of entries.");

            // extract the first time - this should succeed
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                    zip[Path.GetFileName(filenames[j])].Extract("unpack", ExtractExistingFileAction.Throw);
            }

            // extract the second time - this should fail (overwrite exception)
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                    zip[Path.GetFileName(filenames[j])].Extract("unpack", ExtractExistingFileAction.Throw);
            }
        }





        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.ZipException))]
        public void Error_Read_InvalidZip()
        {
            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            Directory.SetCurrentDirectory(TopLevelDir);

            string filename =
                Path.Combine(SourceDir, "Examples\\Zipit\\bin\\Debug\\Zipit.exe");

            // try reading the invalid zipfile - this should fail
            using (ZipFile zip = ZipFile.Read(filename))
            {
                foreach (ZipEntry e in zip)
                {
                    System.Console.WriteLine("name: {0}  compressed: {1} has password?: {2}",
                        e.FileName, e.CompressedSize, e.UsesEncryption);
                }
            }
        }


        [TestMethod]
        [ExpectedException(typeof(ZipException))]
        public void Error_Save_InvalidLocation()
        {
            //string ZipFileToCreate = Path.Combine(TopLevelDir, "Error_Save_InvalidLocation.zip");

            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            Directory.SetCurrentDirectory(TopLevelDir);

            string filename =
                Path.Combine(SourceDir, "Examples\\Zipit\\bin\\Debug\\Zipit.exe");

            string badLocation = "c:\\Windows\\";
            Assert.IsTrue(Directory.Exists(badLocation));

            // add an entry to the zipfile, then try saving to a directory. this should fail
            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile(filename, "");
                zip.Save(badLocation);  // fail
            }
        }

        [TestMethod]
        public void Error_Save_NonExistentFile()
        {
            int j;
            string repeatedLine;
            string filename;

            string ZipFileToCreate = Path.Combine(TopLevelDir, "Error_Save_NonExistentFile.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = Path.Combine(TopLevelDir, "DirToZip");
            Directory.CreateDirectory(Subdir);

            int entriesAdded = 0;
            // create the files
            int NumFilesToCreate = _rnd.Next(20) + 18;
            for (j = 0; j < NumFilesToCreate; j++)
            {
                filename = Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                    Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(1800) + 1500);
                entriesAdded++;
            }

            string TempFileFolder = Path.Combine(TopLevelDir, "Temp");
            Directory.CreateDirectory(TempFileFolder);
            TestContext.WriteLine("Using {0} as the temp file folder....", TempFileFolder);
            String[] tfiles = Directory.GetFiles(TempFileFolder);
            int nTemp = tfiles.Length;
            TestContext.WriteLine("There are {0} files in the temp file folder.", nTemp);


            String[] filenames = Directory.GetFiles(Subdir);

            System.Reflection.Assembly a1 = System.Reflection.Assembly.GetExecutingAssembly();
            String myName = a1.GetName().ToString();
            string toDay = System.DateTime.Now.ToString("yyyy-MMM-dd");

            try
            {
                using (ZipFile zip = new ZipFile(ZipFileToCreate))
                {
                    zip.TempFileFolder = TempFileFolder;
                    zip.ForceNoCompression = true;

                    TestContext.WriteLine("Zipping {0} files...", filenames.Length);

                    int count = 0;
                    foreach (string fn in filenames)
                    {
                        count++;
                        TestContext.WriteLine("  {0}", fn);

                        string file = fn;

                        if (count == filenames.Length - 2)
                        {
                            file += "xx";
                            TestContext.WriteLine("(Injecting a failure...)");
                        }

                        zip.UpdateFile(file, myName + '-' + toDay + "_done");
                    }
                    TestContext.WriteLine("\n");
                    zip.Save();
                    TestContext.WriteLine("Zip Completed '{0}'", ZipFileToCreate);
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine("Zip Failed (EXPECTED): {0}", ex.Message);
            }

            tfiles = Directory.GetFiles(TempFileFolder);

            Assert.AreEqual<int>(nTemp, tfiles.Length,
                    "There are unexpected files remaining in the TempFileFolder.");
        }


        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.BadStateException))]
        public void Error_Save_NoFilename()
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
        public void Error_AddDirectory_SpecifyingFile()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "AddDirectory_SpecifyingFile.zip");

            Directory.SetCurrentDirectory(TopLevelDir);

            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            string filename = Path.Combine(SourceDir, "Examples\\Zipit\\bin\\Debug\\Zipit.exe");
            File.Copy(filename, "ThisIsAFile");

            string baddirname = Path.Combine(TopLevelDir, "ThisIsAFile");

            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(baddirname); // fail
                zip.Save(ZipFileToCreate);
            }
        }


        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void Error_AddFile_SpecifyingDirectory()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "AddFile_SpecifyingDirectory.zip");

            Directory.SetCurrentDirectory(TopLevelDir);

            Directory.CreateDirectory("ThisIsADirectory.txt");

            string badfilename = Path.Combine(TopLevelDir, "ThisIsADirectory.txt");

            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile(badfilename); // should fail
                zip.Save(ZipFileToCreate);
            }
        }

        private void IntroduceCorruption(string filename)
        {
            // now corrupt the zip archive
            using (FileStream fs = File.OpenWrite(filename))
            {
                byte[] corruption = new byte[_rnd.Next(100) + 12];
                int min = 5;
                int max = (int)fs.Length - 20;
                int OffsetForCorruption, LengthOfCorruption;

                int NumCorruptions = _rnd.Next(2) + 2;
                for (int i = 0; i < NumCorruptions; i++)
                {
                    _rnd.NextBytes(corruption);
                    OffsetForCorruption = _rnd.Next(min, max);
                    LengthOfCorruption = _rnd.Next(2) + 3;
                    fs.Seek(OffsetForCorruption, SeekOrigin.Begin);
                    fs.Write(corruption, 0, LengthOfCorruption);
                }
            }
        }


        [TestMethod]
        [ExpectedException(typeof(System.SystemException))] // not sure which exception - could be one of several.
        public void Error_ReadCorruptedZipFile_Passwords()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Read_CorruptedZipFile_Passwords.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            Directory.SetCurrentDirectory(TopLevelDir);

            // the list of filenames to add to the zip
            string[] filenames =
            {
                Path.Combine(SourceDir, "Examples\\Zipit\\bin\\Debug\\Zipit.exe"),
                Path.Combine(SourceDir, "Zip Full DLL\\bin\\Debug\\Ionic.Zip.xml"),
            };

            // passwords to use for those entries
            string[] passwords = 
            {
                    "12345678",
                    "0987654321",
            };

            // create the zipfile, adding the files
            int j = 0;
            using (ZipFile zip = new ZipFile())
            {
                for (j = 0; j < filenames.Length; j++)
                {
                    zip.Password = passwords[j];
                    zip.AddFile(filenames[j], "");
                }
                zip.Save(ZipFileToCreate);
            }

            IntroduceCorruption(ZipFileToCreate);

            try
            {
                // read the corrupted zip - this should fail in some way
                using (ZipFile zip = ZipFile.Read(ZipFileToCreate))
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


        [TestMethod]
        [ExpectedException(typeof(System.SystemException))] // not sure which exception - could be one of several.
        public void Error_ReadCorruptedZipFile()
        {
            int i;

            string ZipFileToCreate = Path.Combine(TopLevelDir, "Read_CorruptedZipFile.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string SourceDir = CurrentDir;
            for (i = 0; i < 3; i++)
                SourceDir = Path.GetDirectoryName(SourceDir);

            Directory.SetCurrentDirectory(TopLevelDir);

            // the list of filenames to add to the zip
            string[] filenames =
            {
                Path.Combine(SourceDir, "Examples\\Zipit\\bin\\Debug\\Zipit.exe"),
                Path.Combine(SourceDir, "Examples\\Unzip\\bin\\Debug\\Unzip.exe"),
                Path.Combine(SourceDir, "Zip Full DLL\\bin\\Debug\\Ionic.Zip.xml"),

            };

            // create the zipfile, adding the files
            using (ZipFile zip = new ZipFile())
            {
                for (i = 0; i < filenames.Length; i++)
                    zip.AddFile(filenames[i], "");
                zip.Save(ZipFileToCreate);
            }

            // now corrupt the zip archive
            IntroduceCorruption(ZipFileToCreate);

            try
            {
                // read the corrupted zip - this should fail in some way
                using (ZipFile zip = new ZipFile(ZipFileToCreate))
                {
                    for (i = 0; i < filenames.Length; i++)
                    {
                        ZipEntry e = zip[Path.GetFileName(filenames[i])];

                        System.Console.WriteLine("name: {0}  compressed: {1} has password?: {2}",
                            e.FileName, e.CompressedSize, e.UsesEncryption);
                        e.Extract("extract");
                    }
                }
            }
            catch (Exception exc1)
            {
                throw new SystemException("expected", exc1);
            }
        }



        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Error_AddFile_Twice()
        {
            int i;
            // select the name of the zip file
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Error_AddFile_Twice.zip");
            Assert.IsFalse(File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            // create the subdirectory
            string Subdir = Path.Combine(TopLevelDir, "files");
            Directory.CreateDirectory(Subdir);

            // create a bunch of files
            int NumFilesToCreate = _rnd.Next(23) + 14;
            string[] FilesToZip = new string[NumFilesToCreate];
            for (i = 0; i < NumFilesToCreate; i++)
                FilesToZip[i] =
                    TestUtilities.CreateUniqueFile("bin", Subdir, _rnd.Next(10000) + 5000);

            // Create the zip archive
            Directory.SetCurrentDirectory(TopLevelDir);
            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                zip1.StatusMessageTextWriter = System.Console.Out;
                String[] files = Directory.GetFiles(Subdir);
                for (i = 0; i < files.Length; i++)
                    zip1.AddFile(files[i], "files");
                zip1.Save();
            }


            // this should fail - adding the same file twice
            using (ZipFile zip2 = new ZipFile(ZipFileToCreate))
            {
                zip2.StatusMessageTextWriter = System.Console.Out;
                String[] files = Directory.GetFiles(Subdir);
                for (i = 0; i < files.Length; i++)
                    zip2.AddFile(files[i], "files");
                zip2.Save();
            }
        }


        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Selector_SelectFiles_BadNoun()
        {
            // specify an invalid string 
            Ionic.FileSelector ff = new Ionic.FileSelector("fame = *.txt");
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Selector_SelectFiles_BadSyntax01()
        {
            // specify an invalid string 
            Ionic.FileSelector ff = new Ionic.FileSelector("size = ");
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Selector_SelectFiles_BadSyntax02()
        {
            // specify an invalid string 
            Ionic.FileSelector ff = new Ionic.FileSelector("name = *.txt and");
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Selector_SelectFiles_BadSyntax03()
        {
            // specify an invalid string 
            Ionic.FileSelector ff = new Ionic.FileSelector("name = *.txt  URF ");
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Selector_SelectFiles_BadSyntax04()
        {
            // specify an invalid string 
            Ionic.FileSelector ff = new Ionic.FileSelector("name = *.txt  OR (");
        }

        [TestMethod]
        [ExpectedException(typeof(System.FormatException))]
        public void Selector_SelectFiles_BadSyntax05()
        {
            // specify an invalid string 
            Ionic.FileSelector ff = new Ionic.FileSelector("name = *.txt  OR (size = G)");
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Selector_SelectFiles_BadSyntax06()
        {
            // specify an invalid string 
            Ionic.FileSelector ff = new Ionic.FileSelector("name = *.txt  OR (size > )");
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Selector_SelectFiles_BadSyntax07()
        {
            // specify an invalid string 
            Ionic.FileSelector ff = new Ionic.FileSelector("name = *.txt  OR (size > 7800");
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Selector_SelectFiles_BadSyntax08()
        {
            // specify an invalid string 
            Ionic.FileSelector ff = new Ionic.FileSelector("name = *.txt  OR )size > 7800");
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Selector_SelectFiles_BadSyntax09()
        {
            // specify an invalid string 
            Ionic.FileSelector ff = new Ionic.FileSelector("name = *.txt and  name =");
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Selector_SelectFiles_BadSyntax10()
        {
            // specify an invalid string 
            Ionic.FileSelector ff = new Ionic.FileSelector("name == *.txt");
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Selector_SelectFiles_BadSyntax11()
        {
            // specify an invalid string 
            Ionic.FileSelector ff = new Ionic.FileSelector("name ~= *.txt");
        }
        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Selector_SelectFiles_BadSyntax12()
        {
            // specify an invalid string 
            Ionic.FileSelector ff = new Ionic.FileSelector("name @ = *.txt");
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Selector_SelectFiles_BadSyntax13()
        {
            // specify an invalid string 
            Ionic.FileSelector ff = new Ionic.FileSelector("name LIKE  *.txt");
        }
    }
}
