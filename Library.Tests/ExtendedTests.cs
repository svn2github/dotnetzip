using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Utils.Zip;
using Library.TestUtilities;


/// Tests for more advanced scenarios.
/// 

namespace Ionic.Utils.Zip.Tests.Extended
{
    /// <summary>
    /// Summary description for ExtendedTests
    /// </summary>
    [TestClass]
    public class ExtendedTests
    {
        private System.Random _rnd;

        public ExtendedTests()
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
        public void ReadZip_OpenReader()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "ReadZip_OpenReader.zip");

            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);
            var checksums = new Dictionary<string, string>();

            int fileCount = _rnd.Next(10) + 10;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D2}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
                var chk = TestUtilities.ComputeChecksum(filename);
                checksums.Add(filename, TestUtilities.CheckSumToString(chk));
            }

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(Subdir, System.IO.Path.GetFileName(Subdir));
                zip1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                      "The Zip file has the wrong number of entries.");

            // now extract the files and verify their contents
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (string eName in zip2.EntryFilenames)
                {
                    ZipEntry e1 = zip2[eName];

                    using (CrcCalculatorStream s = e1.OpenReader())
                    {
                        byte[] buffer = new byte[4096];
                        int n, totalBytesRead = 0;
                        do
                        {
                            n = s.Read(buffer, 0, buffer.Length);
                            totalBytesRead += n;
                        } while (n > 0);

                        if (s.Crc32 != e1.Crc32)
                            throw new Exception(string.Format("The Zip Entry {0} failed the CRC Check. (0x{1:X8}!=0x{2:X8})",
                                              eName, s.Crc32, e1.Crc32));

                        if (totalBytesRead != e1.UncompressedSize)
                            throw new Exception(string.Format("We read an unexpected number of bytes. ({0}, {1}!={2})",
                                              eName, totalBytesRead, e1.UncompressedSize));
                    }
                }
            }
        }



        [TestMethod]
        public void TestZip_IsZipFile()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "TestZip_IsZipFile.zip");

            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);
            var checksums = new Dictionary<string, string>();

            int fileCount = _rnd.Next(10) + 10;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D2}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
                var chk = TestUtilities.ComputeChecksum(filename);
                checksums.Add(filename, TestUtilities.CheckSumToString(chk));
            }

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(Subdir, System.IO.Path.GetFileName(Subdir));
                zip1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.IsTrue(TestUtilities.CheckZip(ZipFileToCreate, entriesAdded),
                      "The Zip file has the wrong number of entries.");

            Assert.IsTrue(ZipFile.IsZipFile(ZipFileToCreate),
                "The IsZipFile() method returned an unexpected result for an existing zip file.");

            Assert.IsTrue(!ZipFile.IsZipFile(filename),
                "The IsZipFile() method returned an unexpected result for a extant file that is not a zip.");

            filename = System.IO.Path.Combine(Subdir, String.Format("ThisFileDoesNotExist.{0:D2}.txt", _rnd.Next(2000)));
            Assert.IsTrue(!ZipFile.IsZipFile(filename),
                "The IsZipFile() method returned an unexpected result for a non-existent file.");
        }

        [TestMethod]
        public void CreateZip_SelfExtractor_Console()
        {
            string ExeFileToCreate = System.IO.Path.Combine(TopLevelDir, "TestSelfExtractor.exe");
            string TargetDirectory = System.IO.Path.Combine(TopLevelDir, "unpack");

            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);
            var checksums = new Dictionary<string, string>();

            int fileCount = _rnd.Next(10) + 10;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, "file" + j + ".txt");
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
                var chk = TestUtilities.ComputeChecksum(filename);
                checksums.Add(filename, TestUtilities.CheckSumToString(chk));
            }

            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(Subdir, System.IO.Path.GetFileName(Subdir));
                zip.Comment = "This will be embedded into a self-extracting exe";
                zip.SaveSelfExtractor(ExeFileToCreate, Ionic.Utils.Zip.SelfExtractorFlavor.ConsoleApplication);
            }

            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(ExeFileToCreate);
            psi.Arguments = TargetDirectory;
            psi.WorkingDirectory = TopLevelDir;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);
            process.WaitForExit();

            // now, compare the output in TargetDirectory with the original
            string DirToCheck = System.IO.Path.Combine(TargetDirectory, "A");
            // verify the checksum of each file matches with its brother
            foreach (string fname in System.IO.Directory.GetFiles(DirToCheck))
            {
                string expectedCheckString = checksums[fname.Replace("\\unpack", "")];
                string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(fname));
                Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", fname);
            }
        }

        [TestMethod]
        public void CreateZip_SelfExtractor_WinForms()
        {
            string ExeFileToCreate = System.IO.Path.Combine(TopLevelDir, "TestSelfExtractor-Winforms.exe");
            string TargetUnpackDirectory = System.IO.Path.Combine(TopLevelDir, "unpack");

            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);
            var checksums = new Dictionary<string, string>();

            int fileCount = _rnd.Next(10) + 10;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, "file" + j + ".txt");
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
                var chk = TestUtilities.ComputeChecksum(filename);
                checksums.Add(filename, TestUtilities.CheckSumToString(chk));
            }

            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(Subdir, System.IO.Path.GetFileName(Subdir));
                zip.Comment = "Please extract to:  " + TargetUnpackDirectory;
                zip.SaveSelfExtractor(ExeFileToCreate, Ionic.Utils.Zip.SelfExtractorFlavor.WinFormsApplication);
            }

            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(ExeFileToCreate);
            psi.Arguments = TargetUnpackDirectory;
            psi.WorkingDirectory = TopLevelDir;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);
            process.WaitForExit();

            // now, compare the output in TargetDirectory with the original
            string DirToCheck = System.IO.Path.Combine(TargetUnpackDirectory, "A");
            // verify the checksum of each file matches with its brother
            foreach (string fname in System.IO.Directory.GetFiles(DirToCheck))
            {
                string expectedCheckString = checksums[fname.Replace("\\unpack", "")];
                string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(fname));
                Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", fname);
            }
        }
    }
}
