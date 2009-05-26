using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Zip;
using Ionic.Zip.Tests.Utilities;
using System.IO;


/// Tests for more advanced scenarios?
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
            // Three trials:
            // first trial has no callback.
            // second trial has a callback that always returns false.
            // third trial has a callback that always returns true. 

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
                    {
                        zip2.WillReadTwiceOnInflation = ReadTwiceCallback;
                        _callbackAnswer = (j > 1);
                    }

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
                        TestContext.WriteLine(" Entry: {0}  c({1})  u({2})", e.FileName, e.CompressedSize, e.UncompressedSize);

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
                                using (Ionic.Zlib.CrcCalculatorStream s = e1.OpenReader(Passwords[k]))
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
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Save_DoubleReadCallback.zip");

            // 1. create the directory
            string Subdir = System.IO.Path.Combine(TopLevelDir, "DoubleReadTest");
            System.IO.Directory.CreateDirectory(Subdir);

            // 2. create a small text file, which is incompressible
            var SmallIncompressibleTextFile = System.IO.Path.Combine(Subdir, "IncompressibleTextFile.txt");
            using (var sw = System.IO.File.AppendText(SmallIncompressibleTextFile))
            {
                sw.WriteLine("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            }

            // 3. make a large text file
            string LargeTextFile = System.IO.Path.Combine(Subdir, "LargeTextFile.txt");
            TestUtilities.CreateAndFillFileText(LargeTextFile, _rnd.Next(64000) + 15000);

            // 4. compress that file to make a large incompressible file
            string CompressedFile = LargeTextFile + ".COMPRESSED";

            byte[] working = new byte[0x2000];
            int n = -1;
            using (var input = System.IO.File.OpenRead(LargeTextFile))
            {
                using (var raw = System.IO.File.Create(CompressedFile))
                {
                    using (var compressor = new Ionic.Zlib.GZipStream(raw, Ionic.Zlib.CompressionMode.Compress, Ionic.Zlib.CompressionLevel.BestCompression, true))
                    {
                        n = -1;
                        while (n != 0)
                        {
                            if (n > 0)
                                compressor.Write(working, 0, n);
                            n = input.Read(working, 0, working.Length);
                        }
                    }
                }
            }


            // 5. create the zip file with all those things in it
            using (ZipFile zip = new ZipFile())
            {
                zip.WillReadTwiceOnInflation = ReadTwiceCallback;
                zip.AddFileFromString("ReadMe.txt", "", "This is the content for the Readme file. This is the content. Right here. This is the content. This is it. And this content is compressible.");
                zip.AddFile(SmallIncompressibleTextFile, System.IO.Path.GetFileName(Subdir));
                zip.AddFile(LargeTextFile, System.IO.Path.GetFileName(Subdir));
                zip.AddFile(CompressedFile, System.IO.Path.GetFileName(Subdir));
                zip.Save(ZipFileToCreate);
            }

            // 6. check results
            Assert.AreEqual<int>(TestUtilities.CountEntries(ZipFileToCreate), 4,
                 "The Zip file has the wrong number of entries.");

            Assert.IsTrue(ZipFile.IsZipFile(ZipFileToCreate),
              "The IsZipFile() method returned an unexpected result for an existing zip file.");

            Assert.AreEqual<int>(2, _doubleReadCallbacks);  // 1 for the compressed file, 1 for the small incompressible text file
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
                zip1.AddFileFromStream("Test.xml", "Woo", ms1);
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
        public void Test_AddUpdateFileFromStream()
        {
            string[] Passwords = { null, "Password", TestUtilities.GenerateRandomPassword(), "A" };
            for (int k = 0; k < Passwords.Length; k++)
            {
                string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("Test_AddUpdateFileFromStream-{0}.zip", k));
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
                    zip3.UpdateFileFromStream("Lorem1.txt", "", ms1);
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
        public void Extract_SelfExtractor_CanRead()
        {
            SelfExtractorFlavor[] trials = { SelfExtractorFlavor.ConsoleApplication, SelfExtractorFlavor.WinFormsApplication };
            for (int k = 0; k < trials.Length; k++)
            {
                string SfxFileToCreate = System.IO.Path.Combine(TopLevelDir, String.Format("Extract_SelfExtractor_{0}.exe", trials[k].ToString()));
                string UnpackDirectory = System.IO.Path.Combine(TopLevelDir, "unpack");
                if (Directory.Exists(UnpackDirectory))
                    Directory.Delete(UnpackDirectory, true);
                string ReadmeString = "Hey there!  This zipfile entry was created directly from a string in application code.";

                int entriesAdded = 0;
                String filename = null;

                string Subdir = System.IO.Path.Combine(TopLevelDir, String.Format("A{0}", k));
                System.IO.Directory.CreateDirectory(Subdir);
                var checksums = new Dictionary<string, string>();

                int fileCount = _rnd.Next(50) + 30;
                for (int j = 0; j < fileCount; j++)
                {
                    filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                    entriesAdded++;
                    var chk = TestUtilities.ComputeChecksum(filename);
                    checksums.Add(filename.Replace(TopLevelDir + "\\", "").Replace('\\', '/'), TestUtilities.CheckSumToString(chk));
                }

                using (ZipFile zip1 = new ZipFile())
                {
                    zip1.AddDirectory(Subdir, System.IO.Path.GetFileName(Subdir));
                    zip1.Comment = "This will be embedded into a self-extracting exe";
                    System.IO.MemoryStream ms1 = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(ReadmeString));
                    zip1.AddFileFromStream("Readme.txt", "", ms1);
                    zip1.SaveSelfExtractor(SfxFileToCreate, trials[k]);
                }

                TestContext.WriteLine("---------------Reading {0}...", SfxFileToCreate);
                using (ZipFile zip2 = ZipFile.Read(SfxFileToCreate))
                {
                    //string extractDir = String.Format("extract{0}", j);
                    foreach (var e in zip2)
                    {
                        TestContext.WriteLine(" Entry: {0}  c({1})  u({2})", e.FileName, e.CompressedSize, e.UncompressedSize);
                        e.Extract(UnpackDirectory);
                        if (!e.IsDirectory)
                        {
                            if (checksums.ContainsKey(e.FileName))
                            {
                                filename = System.IO.Path.Combine(UnpackDirectory, e.FileName);
                                string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filename));
                                Assert.AreEqual<string>(checksums[e.FileName], actualCheckString, "In trial {0}, Checksums for ({1}) do not match.", k, e.FileName);
                                //TestContext.WriteLine("     Checksums match ({0}).\n", actualCheckString);
                            }
                            else
                            {
                                Assert.AreEqual<string>("Readme.txt", e.FileName, String.Format("trial {0}", k));
                            }
                        }
                    }
                }
            }
        }



        [TestMethod]
        public void Extract_SelfExtractor_Console()
        {
            string ExeFileToCreate = System.IO.Path.Combine(TopLevelDir, "Extract_SelfExtractor_Console.exe");
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
                zip.AddFileFromStream("Readme.txt", "", ms1);
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
        Int64 maxBytesXferred = 0;
        public void SaveProgress(object sender, SaveProgressEventArgs e)
        {
            switch (e.EventType)
            {
                case ZipProgressEventType.Saving_AfterWriteEntry:
                    _progressEventCalls++;
                    TestContext.WriteLine("{0}: {1} ({2}/{3})", e.EventType.ToString(), e.CurrentEntry.FileName, e.EntriesSaved, e.EntriesTotal);
                    if (_cancelIndex == _progressEventCalls)
                    {
                        e.Cancel = true;
                        TestContext.WriteLine("Cancelling...");
                    }
                    break;

                case ZipProgressEventType.Saving_EntryBytesRead:
                    maxBytesXferred = e.BytesTransferred;
                    break;

                default:
                    break;
            }
        }


        public void ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            switch (e.EventType)
            {
                case ZipProgressEventType.Extracting_AfterExtractEntry:
                    _progressEventCalls++;
                    TestContext.WriteLine("Extracted: {0} ({1}/{2})", e.CurrentEntry.FileName, e.EntriesExtracted, e.EntriesTotal);
                    // synthetic cancellation
                    if (_cancelIndex == _progressEventCalls)
                    {
                        e.Cancel = true;
                        TestContext.WriteLine("Cancelling...");
                    }
                    break;

                case ZipProgressEventType.Extracting_EntryBytesWritten:
                    maxBytesXferred = e.BytesTransferred;
                    break;

                default:
                    break;
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
            _cancelIndex = -1; // don't cancel this Save
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.SaveProgress += SaveProgress;
                zip1.Comment = "This is the comment on the zip archive.";
                zip1.AddDirectory(DirToZip, System.IO.Path.GetFileName(DirToZip));
                zip1.Save(ZipFileToCreate);
            }

            // why entriesAdded + subdirCount + 1?  
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


        [Timeout(1500000), TestMethod]
        public void LargeFile_WithProgress()
        {
            // This test checks the Int64 limits in progress events (Save + Extract)
            TestContext.WriteLine("Test beginning {0}", System.DateTime.Now.ToString("G"));
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "LargeFile_WithProgress.zip");
            string TargetDirectory = System.IO.Path.Combine(TopLevelDir, "unpack");

            string DirToZip = System.IO.Path.Combine(TopLevelDir, "LargeFile");
            System.IO.Directory.CreateDirectory(DirToZip);

            Int64 filesize = 2147483649 + _rnd.Next(1000000);  // larger than max-int 
            TestContext.WriteLine("Creating a large file, size({0})", filesize);
            string filename = System.IO.Path.Combine(DirToZip, "LargeFile.bin");
            TestUtilities.CreateAndFillFileBinaryZeroes(filename, filesize);
            TestContext.WriteLine("File Create complete {0}", System.DateTime.Now.ToString("G"));

            _progressEventCalls = 0;
            maxBytesXferred = 0;
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.SaveProgress += SaveProgress;
                zip1.Comment = "This is the comment on the zip archive.";
                zip1.AddDirectory(DirToZip, System.IO.Path.GetFileName(DirToZip));
                zip1.Save(ZipFileToCreate);
            }

            TestContext.WriteLine("Save complete {0}", System.DateTime.Now.ToString("G"));

            Assert.AreEqual<Int32>(_progressEventCalls, 1 + 1,
                   "The number of Entries added is not equal to the number of entries saved.");

            Assert.AreEqual<Int64>(filesize, maxBytesXferred,
                "The number of bytes saved is not the expected value.");

            // remove the very large file before extracting
            Directory.Delete(DirToZip, true);

            _progressEventCalls = 0;
            _cancelIndex = -1; // don't cancel this Extract
            maxBytesXferred = 0;
            using (ZipFile zip2 = ZipFile.Read(ZipFileToCreate))
            {
                zip2.ExtractProgress += ExtractProgress;
                zip2.ExtractAll(TargetDirectory);
            }

            TestContext.WriteLine("Extract complete {0}", System.DateTime.Now.ToString("G"));

            Assert.AreEqual<Int32>(_progressEventCalls, 1 + 1,
                   "The number of Entries added is not equal to the number of entries extracted.");

            Assert.AreEqual<Int64>(filesize, maxBytesXferred,
                   "The number of bytes extracted is not the expected value.");

            TestContext.WriteLine("Test complete {0}", System.DateTime.Now.ToString("G"));
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

            using (var zFile = ZipFile.Read(fileName))
            {
                zFile.ExtractAll(zDir, ExtractExistingFileAction.OverwriteSilently);
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


        [TestMethod]
        public void Extract_ExistingFile()
        {
            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Extract_ExistingFile.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

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

            // extract the second time - there will be no overwrites, and no extractions
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                    zip[Path.GetFileName(filenames[j])].Extract("unpack", ExtractExistingFileAction.DontOverwrite);
            }

            // extract the third time - there will be overwrites
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                    zip[Path.GetFileName(filenames[j])].Extract("unpack", ExtractExistingFileAction.OverwriteSilently);
            }
        }



        [TestMethod]
        public void Extract_WinZip_SelfExtractor()
        {
            string SourceDir = CurrentDir;
            for (int i = 0; i < 3; i++)
                SourceDir = System.IO.Path.GetDirectoryName(SourceDir);

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            TestContext.WriteLine("Current Dir: {0}", CurrentDir);

            // This is a SFX (Self-Extracting Archive) produced by WinZip
            string filename = System.IO.Path.Combine(SourceDir, "Zip Tests\\bin\\Debug\\zips\\winzip-sfx.exe");

            // try reading the WinZip SFX zipfile - this should succeed
            TestContext.WriteLine("Reading WinZip SFX file: '{0}'", filename);
            using (ZipFile zip = ZipFile.Read(filename))
            {
                string extractDir = "extract";
                foreach (ZipEntry e in zip)
                {

                    TestContext.WriteLine("{1,-22} {2,9} {3,5:F0}%   {4,9}  {5,3} {6:X8} {0}",
                                                                         e.FileName,
                                                                         e.LastModified.ToString("yyyy-MM-dd HH:mm:ss"),
                                                                         e.UncompressedSize,
                                                                         e.CompressionRatio,
                                                                         e.CompressedSize,
                                                                         (e.UsesEncryption) ? "Y" : "N",
                                                                         e.Crc32);
                    e.Extract(extractDir);
                }
            }
        }


        [TestMethod]
        public void Selector_SelectFiles()
        {
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Selector_SelectFiles.zip");
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            int count1, count2;
            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            int fileCount = _rnd.Next(33) + 33;
            for (int j = 0; j < fileCount; j++)
            {
                if (_rnd.Next(2) == 0)
                {
                    filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(5000) + 5000);
                }
                else
                {
                    filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.bin", j));
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
                TestContext.WriteLine(System.IO.Path.GetFileName(s));
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
                TestContext.WriteLine(System.IO.Path.GetFileName(s));
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
                TestContext.WriteLine(System.IO.Path.GetFileName(s));
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
                TestContext.WriteLine(System.IO.Path.GetFileName(s));
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
                TestContext.WriteLine("{0} size({1})", System.IO.Path.GetFileName(s), fi.Length);
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
                TestContext.WriteLine("{0} size({1})", System.IO.Path.GetFileName(s), fi.Length);
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
                TestContext.WriteLine("{0} size({1})", System.IO.Path.GetFileName(s), fi.Length);
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
                TestContext.WriteLine("{0} size({1})", System.IO.Path.GetFileName(s), fi.Length);
                bool x = !s.EndsWith(".bin") || fi.Length <= 7500;
                Assert.IsTrue(x);
                count2++;
            }
            Assert.AreEqual<Int32>(entriesAdded, count1 + count2);
        }




        [TestMethod]
        public void Selector_AddSelectedFiles()
        {
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            string[] ZipFileToCreate = {
		System.IO.Path.Combine(TopLevelDir, "Selector_AddSelectedFiles-1.zip"),
		System.IO.Path.Combine(TopLevelDir, "Selector_AddSelectedFiles-2.zip")
	    };

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate[0]), "The temporary zip file '{0}' already exists.", ZipFileToCreate[0]);
            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate[1]), "The temporary zip file '{0}' already exists.", ZipFileToCreate[1]);

            int count1, count2;
            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            int fileCount = _rnd.Next(95) + 95;
            TestContext.WriteLine("===============================================");
            TestContext.WriteLine("Creating {0} files.", fileCount);
            for (int j = 0; j < fileCount; j++)
            {
                if (_rnd.Next(2) == 0)
                {
                    filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(5000) + 5000);
                }
                else
                {
                    filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.bin", j));
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(5000) + 5000);
                }

                // mark one third of the files as Hidden
                if (j % 3 == 0)
                {
                    System.IO.File.SetAttributes(filename, System.IO.FileAttributes.Hidden);
                }

                // set the last mod time on 1/4th of the files
                if (j % 4 == 0)
                {
                    DateTime x = new DateTime(1998, 4, 29);
                    System.IO.File.SetLastWriteTime(filename, x);
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
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Selector_SelectEntries.zip");

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            //int count1, count2;
            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            int fileCount = _rnd.Next(33) + 33;
            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Files being added to the zip:");
            for (int j = 0; j < fileCount; j++)
            {
                if (_rnd.Next(2) == 0)
                {
                    filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.txt", j));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(5000) + 5000);
                }
                else
                {
                    filename = System.IO.Path.Combine(Subdir, String.Format("file{0:D3}.bin", j));
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(5000) + 5000);
                }
                TestContext.WriteLine(System.IO.Path.GetFileName(filename));
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
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Selector_SelectEntries_Spaces.zip");

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            //int count1, count2;
            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            int fileCount = _rnd.Next(44) + 44;
            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Files being added to the zip:");
            for (int j = 0; j < fileCount; j++)
            {
                string space = (_rnd.Next(2) == 0) ? " " : "";
                if (_rnd.Next(2) == 0)
                {
                    filename = System.IO.Path.Combine(Subdir, String.Format("file{1}{0:D3}.txt", j, space));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(5000) + 5000);
                }
                else
                {
                    filename = System.IO.Path.Combine(Subdir, String.Format("file{1}{0:D3}.bin", j, space));
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(5000) + 5000);
                }
                TestContext.WriteLine(System.IO.Path.GetFileName(filename));
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
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Selector_RemoveSelectedEntries_Spaces.zip");

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            //int count1, count2;
            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            int fileCount = _rnd.Next(44) + 44;
            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Files being added to the zip:");
            for (int j = 0; j < fileCount; j++)
            {
                string space = (_rnd.Next(2) == 0) ? " " : "";
                if (_rnd.Next(2) == 0)
                {
                    filename = System.IO.Path.Combine(Subdir, String.Format("file{1}{0:D3}.txt", j, space));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(5000) + 5000);
                }
                else
                {
                    filename = System.IO.Path.Combine(Subdir, String.Format("file{1}{0:D3}.bin", j, space));
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(5000) + 5000);
                }
                TestContext.WriteLine(System.IO.Path.GetFileName(filename));
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
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Selector_RemoveSelectedEntries2.zip");

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            //int count1, count2;
            int entriesAdded = 0;
            String filename = null;

            string Subdir = System.IO.Path.Combine(TopLevelDir, "A");
            System.IO.Directory.CreateDirectory(Subdir);

            int fileCount = _rnd.Next(44) + 44;
            TestContext.WriteLine("====================================================");
            TestContext.WriteLine("Files being added to the zip:");
            for (int j = 0; j < fileCount; j++)
            {
                string space = (_rnd.Next(2) == 0) ? " " : "";
                if (_rnd.Next(2) == 0)
                {
                    filename = System.IO.Path.Combine(Subdir, String.Format("file{1}{0:D3}.txt", j, space));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(5000) + 5000);
                }
                else
                {
                    filename = System.IO.Path.Combine(Subdir, String.Format("file{1}{0:D3}.bin", j, space));
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(5000) + 5000);
                }
                TestContext.WriteLine(System.IO.Path.GetFileName(filename));
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
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Selector_SelectFiles_Subdirs.zip");

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            int count1, count2;

            string Fodder = System.IO.Path.Combine(TopLevelDir, "fodder");
            System.IO.Directory.CreateDirectory(Fodder);


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
                string Subdir = System.IO.Path.Combine(Fodder, SubdirShort);
                System.IO.Directory.CreateDirectory(Subdir);

                int filecount = _rnd.Next(8) + 8;
                //int filecount = _rnd.Next(2) + 2;
                FileCount[SubdirShort] = filecount;
                for (int j = 0; j < filecount; j++)
                {
                    string filename = String.Format("file{0:D4}.x", j);
                    string fqFilename = System.IO.Path.Combine(Subdir, filename);
                    TestUtilities.CreateAndFillFile(fqFilename, _rnd.Next(1000) + 1000);

                    var chk = TestUtilities.ComputeChecksum(fqFilename);
                    var s = TestUtilities.CheckSumToString(chk);
                    var t1 = System.IO.Path.GetFileName(Fodder);
                    var t2 = System.IO.Path.Combine(t1, SubdirShort);
                    var key = System.IO.Path.Combine(t2, filename);
                    key = TestUtilities.TrimVolumeAndSwapSlashes(key);
                    TestContext.WriteLine("chk[{0}]= {1}", key, s);
                    checksums.Add(key, s);
                    entries++;
                }
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

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
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

            string ZipFileToCreate = System.IO.Path.Combine(TopLevelDir, "Selector_SelectFiles_Fullpath.zip");

            Assert.IsFalse(System.IO.File.Exists(ZipFileToCreate), "The temporary zip file '{0}' already exists.", ZipFileToCreate);

            int count1, count2;

            string Fodder = System.IO.Path.Combine(TopLevelDir, "fodder");
            System.IO.Directory.CreateDirectory(Fodder);


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
                string Subdir = System.IO.Path.Combine(Fodder, SubdirShort);
                System.IO.Directory.CreateDirectory(Subdir);

                int filecount = _rnd.Next(8) + 8;
                //int filecount = _rnd.Next(2) + 2;
                FileCount[SubdirShort] = filecount;
                for (int j = 0; j < filecount; j++)
                {
                    string filename = String.Format("file{0:D4}.x", j);
                    string fqFilename = System.IO.Path.Combine(Subdir, filename);
                    TestUtilities.CreateAndFillFile(fqFilename, _rnd.Next(1000) + 1000);

                    var chk = TestUtilities.ComputeChecksum(fqFilename);
                    var s = TestUtilities.CheckSumToString(chk);
                    var t1 = System.IO.Path.GetFileName(Fodder);
                    var t2 = System.IO.Path.Combine(t1, SubdirShort);
                    var key = System.IO.Path.Combine(t2, filename);
                    key = TestUtilities.TrimVolumeAndSwapSlashes(key);
                    TestContext.WriteLine("chk[{0}]= {1}", key, s);
                    checksums.Add(key, s);
                    entries++;
                }
            }

            System.IO.Directory.SetCurrentDirectory(TopLevelDir);

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
                    var selected1 = zip1.SelectEntries(System.IO.Path.Combine(dirInArchive, "*.*"));
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
                             System.IO.Path.Combine(dirInArchive, "*.*"));
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
