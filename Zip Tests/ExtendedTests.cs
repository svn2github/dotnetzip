using System;
//using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Zip;
using Ionic.Zip.Tests.Utilities;


/// Tests for more advanced scenarios.
/// 

namespace Ionic.Zip.Tests.Extended
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
        public void CreateZip_CheckInflation()
        {
            for (int j = 0; j < 3; j++)
            {
                System.IO.Directory.SetCurrentDirectory(TopLevelDir);
                TestContext.WriteLine("\n\n==================Trial {0}...", j);
                _doubleReadCallbacks = 0;
                string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("CreateZip_CheckInflation-{0}.zip", j));

                int entries = _rnd.Next(3) + 3;
                String filename = null;

                string Subdir = System.IO.Path.Combine(TopLevelDir, String.Format("A{0}", j));
                System.IO.Directory.CreateDirectory(Subdir);

                var checksums = new Dictionary<string, string>();

                TestContext.WriteLine("---------------Creating {0}...", ZipFileToCreate);
                using (ZipFile zip2 = new ZipFile())
                {
                    if (j > 0)
                        zip2.WillReadTwiceOnInflation = ReadTwiceCallback;
                    if (j > 1) _callbackAnswer = true;

                    for (int i = 0; i < entries; i++)
                    {
                        filename = System.IO.Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                        TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(44000) + 5000);
                        zip2.AddFile(filename, "");

                        var chk = TestUtilities.ComputeChecksum(filename);
                        checksums.Add(System.IO.Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                    }

                    zip2.Save(ZipFileToCreate);
                }

                TestContext.WriteLine("---------------Reading {0}...", ZipFileToCreate);
                using (ZipFile zip3 = ZipFile.Read(ZipFileToCreate))
                {
                    string extractDir = String.Format("extract{0}", j);
                    foreach (var e in zip3)
                    {
                        TestContext.WriteLine(" Entry: {0}", e.FileName);
                        TestContext.WriteLine("        compressed size: {0} bytes", e.CompressedSize);
                        TestContext.WriteLine("      uncompressed size: {0} bytes", e.UncompressedSize);

                        if (j != 1)
                            Assert.IsTrue(e.CompressedSize <= e.UncompressedSize,
                      "In trial {0}, Entry '{1}'  has expanded ({2} > {3}).", j, e.FileName, e.CompressedSize, e.UncompressedSize);

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
        public void ReadZip_OpenReader()
        {
            bool[] ForceCompressionOptions = { true, false };
            string[] Passwords = { null, System.IO.Path.GetRandomFileName(), "Esd;j39" };

            for (int j = 0; j < ForceCompressionOptions.Length; j++)
            {
                for (int k = 0; k < Passwords.Length; k++)
                {
                    string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("ReadZip_OpenReader-{0}-{1}.zip", j, k));

                    int entriesAdded = 0;
                    String filename = null;

                    string Subdir = System.IO.Path.Combine(TopLevelDir, String.Format("A{0}{1}", j, k));
                    System.IO.Directory.CreateDirectory(Subdir);
                    //var checksums = new Dictionary<string, string>();

                    int fileCount = _rnd.Next(10) + 10;
                    //int fileCount = 4;
                    for (int i = 0; i < fileCount; i++)
                    {
                        filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D2}.txt", i));
                        int filesize = _rnd.Next(34000) + 5000;
                        //int filesize = 2000;
                        TestUtilities.CreateAndFillFileText(filename, filesize);
                        entriesAdded++;
                        //var chk = TestUtilities.ComputeChecksum(filename);
                        //checksums.Add(filename, TestUtilities.CheckSumToString(chk));
                    }

                    using (ZipFile zip1 = new ZipFile())
                    {
                        zip1.ForceNoCompression = ForceCompressionOptions[j];
                        zip1.Password = Passwords[k];
                        zip1.AddDirectory(Subdir, System.IO.Path.GetFileName(Subdir));
                        zip1.Save(ZipFileToCreate);
                    }


                    // Verify the files are in the zip
                    Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entriesAdded,
                     String.Format("Trial {0}-{1}: The Zip file has the wrong number of entries.", j, k));

                    // now extract the files and verify their contents
                    using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
                    {
                        //zip2.Password = Passwords[k];
                        foreach (string eName in zip2.EntryFileNames)
                        {
                            ZipEntry e1 = zip2[eName];

                            if (!e1.IsDirectory)
                            {
                                using (CrcCalculatorStream s = e1.OpenReader(Passwords[k]))
                                {
                                    using (var output = System.IO.File.Create(System.IO.Path.Combine(TopLevelDir, eName + ".out")))
                                    {
                                        byte[] buffer = new byte[4096];
                                        int n, totalBytesRead = 0;
                                        do
                                        {
                                            n = s.Read(buffer, 0, buffer.Length);
                                            totalBytesRead += n;
                                            output.Write(buffer, 0, n);
                                        } while (n > 0);

                                        output.Flush();
                                        output.Close();
                                        TestContext.WriteLine("CRC expected({0:X8}) actual({1:X8})",
                                            e1.Crc32, s.Crc32);


                                        Assert.AreEqual<Int32>(s.Crc32, e1.Crc32,
                                   string.Format("The Entry {0} failed the CRC Check.", eName));

                                        Assert.AreEqual<Int32>(totalBytesRead, (int)e1.UncompressedSize,
                                   string.Format("We read an unexpected number of bytes. ({0})", eName));


                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        private int _doubleReadCallbacks = 0;
        private bool _callbackAnswer = false;
        public bool ReadTwiceCallback(long u, long c, string filename)
        {
            _doubleReadCallbacks++;
            TestContext.WriteLine("ReadTwiceCallback: {0} {1} {2}", u, c, filename);
            return _callbackAnswer;
        }



        [TestMethod]
        public void Save_DoubleReadCallback()
        {
            int j;
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Save_DoubleReadCallback.zip");

            // 1. make the inner zip file
            string Subdir = System.IO.Path.Combine(TopLevelDir, "DoubleRead");
            System.IO.Directory.CreateDirectory(Subdir);
            string InnerZipFile = System.IO.Path.Combine(Subdir, "DoubleReadCallback.zip");

            string InnerSubdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(InnerSubdir);
            //var checksums = new Dictionary<string, string>();
            string filename = null;
            int fileCount = _rnd.Next(10) + 10;
            for (j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(InnerSubdir, String.Format("file{0:D2}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);

                //var chk = TestUtilities.ComputeChecksum(filename);
                //checksums.Add(filename, TestUtilities.CheckSumToString(chk));
            }

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(InnerSubdir, System.IO.Path.GetFileName(InnerSubdir));
                zip1.Save(InnerZipFile);
            }


            // 2. make the outer zip file
            Subdir = System.IO.Path.Combine(TopLevelDir, "DoubleRead");
            var filename1 = System.IO.Path.Combine(Subdir, "SmallTextFile.txt");
            TestUtilities.CreateAndFillFileText(filename1, _rnd.Next(34) + 12);

            using (ZipFile zip2 = new ZipFile())
            {
                zip2.WillReadTwiceOnInflation = ReadTwiceCallback;
                zip2.AddFile(filename1, System.IO.Path.GetFileName(Subdir));
                zip2.AddFile(InnerZipFile, System.IO.Path.GetFileName(Subdir));
                for (j = 0; j < 5; j++)
                {
                    filename = System.IO.Path.Combine(InnerSubdir, String.Format("file{0:D2}.txt", j));
                    zip2.AddFile(filename, System.IO.Path.GetFileName(Subdir));
                }
                zip2.Save(ZipFileToCreate);
            }

            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), 7,
                 "The Zip file has the wrong number of entries.");

            Assert.IsTrue(ZipFile.IsZipFile(ZipFileToCreate),
              "The IsZipFile() method returned an unexpected result for an existing zip file.");

            Assert.AreEqual<int>(1, _doubleReadCallbacks);  // 1, for the single zip file added to the zip
        }




        [TestMethod]
        public void TestZip_IsZipFile()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "TestZip_IsZipFile.zip");

            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);
            //var checksums = new Dictionary<string, string>();

            int fileCount = _rnd.Next(10) + 10;
            for (int j = 0; j < fileCount; j++)
            {
                filename = System.IO.Path.Combine(Subdir, String.Format("FileToBeAdded-{0:D2}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
                //var chk = TestUtilities.ComputeChecksum(filename);
                //checksums.Add(filename, TestUtilities.CheckSumToString(chk));
            }

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(Subdir, System.IO.Path.GetFileName(Subdir));
                zip1.Save(ZipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entriesAdded,
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
        public void ReadZip_DirectoryBitSetForEmptyDirectories()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "ReadZip_DirectoryBitSetForEmptyDirectories.zip");

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectoryByName("Directory1");
                ZipEntry e1 = zip1["Directory1"];
                Assert.AreNotEqual<ZipEntry>(null, e1);
                Assert.IsTrue(e1.IsDirectory,
                      "The IsDirectory property was not set as expected.");
                zip1.AddDirectoryByName("Directory2");
                zip1.AddFileFromString("Readme.txt", "Directory2", "This is the content");
                Assert.IsTrue(zip1["Directory2"].IsDirectory,
                      "The IsDirectory property was not set as expected.");
                zip1.Save(ZipFileToCreate);
                Assert.IsTrue(zip1["Directory1"].IsDirectory,
                      "The IsDirectory property was not set as expected.");

            }


            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                Assert.IsTrue(zip2["Directory1"].IsDirectory,
                      "The IsDirectory property was not set as expected.");

                Assert.IsTrue(zip2["Directory2"].IsDirectory,
                      "The IsDirectory property was not set as expected.");
            }

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
            string[] Passwords = { null, "Password", TestUtilities.GenerateRandomPassword(), "A" };
            for (int k = 0; k < Passwords.Length; k++)
            {
                string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("Test_AddUpdateFileStream-{0}.zip", k));
                string[] InputStrings = new string[] { 
                    TestUtilities.LoremIpsum.Substring(0, 90),
                    TestUtilities.LoremIpsum.Substring(240, 80)};

                System.IO.Directory.SetCurrentDirectory(TopLevelDir);

                // add entries to a zipfile.  
                // use a password.(possibly null)
                using (ZipFile zip1 = new ZipFile(ZipFileToCreate))
                {
                    zip1.Password = Passwords[k];
                    //_doubleReadCallbacks = 0;
                    //_callbackAnswer = false;
                    //zip1.WillReadTwiceOnInflation = ReadTwiceCallback;
                    for (int i = 0; i < InputStrings.Length; i++)
                    {
                        //var ms1 = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(InputStrings[i]));
                        zip1.AddFileFromString(String.Format("Lorem{0}.txt", i + 1), "", InputStrings[i]);
                    }
                    zip1.Save();
                }

                using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
                {
                    zip2["Lorem2.txt"].Password = Passwords[k];
                    string output = StreamToString(zip2["Lorem2.txt"].OpenReader());

                    Assert.AreEqual<String>(output, InputStrings[1], "Trial {0}: Read entry 2 after create: Unexpected value on extract.", k);

                    zip2["Lorem1.txt"].Password = Passwords[k];
                    System.IO.Stream s = zip2["Lorem1.txt"].OpenReader();
                    output = StreamToString(s);

                    Assert.AreEqual<String>(output, InputStrings[0], "Trial {0}: Read entry 1 after create: Unexpected value on extract.", k);
                }


                // update an entry in the zipfile.  For this pass, don't use a password. 
                string UpdateString = "Nothing to see here.  Move along folks!  Move Along!";
                using (ZipFile zip3 = ZipFile.Read(ZipFileToCreate))
                {
                    //zip2.Password = password;
                    var ms1 = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(UpdateString));
                    zip3.UpdateFileStream("Lorem1.txt", "", ms1);
                    zip3.Save();
                }

                using (ZipFile zip4 = ZipFile.Read(ZipFileToCreate))
                {
                    string output = StreamToString(zip4["Lorem1.txt"].OpenReader());
                    Assert.AreEqual<String>(output, UpdateString, "Trial {0}: Reading after update: Unexpected value on extract.", k);
                }
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
                        DirsAdded.Add(DirName + "/");
                    }
                    zip1.Save();
                }


                int dirCount = 0;
                using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
                {
                    foreach (var e in zip2)
                    {
                        TestContext.WriteLine("dir: {0}", e.FileName);
                        Assert.IsTrue(DirsAdded.Contains(e.FileName), "Cannot find the expected entry");
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
                    DirsAdded.Add(DirName.Replace("\\", "/") + "/");
                }
                zip1.Save();
            }

            int dirCount = 0;
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                foreach (var e in zip2)
                {
                    TestContext.WriteLine("dir: {0}", e.FileName);
                    Assert.IsTrue(DirsAdded.Contains(e.FileName), "Cannot find the expected directory.");
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
                    // nested directories
                    DirName = (n == 0) ? "root" :
                        System.IO.Path.Combine(DirName, TestUtilities.GenerateRandomAsciiString(8));

                    zip1.AddDirectoryByName(DirName);
                    DirsAdded.Add(DirName.Replace("\\", "/") + "/");
                    if (n % 2 == 0) zip1.Password = password;
                    zip1.AddFileFromString(new System.String((char)(n + 48), 3) + ".txt", DirName, "Hello, Dolly!");
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
                        Assert.IsTrue(DirsAdded.Contains(e.FileName), "Cannot find the expected directory.");
                    else
                    {
                        if ((entryCount - 1) % 4 == 0) e.Password = password;
                        string output = StreamToString(e.OpenReader());
                        Assert.AreEqual<string>("Hello, Dolly!", output);
                    }
                    entryCount++;
                }
            }
            Assert.AreEqual<int>(DirsAdded.Count * 2, entryCount);
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
                filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
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
                zip.SaveSelfExtractor(ExeFileToCreate, Ionic.Zip.SelfExtractorFlavor.ConsoleApplication);
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
            if (e.EventType == ZipProgressEventType.Saving_AfterWriteEntry)
            {
                _progressEventCalls++;
                TestContext.WriteLine("{0}: {1} ({2}/{3})", e.EventType.ToString(), e.CurrentEntry.FileName, e.EntriesSaved, e.EntriesTotal);
                if (_cancelIndex == _progressEventCalls)
                {
                    e.Cancel = true;
                    TestContext.WriteLine("Cancelling...");
                }
            }
        }


        public void ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            if (e.EventType == ZipProgressEventType.Extracting_AfterExtractEntry)
            {
                _progressEventCalls++;
                TestContext.WriteLine("Extracted: {0} ({1}/{2})", e.CurrentEntry.FileName, e.EntriesExtracted, e.EntriesTotal);
                if (_cancelIndex == _progressEventCalls)
                {
                    e.Cancel = true;
                    TestContext.WriteLine("Cancelling...");
                }
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
        public void CreateZip_AddDirectory_NoFilesInRoot_WI5893()
        {
            int i, j;
            int entries = 0;

            int subdirCount = _rnd.Next(4) + 4;
            for (i = 0; i < subdirCount; i++)
            {
                string Subdir = System.IO.Path.Combine(TopLevelDir, "DirectoryToZip.test." + i);
                System.IO.Directory.CreateDirectory(Subdir);

                int fileCount = _rnd.Next(3) + 3;
                for (j = 0; j < fileCount; j++)
                {
                    String file = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.a", j));
                    TestUtilities.CreateAndFillFile(file, _rnd.Next(100) + 500);
                    entries++;
                }
            }

            string ZipFileToCreate = TestUtilities.GenerateUniquePathname("zip");
            _FilesToRemove.Add(ZipFileToCreate);

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(TopLevelDir, string.Empty);
                zip.Save();
            }
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entries, "The Zip file has the wrong number of entries.");
        }


        [TestMethod]
        public void Create_AddDirectory_NoFilesInRoot_WI5893a()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Create_AddDirectory_NoFilesInRoot_WI5893a.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            int i, j;
            int entries = 0;

            int subdirCount = _rnd.Next(4) + 4;
            for (i = 0; i < subdirCount; i++)
            {
                string Subdir = System.IO.Path.Combine(TopLevelDir, "DirectoryToZip.test." + i);
                System.IO.Directory.CreateDirectory(Subdir);

                int fileCount = _rnd.Next(3) + 3;
                for (j = 0; j < fileCount; j++)
                {
                    String file = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.a", j));
                    TestUtilities.CreateAndFillFile(file, _rnd.Next(100) + 500);
                    entries++;
                }
            }

            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                zip.AddDirectory(TopLevelDir, string.Empty);
                zip.Save();
            }
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), entries, "The Zip file has the wrong number of entries.");
        }



        [TestMethod]
        public void Read_BadFile()
        {
            string ZipFileToRead = System.IO.Path.Combine(TopLevelDir, "Read_BadFile.zip");

            string NewFile = System.IO.Path.GetTempFileName();
            System.IO.File.Move(NewFile, ZipFileToRead);

            NewFile = System.IO.Path.GetTempFileName();

            string EntryToAdd = System.IO.Path.Combine(TopLevelDir, "NonExistentFile.txt");
            System.IO.File.Move(NewFile, EntryToAdd);

            try
            {
                using (ZipFile zip = ZipFile.Read(ZipFileToRead))
                {
                    zip.AddFile(EntryToAdd, "");
                    zip.Save();
                }
            }
            catch (Exception ex1)
            {
                // expected - the zip file is invalid
                Console.WriteLine("Exception: {0}", ex1);
            }

            // this should succeed
            System.IO.File.Delete(ZipFileToRead);
            System.IO.File.Delete(EntryToAdd);

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
            bool[] ForceCompressionOptions = { true, false };
            for (int k = 0; k < ForceCompressionOptions.Length; k++)
            {
                string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("Extract_ImplicitPassword-{0}.zip", k));

                System.IO.Directory.SetCurrentDirectory(TopLevelDir);
                string DirToZip = System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetRandomFileName());

                var Files = TestUtilities.GenerateFilesFlat(DirToZip);
                string[] Passwords = new string[Files.Length];

                using (ZipFile zip1 = new ZipFile())
                {
                    zip1.Comment = "Brick walls are there for a reason: to let you show how badly you want your goal.";
                    for (int i = 0; i < Files.Length; i++)
                    {
                        Passwords[i] = TestUtilities.GenerateRandomPassword();
                        zip1.Password = Passwords[i];
                        TestContext.WriteLine("  Adding entry: {0} pw({1})", Files[i], Passwords[i]);
                        zip1.AddFile(Files[i], System.IO.Path.GetFileName(DirToZip));
                    }
                    zip1.Save(ZipFileToCreate);
                }
                TestContext.WriteLine("\n");

                // extract using the entry from the enumerator
                int nExtracted = 0;
                using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
                {
                    foreach (ZipEntry e in zip2)
                    {
                        e.Password = Passwords[nExtracted];
                        TestContext.WriteLine("  Extracting entry: {0} pw({1})", e.FileName, Passwords[nExtracted]);
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
        }



        [TestMethod]
        public void Extract_MultiThreaded_wi6637()
        {
            int nConcurrentZipFiles = 5;
            for (int k = 0; k < 1; k++)
            {
                TestContext.WriteLine("\n-----------------------------\r\n{0}: Trial {1}...",
                      DateTime.Now.ToString("HH:mm:ss"),
                      k);

                System.IO.Directory.SetCurrentDirectory(TopLevelDir);

                string[] ZipFileToCreate = new string[nConcurrentZipFiles];
                for (int m = 0; m < nConcurrentZipFiles; m++)
                {
                    ZipFileToCreate[m] = System.IO.Path.Combine(TopLevelDir, String.Format("Extract_MultiThreaded-{0}-{1}.zip", k, m));
                    TestContext.WriteLine("  Creating file: {0}", ZipFileToCreate[m]);
                    string DirToZip = System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetRandomFileName());

                    var Files = TestUtilities.GenerateFilesFlat(DirToZip);
                    TestContext.WriteLine("Zipping {0} files from dir '{1}'...", Files.Length, DirToZip);

                    using (ZipFile zip1 = new ZipFile())
                    {
                        zip1.Comment = "Brick walls are there for a reason: to let you show how badly you want your goal.";
                        for (int i = 0; i < Files.Length; i++)
                        {
                            TestContext.WriteLine("  Adding entry: {0}", Files[i]);
                            zip1.AddFile(Files[i], System.IO.Path.GetFileName(DirToZip));
                        }
                        zip1.Save(ZipFileToCreate[m]);
                    }
                    TestContext.WriteLine("\n");
                }


                // multi-thread extract
                foreach (string fileName in ZipFileToCreate)
                {
                    TestContext.WriteLine("queueing unzip for file: {0}", fileName);

                    System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(processZip), fileName);
                }

                while (completedEntries != ZipFileToCreate.Length)
                    System.Threading.Thread.Sleep(400);

                TestContext.WriteLine("done.");

            }
        }



        private int _completedEntries;
        private int completedEntries
        {
            get { return _completedEntries; }
            set
            {
                lock (this)
                {
                    _completedEntries = value;
                }
            }
        }



        private void processZip(object o)
        {
            string fileName = o as string;

            string zDir = System.IO.Path.Combine("extract",
                             System.IO.Path.GetFileNameWithoutExtension(fileName.ToString()));

            TestContext.WriteLine("extracting {0}...", fileName);

            using (var zFile = Ionic.Zip.ZipFile.Read(fileName))
            {
                zFile.ExtractAll(zDir, true);
            }
            completedEntries++;
        }






        [TestMethod]
        public void Extract_SelfExtractor_WinForms()
        {
            string[] Passwords = { null, "12345" };
            for (int k = 0; k < Passwords.Length; k++)
            {
                string ExeFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("Extract_SelfExtractor_WinForms-{0}.exe", k));
                string TargetUnpackDirectory = System.IO.Path.Combine(TopLevelDir, String.Format("unpack{0}", k));

                String filename = null;

                string Subdir = System.IO.Path.Combine(TopLevelDir, String.Format("A{0}", k));
                System.IO.Directory.CreateDirectory(Subdir);
                var checksums = new Dictionary<string, string>();

                int fileCount = _rnd.Next(10) + 10;
                for (int j = 0; j < fileCount; j++)
                {
                    filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                    var chk = TestUtilities.ComputeChecksum(filename);
                    checksums.Add(filename, TestUtilities.CheckSumToString(chk));
                }

                using (ZipFile zip = new ZipFile())
                {
                    zip.Password = Passwords[k];
                    zip.AddDirectory(Subdir, System.IO.Path.GetFileName(Subdir));
                    zip.Comment = "For testing purposes, please extract to:  " + TargetUnpackDirectory;
                    if (Passwords[k] != null) zip.Comment += String.Format("\r\n\r\nThe password for all entries is:  {0}\n", Passwords[k]);
                    zip.SaveSelfExtractor(ExeFileToCreate, Ionic.Zip.SelfExtractorFlavor.WinFormsApplication);
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
                string DirToCheck = System.IO.Path.Combine(TargetUnpackDirectory, String.Format("A{0}", k));
                // verify the checksum of each file matches with its brother
                var fileList = System.IO.Directory.GetFiles(DirToCheck);
                Assert.AreEqual<Int32>(checksums.Keys.Count, fileList.Length, "Trial {0}: Inconsistent results.", k);

                foreach (string fname in fileList)
                {
                    string expectedCheckString = checksums[fname.Replace(String.Format("\\unpack{0}", k), "")];
                    string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(fname));
                    Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Trial {0}: Unexpected checksum on extracted filesystem file ({1}).", k, fname);
                }
            }
        }


    }
}
