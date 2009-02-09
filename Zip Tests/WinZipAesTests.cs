using System;
//using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;


using Ionic.Zip;
using Ionic.Zip.Tests.Utilities;

namespace Ionic.Zip.Tests.WinZipAes
{
    /// <summary>
    /// Summary description for WinZipAesTests
    /// </summary>
    [TestClass]
    public class WinZipAesTests
    {
        private System.Random _rnd;

        public WinZipAesTests()
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
        public void CreateZip_WinZipAes()
        {
            EncryptionAlgorithm[] EncOptions = { 
                                                   EncryptionAlgorithm.None, 
						 EncryptionAlgorithm.WinZipAes256,
						 EncryptionAlgorithm.WinZipAes128, 
						 EncryptionAlgorithm.PkzipWeak
	    };

            for (int k = 0; k < EncOptions.Length; k++)
            {
                string filename = null;
                string password = TestUtilities.GenerateRandomPassword();
                System.IO.Directory.SetCurrentDirectory(TopLevelDir);
                TestContext.WriteLine("\n\n==================Trial {0}...", k);
                string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("CreateZip_WinZipAes-{0}.zip", k));

                TestContext.WriteLine("Creating file {0}", ZipFileToCreate);
                TestContext.WriteLine("  Encryption: {0}", EncOptions[k].ToString());
                //int entries = _rnd.Next(11) + 8;
                int entries = 2;


                var checksums = new Dictionary<string, string>();
                using (ZipFile zip1 = new ZipFile())
                {
                    zip1.Encryption = EncOptions[k];
                    if (k != 0)
                        zip1.Password = password;
                    for (int i = 0; i < entries; i++)
                    {
                        //if (_rnd.Next(2) == 1)
                            if (true)
                            {
                            filename = System.IO.Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                            //int filesize = _rnd.Next(44000) + 5000;
                            int filesize = 20000;
                            TestUtilities.CreateAndFillFileBinary(filename, filesize);
                        }
                        else
                        {
                            filename = System.IO.Path.Combine(TopLevelDir, String.Format("Data{0}.txt", i));
                            int filesize = _rnd.Next(44000) + 5000;
                            //int filesize = 1000;
                            TestUtilities.CreateAndFillFileText(filename, filesize);
                        }
                        zip1.AddFile(filename, "");

                        var chk = TestUtilities.ComputeChecksum(filename);
                        checksums.Add(System.IO.Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                    }

                    zip1.Comment = String.Format("This archive uses Encryption: {0}, password({1})",
                        zip1.Encryption, password);
                    zip1.Save(ZipFileToCreate);
                }



                TestContext.WriteLine("---------------Reading {0}...", ZipFileToCreate);
                using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
                {
                    string extractDir = String.Format("extract{0}", k);
                    foreach (var e in zip2)
                    {
                        TestContext.WriteLine(" Entry: {0}  c({1})  unc({2})", e.FileName, e.CompressedSize, e.UncompressedSize);
                        Assert.AreEqual<EncryptionAlgorithm>(EncOptions[k], e.Encryption);
                        e.ExtractWithPassword(extractDir, password);
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
        public void CreateZip_WinZipAes_NoPassword()
        {
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("CreateZip_WinZipAes_NoPassword.zip"));

            TestContext.WriteLine("Creating file {0}", ZipFileToCreate);
            int entries = _rnd.Next(11) + 8;

            string filename = null;
            var checksums = new Dictionary<string, string>();
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Encryption = EncryptionAlgorithm.WinZipAes256;
                for (int i = 0; i < entries; i++)
                {
                    if (_rnd.Next(2) == 1)
                    {
                        filename = System.IO.Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                        int filesize = _rnd.Next(44000) + 5000;
                        TestUtilities.CreateAndFillFileBinary(filename, filesize);
                    }
                    else
                    {
                        filename = System.IO.Path.Combine(TopLevelDir, String.Format("Data{0}.txt", i));
                        int filesize = _rnd.Next(44000) + 5000;
                        TestUtilities.CreateAndFillFileText(filename, filesize);
                    }
                    zip1.AddFile(filename, "");

                    var chk = TestUtilities.ComputeChecksum(filename);
                    checksums.Add(System.IO.Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                }

                zip1.Comment = String.Format("This archive uses Encryption: {0}, no password!", zip1.Encryption);
                zip1.Save(ZipFileToCreate);
            }

        }

        [TestMethod]
        public void CreateZip_WinZipAes_DirectoriesOnly()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CreateZip_WinZipAes_ZeroLengthFiles()
        {
            throw new NotImplementedException();
        }


        [TestMethod]
        public void CreateZip_WinZipAes_NoCompression()
        {
            throw new NotImplementedException();
        }

    }
}
