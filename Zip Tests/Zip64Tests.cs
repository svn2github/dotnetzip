using System;
//using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Utils.Zip;
using Library.TestUtilities;


/// Tests for more advanced scenarios.
/// 

namespace Ionic.Utils.Zip.Tests.Zip64
{

    /// <summary>
    /// Summary description for Zip64Tests
    /// </summary>
    [TestClass]
    public class Zip64Tests
    {
        private System.Random _rnd;

        public Zip64Tests()
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
        public void CreateZip_Zip64()
        {
            Zip64Option[] Options = { Zip64Option.Never, Zip64Option.Always, Zip64Option.AsNecessary };
            for (int k = 0; k < Options.Length; k++)
            {
                string filename = null;
                System.IO.Directory.SetCurrentDirectory(TopLevelDir);
                TestContext.WriteLine("\n\n==================Trial {0}...", k);
                string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("CreateZip_Zip64-{0}.zip", k));

                TestContext.WriteLine("Creating file {0}", ZipFileToCreate);
                TestContext.WriteLine("  ZIP64 option: {0}", Options[k].ToString());
                int entries = _rnd.Next(3) + 13;

                var checksums = new Dictionary<string, string>();
                using (ZipFile zip1 = new ZipFile())
                {
                    for (int i = 0; i < entries; i++)
                    {
                        if (_rnd.Next(2) == 1)
                        {
                            filename = System.IO.Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                            TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(44000) + 5000);
                        }
                        else
                        {
                            filename = System.IO.Path.Combine(TopLevelDir, String.Format("Data{0}.txt", i));
                            TestUtilities.CreateAndFillFileText(filename, _rnd.Next(44000) + 5000);
                        }
                        zip1.AddFile(filename, "");

                        var chk = TestUtilities.ComputeChecksum(filename);
                        checksums.Add(System.IO.Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                    }

                    zip1.UseZip64WhenSaving = Options[k];
                    zip1.Comment = String.Format("This archive uses zip64 option: {0}", Options[k].ToString());
                    zip1.Save(ZipFileToCreate);
                }

                TestContext.WriteLine("---------------Reading {0}...", ZipFileToCreate);
                using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
                {
                    string extractDir = String.Format("extract{0}", k);
                    foreach (var e in zip2)
                    {
                        TestContext.WriteLine(" Entry: {0}  c({1})  unc({2})", e.FileName, e.CompressedSize, e.UncompressedSize);

                        e.Extract(extractDir);
                        filename = System.IO.Path.Combine(extractDir, e.FileName);
                        string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filename));
                        Assert.IsTrue(checksums.ContainsKey(e.FileName), "Checksum is missing");
                        Assert.AreEqual<string>(checksums[e.FileName], actualCheckString, "Checksums for ({0}) do not match.", e.FileName);
                        TestContext.WriteLine("     Checksums match ({0}).\n", actualCheckString);
                    }
                }
            }
        }



        [TestMethod]
        public void CreateZip_ConvertToZip64()
        {
            for (int k = 0; k < 2; k++)
            {
                string filename = null;
                System.IO.Directory.SetCurrentDirectory(TopLevelDir);
                TestContext.WriteLine("\n\n==================Trial {0}...", k);
                string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("CreateZip_ConvertToZip64-{0}.A.zip", k));

                TestContext.WriteLine("Creating file {0}", ZipFileToCreate);
                //int entries = _rnd.Next(13) + 32;
                int entries = 2;

                var checksums = new Dictionary<string, string>();
                using (ZipFile zip1 = new ZipFile())
                {
                    for (int i = 0; i < entries; i++)
                    {
                        if (_rnd.Next(2) == 1)
                        {
                            filename = System.IO.Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                            TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(44000) + 5000);
                        }
                        else
                        {
                            filename = System.IO.Path.Combine(TopLevelDir, String.Format("Data{0}.txt", i));
                            TestUtilities.CreateAndFillFileText(filename, _rnd.Next(44000) + 5000);
                        }
                        zip1.AddFile(filename, "");

                        var chk = TestUtilities.ComputeChecksum(filename);
                        checksums.Add(System.IO.Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                    }

                    if (k == 0)
                        zip1.Comment = String.Format("This archive uses no zip64 option.");
                    else
                    {
                        zip1.UseZip64WhenSaving = Zip64Option.Always;
                        zip1.Comment = String.Format("This archive uses Zip64Option.Always.");
                    }
                    zip1.Save(ZipFileToCreate);
                }


                Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entries,
                    "The Zip file has the wrong number of entries.");



                TestContext.WriteLine("---------------Converting {0}...", ZipFileToCreate);
                using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
                {
                    string extractDir = String.Format("extract-{0}.1", k);
                    foreach (var e in zip2)
                    {
                        TestContext.WriteLine(" {0}  c({1})  unc({2})", e.FileName, e.CompressedSize, e.UncompressedSize);

                        e.Extract(extractDir);
                        filename = System.IO.Path.Combine(extractDir, e.FileName);
                        string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filename));
                        Assert.IsTrue(checksums.ContainsKey(e.FileName), "Checksum is missing");
                        Assert.AreEqual<string>(checksums[e.FileName], actualCheckString, "Checksums for ({0}) do not match.", e.FileName);
                    }

                    if (k != 0)
                        zip2.Comment = String.Format("This archive uses no zip64 option.");
                    else
                    {
                        zip2.UseZip64WhenSaving = Zip64Option.Always;
                        zip2.Comment = String.Format("This archive uses Zip64Option.Always.");
                    }
                    zip2.Save(ZipFileToCreate.Replace(".A.", ".B."));
                }

                using ( ZipFile zip3 = ZipFile.Read(ZipFileToCreate.Replace(".A.", ".B.")))
                {
                    string extractDir = String.Format("extract-{0}.2", k);
                    foreach (var e in zip3)
                    {
                        TestContext.WriteLine(" {0}  c({1})  unc({2})", e.FileName, e.CompressedSize, e.UncompressedSize);

                        e.Extract(extractDir);
                        filename = System.IO.Path.Combine(extractDir, e.FileName);
                        string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filename));
                        Assert.IsTrue(checksums.ContainsKey(e.FileName), "Checksum is missing");
                        Assert.AreEqual<string>(checksums[e.FileName], actualCheckString, "Checksums for ({0}) do not match.", e.FileName);
                    }
                }
            }
        }



    }
}
