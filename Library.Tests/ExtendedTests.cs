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

    public class XTWFND : System.Xml.XmlTextWriter
    {
        public XTWFND(System.IO.TextWriter w) : base(w) { Formatting = System.Xml.Formatting.Indented; }
        public override void WriteStartDocument() { }
    }

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



        static System.IO.MemoryStream StringToMemoryStream(string s)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            int byteCount = enc.GetByteCount(s.ToCharArray(), 0, s.Length);
            byte[] ByteArray = new byte[byteCount];
            int bytesEncodedCount = enc.GetBytes(s, 0, s.Length, ByteArray, 0);
            System.IO.MemoryStream ms = new System.IO.MemoryStream(ByteArray);
            return ms;
        }



        static String StreamToString(System.IO.Stream s)
        {
            string result = null;
            using (var f = new System.IO.StreamReader(s))
            {
                result = f.ReadToEnd();
            }
            return result;
        }


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
                foreach (string eName in zip2.EntryFileNames)
                {
                    ZipEntry e1 = zip2[eName];

                    if (!e1.IsDirectory)
                    {
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
                                throw new Exception(string.Format("The Entry {0} failed the CRC Check. (0x{1:X8}!=0x{2:X8})",
                                  eName, s.Crc32, e1.Crc32));

                            if (totalBytesRead != e1.UncompressedSize)
                                throw new Exception(string.Format("We read an unexpected number of bytes. ({0}, {1}!={2})",
                                  eName, totalBytesRead, e1.UncompressedSize));
                        }
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
        public void Extract_AfterSaveNoDispose()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Extract_AfterSaveNoDispose.zip");
            string InputString = "<bob><YourUncle/></bob>";

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                System.IO.MemoryStream ms1 = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(InputString));
                zip1.AddFileStream("Test.xml", "Woo", ms1);
                zip1.Save();

                System.IO.MemoryStream ms2 = new System.IO.MemoryStream();
                zip1.Extract("Woo/Test.xml", ms2);
                ms2.Seek(0, System.IO.SeekOrigin.Begin);

                var sw1 = new System.IO.StringWriter();
                var w1 = new XTWFND(sw1);

                var d1 = new System.Xml.XmlDocument();
                d1.Load(ms2);
                d1.Save(w1);

                var sw2 = new System.IO.StringWriter();
                var w2 = new XTWFND(sw2);
                var d2 = new System.Xml.XmlDocument();
                d2.Load(StringToMemoryStream(InputString));
                d2.Save(w2);

                Assert.AreEqual<String>(sw2.ToString(), sw1.ToString(), "Unexpected value on extract ({0}).", sw1.ToString());
            }

        }



        [TestMethod]
        public void Test_AddUpdateFileStream()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Test_AddUpdateFileStream.zip");
            string[] InputStrings = new string[] { 
                    TestUtilities.LoremIpsum.Substring(0, 90),
                    TestUtilities.LoremIpsum.Substring(240, 80)};

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
            string password = TestUtilities.GenerateRandomPassword();
            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                zip1.Password = password;
                for (int i = 0; i < InputStrings.Length; i++)
                {
                    //var ms1 = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(InputStrings[i]));
                    zip1.AddFileFromString(String.Format("Lorem{0}.txt", i + 1), "", InputStrings[i]);
                }
                zip1.Save();

                zip1["Lorem2.txt"].Password = password;
                string output = StreamToString(zip1["Lorem2.txt"].OpenReader());

                Assert.AreEqual<String>(output, InputStrings[1], "Unexpected value on extract.");

                zip1["Lorem1.txt"].Password = password;
                System.IO.Stream s = zip1["Lorem1.txt"].OpenReader();
                output = StreamToString(s);

                Assert.AreEqual<String>(output, InputStrings[0], "Unexpected value on extract.");
            }

            string UpdateString = "Nothing to see here.  Move along folks!  Move Along!";
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                //zip2.Password = password;
                var ms1 = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(UpdateString));
                zip2.UpdateFileStream("Lorem1.txt", "", ms1);
                zip2.Save();

                string output = StreamToString(zip2["Lorem1.txt"].OpenReader());

                Assert.AreEqual<String>(output, UpdateString, "Unexpected value on extract.");
            }
        }


        [TestMethod]
        public void Test_AddDirectoryByName()
        {
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            for (int n = 1; n <= 10; n++)
            {
                var DirsAdded = new System.Collections.Generic.List<String>();
                string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("Test_AddDirectoryByName{0:N2}.zip", n));
                using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
                {
                    for (int i = 0; i < n; i++)
                    {
                        // create an arbitrary directory name, add it to the zip archive
                        string DirName = TestUtilities.GenerateRandomName(24);
                        zip1.AddDirectoryByName(DirName);
                        DirsAdded.Add(DirName);
                    }
                    zip1.Save();
                }


                int dirCount = 0;
                using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
                {
                    foreach (var e in zip2)
                    {
                        TestContext.WriteLine("dir: {0}", e.FileName);
                        Assert.IsTrue(DirsAdded.Contains(e.FileName));
                        Assert.IsTrue(e.IsDirectory);
                        dirCount++;
                    }
                }
                Assert.AreEqual<int>(n, dirCount);
            }
        }



        [TestMethod]
        public void Test_AddDirectoryByName_Nested()
        {
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            var DirsAdded = new System.Collections.Generic.List<String>();
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Test_AddDirectoryByName_Nested.zip");
            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                for (int n = 1; n <= 14; n++)
                {
                    string DirName = n.ToString();
                    for (int i = 0; i < n; i++)
                    {
                        // create an arbitrary directory name, add it to the zip archive
                        DirName = System.IO.Path.Combine(DirName, TestUtilities.GenerateRandomAsciiString(11));
                    }
                    zip1.AddDirectoryByName(DirName);
                    DirsAdded.Add(DirName);
                }
                zip1.Save();
            }

            int dirCount = 0;
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (var e in zip2)
                {
                    TestContext.WriteLine("dir: {0}", e.FileName);
                    Assert.IsTrue(DirsAdded.Contains(e.FileName.Replace("/", "\\")));
                    Assert.IsTrue(e.IsDirectory);
                    dirCount++;
                }
            }
            Assert.AreEqual<int>(DirsAdded.Count, dirCount);
        }


        [TestMethod]
        public void Test_AddDirectoryByName_WithFiles()
        {
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            var DirsAdded = new System.Collections.Generic.List<String>();
            string password = TestUtilities.GenerateRandomPassword();
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Test_AddDirectoryByName_WithFiles.zip");
            using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
            {
                string DirName = null;
                int T = 3 + _rnd.Next(4);
                for (int n = 0; n < T; n++)
                {
                    DirName = (n==0) ? "root" : 
                        System.IO.Path.Combine(DirName, TestUtilities.GenerateRandomAsciiString(8));

                    zip1.AddDirectoryByName(DirName);
                    DirsAdded.Add(DirName);
                    if (n % 2 == 0) zip1.Password = password;
                    zip1.AddFileFromString( new System.String((char)(n + 48), 3) + ".txt", DirName, "Hello, Dolly!");
                    if (n % 2 == 0) zip1.Password = null;
                }
                zip1.Save();
            }

            int entryCount = 0;
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (var e in zip2)
                {
                    TestContext.WriteLine("e: {0}", e.FileName);
                    if (e.IsDirectory)
                        Assert.IsTrue(DirsAdded.Contains(e.FileName.Replace("/", "\\")));
                    else
                    {
                        if ((entryCount-1) % 4 == 0) e.Password = password;
                        string output = StreamToString(e.OpenReader());
                        Assert.AreEqual<string>("Hello, Dolly!", output);
                    }
                    entryCount++;
                }
            }
            Assert.AreEqual<int>(DirsAdded.Count*2, entryCount);
        }



        [TestMethod]
        public void Extract_SelfExtractor_Console()
        {
            string ExeFileToCreate = System.IO.Path.Combine(TopLevelDir, "TestSelfExtractor.exe");
            string TargetDirectory = System.IO.Path.Combine(TopLevelDir, "unpack");
            string ReadmeString = "Hey there!  This zipfile entry was created directly from a string in application code.";

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
                System.IO.MemoryStream ms1 = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(ReadmeString));
                zip.AddFileStream("Readme.txt", "", ms1);
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
                string originalName = fname.Replace("\\unpack", "");
                if (checksums.ContainsKey(originalName))
                {
                    string expectedCheckString = checksums[originalName];
                    string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(fname));
                    Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", fname);
                }
                else
                    Assert.AreEqual<string>("Readme.txt", originalName);

            }
        }


        int _progressEventCalls;
        int _cancelIndex;
        public void SaveProgress(object sender, SaveProgressEventArgs e)
        {
            _progressEventCalls++;
            TestContext.WriteLine("Saved: {0} ({1}/{2})", e.NameOfLatestEntry, e.EntriesSaved, e.EntriesTotal);
            if (_cancelIndex == _progressEventCalls)
            {
                e.Cancel = true;
                TestContext.WriteLine("Cancelling...");
            }
        }


        public void ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            _progressEventCalls++;
            TestContext.WriteLine("Extracted: {0} ({1}/{2})", e.NameOfLatestEntry, e.EntriesExtracted, e.EntriesTotal);
            if (_cancelIndex == _progressEventCalls)
            {
                e.Cancel = true;
                TestContext.WriteLine("Cancelling...");
            }
        }


        [TestMethod]
        public void Create_WithEvents()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Create_WithEvents.zip");
            string TargetDirectory = System.IO.Path.Combine(TopLevelDir, "unpack");

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "EventTest");
            System.IO.Directory.CreateDirectory(DirToZip);

            int subdirCount = 0;
            int entriesAdded = TestUtilities.GenerateFilesOneLevelDeep(TestContext, "Create_SaveCancellation", DirToZip, out subdirCount);

            _progressEventCalls = 0;
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.SaveProgress += SaveProgress;
                zip1.Comment = "This is the comment on the zip archive.";
                zip1.AddDirectory(DirToZip, System.IO.Path.GetFileName(DirToZip));
                zip1.Save(ZipFileToCreate);
            }

            Assert.AreEqual<Int32>(_progressEventCalls, entriesAdded + subdirCount + 1,
                   "The number of Entries added is not equal to the number of entries saved.");

            _progressEventCalls = 0;
            _cancelIndex = -1; // don't cancel this Extract
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                zip2.ExtractProgress += ExtractProgress;
                zip2.ExtractAll(TargetDirectory);
            }

            Assert.AreEqual<Int32>(_progressEventCalls, entriesAdded + subdirCount + 1,
                   "The number of Entries added is not equal to the number of entries extracted.");

        }




        [TestMethod]
        public void Create_SaveCancellation()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Create_SaveCancellation.zip");

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "EventTest");
            System.IO.Directory.CreateDirectory(DirToZip);

            int subdirCount = 0;
            int entriesAdded = TestUtilities.GenerateFilesOneLevelDeep(TestContext, "Create_SaveCancellation", DirToZip, out subdirCount);

            _cancelIndex = entriesAdded - _rnd.Next(entriesAdded / 2);
            _progressEventCalls = 0;
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.SaveProgress += SaveProgress;
                zip1.Comment = "The save on this zip archive will be canceled.";
                zip1.AddDirectory(DirToZip, System.IO.Path.GetFileName(DirToZip));
                zip1.Save(ZipFileToCreate);
            }

            Assert.AreEqual<Int32>(_progressEventCalls, _cancelIndex);

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The zip file save should have been canceled.");
        }



        [TestMethod]
        public void ExtractAll_Cancellation()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "ExtractAll_Cancellation.zip");
            string TargetDirectory = System.IO.Path.Combine(TopLevelDir, "unpack");

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "EventTest");
            System.IO.Directory.CreateDirectory(DirToZip);

            int subdirCount = 0;
            int entriesAdded = TestUtilities.GenerateFilesOneLevelDeep(TestContext, "ExtractAll_Cancellation", DirToZip, out subdirCount);

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Comment = "The extract on this zip archive will be canceled.";
                zip1.AddDirectory(DirToZip, System.IO.Path.GetFileName(DirToZip));
                zip1.Save(ZipFileToCreate);
            }

            _cancelIndex = entriesAdded - _rnd.Next(entriesAdded / 2);
            _progressEventCalls = 0;
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                zip2.ExtractProgress += ExtractProgress;
                zip2.ExtractAll(TargetDirectory);
            }

            Assert.AreEqual<Int32>(_progressEventCalls, _cancelIndex);
        }



        [TestMethod]
        public void ExtractAll_WithPassword()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "ExtractAll_WithPassword.zip");
            string TargetDirectory = System.IO.Path.Combine(TopLevelDir, "unpack");

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "DirToZip");
            System.IO.Directory.CreateDirectory(DirToZip);
            int subdirCount = 0;

            int entriesAdded = TestUtilities.GenerateFilesOneLevelDeep(TestContext, "ExtractAll_WithPassword", DirToZip, out subdirCount);
            string password = TestUtilities.GenerateRandomPassword();
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Password = password;
                zip1.Comment = "Brick walls are there for a reason: to let you show how badly you want your goal.";
                zip1.AddDirectory(DirToZip, System.IO.Path.GetFileName(DirToZip));
                zip1.Save(ZipFileToCreate);
            }

            _cancelIndex = -1; // don't cancel this Extract
            _progressEventCalls = 0;
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                zip2.Password = password;
                zip2.ExtractProgress += ExtractProgress;
                zip2.ExtractAll(TargetDirectory);
            }

            Assert.AreEqual<Int32>(_progressEventCalls, entriesAdded + subdirCount + 1);
        }


        [TestMethod]
        public void Extract_ImplicitPassword()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Extract_ImplicitPassword.zip");

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "DirToZip");

            var Files = TestUtilities.GenerateFilesFlat(DirToZip);
            string[] Passwords = new string[Files.Length];

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Comment = "Brick walls are there for a reason: to let you show how badly you want your goal.";
                for (int i = 0; i < Files.Length; i++)
                {
                    Passwords[i] = TestUtilities.GenerateRandomPassword();
                    zip1.Password = Passwords[i];
                    zip1.AddFile(Files[i], System.IO.Path.GetFileName(DirToZip));
                }
                zip1.Save(ZipFileToCreate);
            }

            // extract using the entry from the enumerator
            int nExtracted = 0;
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (ZipEntry e in zip2)
                {
                    e.Password = Passwords[nExtracted];
                    e.Extract("unpack1");
                    nExtracted++;
                }
            }

            Assert.AreEqual<Int32>(Files.Length, nExtracted);

            // extract using the filename indexer
            nExtracted = 0;
            using (ZipFile zip3 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (var n in zip3.EntryFileNames)
                {
                    zip3.Password = Passwords[nExtracted];
                    zip3.Extract(n, "unpack2");
                    nExtracted++;
                }
            }

            Assert.AreEqual<Int32>(Files.Length, nExtracted);

        }




        [TestMethod]
        public void Extract_SelfExtractor_WinForms()
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
                zip.Comment = "For testing purposes, please extract to:  " + TargetUnpackDirectory;
                //for (int i = 0; i < 44; i++) zip.Comment += "Lorem ipsum absalom hibiscus lasagne ";
                zip.SaveSelfExtractor(ExeFileToCreate, Ionic.Utils.Zip.SelfExtractorFlavor.WinFormsApplication);
            }

            // run the self-extracting EXE we just created 
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
