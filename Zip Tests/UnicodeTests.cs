// UnicodeTests.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2008, 2009 Dino Chiesa and Microsoft Corporation.  
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
// Time-stamp: <2009-July-26 23:51:07>
//
// ------------------------------------------------------------------
//
// This module defines the tests for the Unicode features in DotNetZip.
//
// ------------------------------------------------------------------

﻿
using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Zip;
using Ionic.Zip.Tests.Utilities;
using System.IO;

namespace Ionic.Zip.Tests.Unicode
{
    /// <summary>
    /// Summary description for UnicodeTests
    /// </summary>
    [TestClass]
    public class UnicodeTests : IonicTestClass
    {
        public UnicodeTests() : base() { }


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
                string Subdir = Path.Combine(TopLevelDir, "files" + k);
                Directory.CreateDirectory(Subdir);

                // create a bunch of files
                int NumFilesToCreate = _rnd.Next(8) + 4;
                string[] FilesToZip = new string[NumFilesToCreate];
                for (i = 0; i < NumFilesToCreate; i++)
                {
                    FilesToZip[i] = Path.Combine(Subdir, String.Format(formats[k], i));
                    TestUtilities.CreateAndFillFileBinary(FilesToZip[i], _rnd.Next(5000) + 2000);
                }

                //Directory.SetCurrentDirectory(Subdir);

                // create a zipfile twice, once using Unicode, once without
                for (int j = 0; j < 2; j++)
                {
                    // select the name of the zip file
                    string ZipFileToCreate = Path.Combine(TopLevelDir, String.Format("Create_UnicodeEntries_{0}_{1}.zip", k, j));
                    Assert.IsFalse(File.Exists(ZipFileToCreate), "The zip file '{0}' already exists.", ZipFileToCreate);

                    TestContext.WriteLine("\n\nFormat {0}, trial {1}.  filename: {2}...", k, j, ZipFileToCreate);
                    string dirInArchive = String.Format("{0}-{1}", Path.GetFileName(Subdir), j);

                    using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
                    {
                        zip1.UseUnicodeAsNecessary = (j == 0);
                        for (i = 0; i < FilesToZip.Length; i++)
                        {
                            // use the local filename (not fully qualified)
                            ZipEntry e = zip1.AddFile(FilesToZip[i], dirInArchive);
                            e.Comment = String.Format("This entry encoded with {0}", (j == 0) ? "unicode" : "the default code page.");
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
                                Assert.AreEqual<String>(fname, Path.GetFileName(e.FileName));
                            }
                            else
                            {
                                Assert.AreNotEqual<String>(fname, Path.GetFileName(e.FileName));
                            }
                            i++;
                        }


                        // according to the spec,
                        // unicode is not supported on the zip archive comment!
                        // But this library won't enforce that. 
                        // We will leave it up to the application.
                        // Assert.AreNotEqual<String>(OrigComment, zip2.Comment);

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
            string Subdir = Path.Combine(TopLevelDir, "files");
            Directory.CreateDirectory(Subdir);

            // create a bunch of files
            int NumFilesToCreate = _rnd.Next(18) + 14;
            string[] FilesToZip = new string[NumFilesToCreate];
            for (i = 0; i < NumFilesToCreate; i++)
            {
                int k = i % formats.Length;
                FilesToZip[i] = Path.Combine(Subdir, String.Format(formats[k], i));
                TestUtilities.CreateAndFillFileBinary(FilesToZip[i], _rnd.Next(5000) + 2000);
            }

            Directory.SetCurrentDirectory(Subdir);

            // create a zipfile twice
            for (int j = 0; j < 2; j++)
            {
                // select the name of the zip file
                string ZipFileToCreate = Path.Combine(TopLevelDir, String.Format("Create_UnicodeEntries_Mixed{0}.zip", j));
                Assert.IsFalse(File.Exists(ZipFileToCreate), "The zip file '{0}' already exists.", ZipFileToCreate);

                using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
                {
                    zip1.UseUnicodeAsNecessary = (j == 0);
                    for (i = 0; i < FilesToZip.Length; i++)
                    {
                        // use the local filename (not fully qualified)
                        ZipEntry e = zip1.AddFile(Path.GetFileName(FilesToZip[i]));
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
                string Subdir = Path.Combine(TopLevelDir, String.Format("trial{0}-files", k));
                Directory.CreateDirectory(Subdir);

                // create a bunch of files
                int NumFilesToCreate = _rnd.Next(3) + 3;
                string[] FilesToZip = new string[NumFilesToCreate];
                for (i = 0; i < NumFilesToCreate; i++)
                {
                    FilesToZip[i] = Path.Combine(Subdir, String.Format(trials[k].filenameFormat, i));
                    TestUtilities.CreateAndFillFileBinary(FilesToZip[i], _rnd.Next(5000) + 2000);
                }

                Directory.SetCurrentDirectory(Subdir);

                // select the name of the zip file
                string ZipFileToCreate = Path.Combine(TopLevelDir, String.Format("Create_WithSpecifiedCodepage_{0}_{1}.zip", k, trials[k].codepage));
                Assert.IsFalse(File.Exists(ZipFileToCreate), "The zip file '{0}' already exists.", ZipFileToCreate);

                TestContext.WriteLine("\n---------------------Creating zip....");

                using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
                {
                    zip1.ProvisionalAlternateEncoding = System.Text.Encoding.GetEncoding(trials[k].codepage);
                    for (i = 0; i < FilesToZip.Length; i++)
                    {
                        TestContext.WriteLine("adding entry {0}", FilesToZip[i]);
                        // use the local filename (not fully qualified)
                        ZipEntry e = zip1.AddFile(Path.GetFileName(FilesToZip[i]));
                        e.Comment = String.Format("This entry was encoded in the {0} codepage", trials[k].codepage);
                    }
                    zip1.Save();
                }

                TestContext.WriteLine("\n---------------------Extracting....");
                Directory.SetCurrentDirectory(TopLevelDir);

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
