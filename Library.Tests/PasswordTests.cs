using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

using Ionic.Utils.Zip;

namespace Library.Tests
{
    [TestClass]
    public class PasswordTests
    {
        private System.Random _rnd;
        System.Security.Cryptography.MD5 _md5;


        public PasswordTests()
        {
            _md5 = System.Security.Cryptography.MD5.Create();
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


        private string CheckSumToString(byte[] checksum)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (byte b in checksum)
                sb.Append(b.ToString("x2").ToLower());
            return sb.ToString();
        }

        private byte[] ComputeChecksum(string filename)
        {
            byte[] hash = null;
            using (FileStream fs = File.Open(filename, FileMode.Open))
            {
                hash= _md5.ComputeHash(fs);
            }
            return hash;
        }


        [TestMethod]
        public void TestBasicPassword()
        {
            int i, j;
            string password = "Password!";
            //int entries = 0;

            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Password1.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            string DirToZip = Path.Combine(TopLevelDir, "zipthis");
            System.IO.Directory.CreateDirectory(DirToZip);

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            int fileCount = _rnd.Next(3) + 3;
            string[] filenames= new string[fileCount];
            byte[][] checksums = new byte[fileCount][];
            for (j = 0; j < fileCount; j++)
            {
                filenames[j] = Path.Combine("zipthis", "file" + j + ".txt");

                using (StreamWriter sw = File.CreateText(filenames[j]))
                {
                    sw.WriteLine("{0}", j);
                    for (i = 0; i < 1000; i++)
                        sw.Write("{0:X2}", (byte)(_rnd.Next(255) & 0xff));
                }
                
                checksums[j]= ComputeChecksum(filenames[j]);
            }


            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.Password = password;
                zip.AddDirectory(System.IO.Path.GetFileName(DirToZip));
                zip.Save();
            }

            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, fileCount),
                    "Zip file created seems to be invalid.");

            j = 0;
            using (ZipFile zip = ZipFile.Read(ZipFileToCreate))
            {
                foreach (ZipEntry e in zip)
                {
                    e.ExtractWithPassword("unpack", true, password);
                    //bool success = Int32.TryParse(e.FileName, out j);
                    byte[] c2 = ComputeChecksum(Path.Combine("unpack", filenames[j]));
                    Assert.AreEqual<string>(CheckSumToString(checksums[j]), CheckSumToString(c2), "Checksums do not match.");
                    
                    j++;
                }
            }
            
        }
    }
}
