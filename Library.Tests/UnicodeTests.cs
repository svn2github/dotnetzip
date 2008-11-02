
using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Utils.Zip;
using Library.TestUtilities;
using System.IO;

namespace Ionic.Utils.Zip.Tests.Unicode
{
    /// <summary>
    /// Summary description for UnicodeTests
    /// </summary>
    [TestClass]
    public class UnicodeTests
    {
        private System.Random _rnd;

        public UnicodeTests()
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
        public void Create_UnicodeEntries()
        {
            int i;
            string OrigComment = "This is a Unicode comment. Chinese: 弹 出 应 用 程 序 Norwegian/Danish: æøåÆØÅ. Portugese: Configurações.";
            string[] formats = {"弹出应用程序{0:D3}.bin", 
                                   "n.æøåÆØÅ{0:D3}.bin",
                               "Configurações-弹出-ÆØÅ-xx{0:D3}.bin"};

            for (int k = 0; k < formats.Length; k++)
            {
                // create the subdirectory
                string Subdir = System.IO.Path.Combine(TopLevelDir, "files" + k);
                System.IO.Directory.CreateDirectory(Subdir);

                // create a bunch of files
                int NumFilesToCreate = _rnd.Next(8) + 4;
                string[] FilesToZip = new string[NumFilesToCreate];
                for (i = 0; i < NumFilesToCreate; i++)
                {
                    FilesToZip[i] = System.IO.Path.Combine(Subdir, String.Format(formats[k], i));
                    TestUtilities.CreateAndFillFileBinary(FilesToZip[i], _rnd.Next(5000) + 2000);
                }

                System.IO.Directory.SetCurrentDirectory(Subdir);

                // create a zipfile twice, once using Unicode, once without
                for (int j = 0; j < 2; j++)
                {
                    // select the name of the zip file
                    string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("Create_UnicodeEntries_{0}_{1}.zip", k, j));
                    Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The zip file '{0}' already exists.", ZipFileToCreate);

                    TestContext.WriteLine("\n\nFormat {0}, trial {1}.  filename: {2}...", k, j, ZipFileToCreate);

                    using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
                    {
                        zip1.UseUnicodeAsNecessary = (j == 0);
                        for (i = 0; i < FilesToZip.Length; i++)
                        {
                            // use the local filename (not fully qualified)
                            ZipEntry e = zip1.AddFile(System.IO.Path.GetFileName(FilesToZip[i]));
                            e.Comment = (j == 0) ? "This entry encoded with unicode" : "This entry encoded with the default code page.";
                        }
                        zip1.Comment = OrigComment;
                        zip1.Save();
                    }

                    // Verify the number of files in the zip
                    Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                            "Incorrect number of entries in the zip file.");

                    i = 0;
                    // verify the filenames are (or are not) unicode
                    using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate, (j == 0) ? System.Text.Encoding.UTF8 : ZipFile.DefaultEncoding))
                    {
                        foreach (ZipEntry e in zip2)
                        {
                            string fname = String.Format(formats[k], i);
                            if (j == 0)
                            {
                                Assert.AreEqual<String>(fname, e.FileName);
                            }
                            else
                            {
                                Assert.AreNotEqual<String>(fname, e.FileName);
                            }
                            i++;
                        }

                        // unicode is not supported on the zip archive comment!
                        Assert.AreNotEqual<String>(OrigComment, zip2.Comment);

                    }
                }
            }
        }

        [TestMethod]
        public void Create_UnicodeEntries_Mixed()
        {
            int i;
            string[] formats = {"弹出应用程序{0:D3}.bin", 
                                   "n.æøåÆØÅ{0:D3}.bin",
                               "Configurações-弹出-ÆØÅ-xx{0:D3}.bin",
                               "file{0:D3}.bin",
                               "Â¡¢£ ¥â° €Ãƒ †œ Ñ añoAbba{0:D3.bin}", 
                               "А Б В Г Д Є Ж Ѕ З И І К Л М Н О П Р С Т Ф Х Ц Ч Ш Щ Ъ ЪІ Ь Ю ІА {0:D3}.b",
                               "Ελληνικό αλφάβητο {0:D3}.b",
                               "א ב ג ד ה ו ז ח ט י " + "{0:D3}", 
                               };

            // create the subdirectory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "files");
            System.IO.Directory.CreateDirectory(Subdir);

            // create a bunch of files
            int NumFilesToCreate = _rnd.Next(18) + 14;
            string[] FilesToZip = new string[NumFilesToCreate];
            for (i = 0; i < NumFilesToCreate; i++)
            {
                int k = i % formats.Length;
                FilesToZip[i] = System.IO.Path.Combine(Subdir, String.Format(formats[k], i));
                TestUtilities.CreateAndFillFileBinary(FilesToZip[i], _rnd.Next(5000) + 2000);
            }

            System.IO.Directory.SetCurrentDirectory(Subdir);

            // create a zipfile twice
            for (int j = 0; j < 2; j++)
            {
                // select the name of the zip file
                string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("Create_UnicodeEntries_Mixed{0}.zip", j));
                Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The zip file '{0}' already exists.", ZipFileToCreate);

                using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
                {
                    zip1.UseUnicodeAsNecessary = (j == 0);
                    for (i = 0; i < FilesToZip.Length; i++)
                    {
                        // use the local filename (not fully qualified)
                        ZipEntry e = zip1.AddFile(System.IO.Path.GetFileName(FilesToZip[i]));
                    }
                    zip1.Save();
                }

                // Verify the number of files in the zip
                Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), FilesToZip.Length,
                        "Incorrect number of entries in the zip file.");

                i = 0;
                // verify the filenames are (or are not) unicode
                using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
                {
                    foreach (ZipEntry e in zip2)
                    {
                        int k = i % formats.Length;
                        string fname = String.Format(formats[k], i);
                        if (j == 0 || k == 3)
                        {
                            Assert.AreEqual<String>(fname, e.FileName);
                        }
                        else
                        {
                            Assert.AreNotEqual<String>(fname, e.FileName);
                        }
                        i++;
                    }
                }
            }
        }


        struct CodepageTrial
        {
            public string codepage;
            public string filenameFormat;
            public bool exceptionExpected; // not all codepages will yield legal filenames for a given filenameFormat
            public CodepageTrial(string cp, string format, bool except)
            {
                codepage = cp;
                filenameFormat = format;
                exceptionExpected = except; 
            }
        }

        [TestMethod]
        public void Create_WithSpecifiedCodepage()
        {
            int i;
            CodepageTrial[] trials = {
                                     new CodepageTrial( "big5",   "弹出应用程序{0:D3}.bin", true),
                                     new CodepageTrial ("big5",   "您好{0:D3}.bin",         false),
                                     new CodepageTrial ("gb2312", "弹出应用程序{0:D3}.bin", false),
                                     new CodepageTrial ("gb2312", "您好{0:D3}.bin",         false),
                                     // insert other languages here.??
                                     };

            for (int k = 0; k < trials.Length; k++)
            {
                TestContext.WriteLine("\n---------------------Trial {0}....", k);
                TestContext.WriteLine("\n---------------------codepage: {0}....", trials[k].codepage);
                // create the subdirectory
                string Subdir = System.IO.Path.Combine(TopLevelDir, String.Format("trial{0}-files", k));
                System.IO.Directory.CreateDirectory(Subdir);

                // create a bunch of files
                int NumFilesToCreate = _rnd.Next(3) + 3;
                string[] FilesToZip = new string[NumFilesToCreate];
                for (i = 0; i < NumFilesToCreate; i++)
                {
                    FilesToZip[i] = System.IO.Path.Combine(Subdir, String.Format(trials[k].filenameFormat, i));
                    TestUtilities.CreateAndFillFileBinary(FilesToZip[i], _rnd.Next(5000) + 2000);
                }

                System.IO.Directory.SetCurrentDirectory(Subdir);

                // select the name of the zip file
                string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("Create_WithSpecifiedCodepage_{0}_{1}.zip", k, trials[k].codepage));
                Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The zip file '{0}' already exists.", ZipFileToCreate);

                TestContext.WriteLine("\n---------------------Creating zip....");

                using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
                {
                    zip1.ProvisionalAlternateEncoding = System.Text.Encoding.GetEncoding(trials[k].codepage);
                    for (i = 0; i < FilesToZip.Length; i++)
                    {
                        TestContext.WriteLine("adding entry {0}", FilesToZip[i]);
                        // use the local filename (not fully qualified)
                        ZipEntry e = zip1.AddFile(System.IO.Path.GetFileName(FilesToZip[i]));
                        e.Comment = String.Format("This entry was encoded in the {0} codepage", trials[k].codepage);
                    }
                    zip1.Save();
                }

                TestContext.WriteLine("\n---------------------Extracting....");
                System.IO.Directory.SetCurrentDirectory(TopLevelDir);

                try
                {

                    // verify the filenames are (or are not) unicode
                    using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate, System.Text.Encoding.GetEncoding(trials[k].codepage)))
                    {
                        foreach (ZipEntry e in zip2)
                        {
                            TestContext.WriteLine("found entry {0}", e.FileName);
                            e.Extract(String.Format("trial{0}-{1}-extract", k, trials[k].codepage));
                        }
                    }
                }
                catch (Exception e1)
                {
                    if (!trials[k].exceptionExpected)
                        throw new System.Exception("while extracting", e1);

                }
            }

            TestContext.WriteLine("\n---------------------Done.");


        }

    }
}
