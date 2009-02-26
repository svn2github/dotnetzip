using System;
using System.Text;
using System.Collections.Generic;
using Ionic.Zlib;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Ionic.Zlib.Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class UnitTest1
    {
        private System.Random _rnd;

        public UnitTest1()
        {
            _rnd = new System.Random();
        }

        static UnitTest1()
        {
            LoremIpsumWords = LoremIpsum.Split(" ".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);
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
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //

        private string CurrentDir = null;
        private string TopLevelDir = null;

        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            CurrentDir = System.IO.Directory.GetCurrentDirectory();
            Assert.AreNotEqual<string>(System.IO.Path.GetFileName(CurrentDir), "Temp", "at start");

            string parentDir = System.Environment.GetEnvironmentVariable("TEMP");

            TopLevelDir = System.IO.Path.Combine(parentDir, String.Format("Ionic.ZlibTest-{0}.tmp", System.DateTime.Now.ToString("yyyyMMMdd-HHmmss")));
            System.IO.Directory.CreateDirectory(TopLevelDir);
            System.IO.Directory.SetCurrentDirectory(TopLevelDir);
        }


        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            System.IO.Directory.SetCurrentDirectory(System.Environment.GetEnvironmentVariable("TEMP"));
            System.IO.Directory.Delete(TopLevelDir, true);
            Assert.AreNotEqual<string>(System.IO.Path.GetFileName(CurrentDir), "Temp", "at finish");
            System.IO.Directory.SetCurrentDirectory(CurrentDir);
        }


        #endregion

        #region Helpers
        /// <summary>
        /// Converts a string to a MemoryStream.
        /// </summary>
        static System.IO.MemoryStream StringToMemoryStream(string s)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            int byteCount = enc.GetByteCount(s.ToCharArray(), 0, s.Length);
            byte[] ByteArray = new byte[byteCount];
            int bytesEncodedCount = enc.GetBytes(s, 0, s.Length, ByteArray, 0);
            System.IO.MemoryStream ms = new System.IO.MemoryStream(ByteArray);
            return ms;
        }

        /// <summary>
        /// Converts a MemoryStream to a string. Makes some assumptions about the content of the stream. 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static String MemoryStreamToString(System.IO.MemoryStream ms)
        {
            byte[] ByteArray = ms.ToArray();
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            var s = enc.GetString(ByteArray);
            return s;
        }

        private static void CopyStream(System.IO.Stream src, System.IO.Stream dest)
        {
            byte[] buffer = new byte[1024];
            int len = src.Read(buffer, 0, buffer.Length);
            while (len > 0)
            {
                dest.Write(buffer, 0, len);
                len = src.Read(buffer, 0, buffer.Length);
            }
            dest.Flush();
        }
        #endregion


        [TestMethod]
        public void Zlib_BasicDeflateAndInflate()
        {
            string TextToCompress = "Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Integer vulputate, nibh non rhoncus euismod, erat odio pellentesque lacus, sit amet convallis mi augue et odio. Phasellus cursus urna facilisis quam. Suspendisse nec metus et sapien scelerisque euismod. Nullam molestie sem quis nisl. Fusce pellentesque, ante sed semper egestas, sem nulla vestibulum nulla, quis sollicitudin leo lorem elementum wisi. Aliquam vestibulum nonummy orci. Sed in dolor sed enim ullamcorper accumsan. Duis vel nibh. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos hymenaeos. Sed faucibus, enim sit amet venenatis laoreet, nisl elit posuere est, ut sollicitudin tortor velit ut ipsum. Aliquam erat volutpat. Phasellus tincidunt vehicula eros. Curabitur vitae erat.";

            int rc;
            int bufferSize = 40000;
            byte[] compressedBytes = new byte[bufferSize];
            byte[] decompressedBytes = new byte[bufferSize];

            ZlibCodec compressingStream = new ZlibCodec();

            rc = compressingStream.InitializeDeflate(CompressionLevel.DEFAULT);
            Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at InitializeDeflate() [{0}]", compressingStream.Message));

            compressingStream.InputBuffer = System.Text.ASCIIEncoding.ASCII.GetBytes(TextToCompress);
            compressingStream.NextIn = 0;

            compressingStream.OutputBuffer = compressedBytes;
            compressingStream.NextOut = 0;

            while (compressingStream.TotalBytesIn != TextToCompress.Length && compressingStream.TotalBytesOut < bufferSize)
            {
                compressingStream.AvailableBytesIn = compressingStream.AvailableBytesOut = 1; // force small buffers
                rc = compressingStream.Deflate(ZlibConstants.Z_NO_FLUSH);
                Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at Deflate(1) [{0}]", compressingStream.Message));
            }

            while (true)
            {
                compressingStream.AvailableBytesOut = 1;
                rc = compressingStream.Deflate(ZlibConstants.Z_FINISH);
                if (rc == ZlibConstants.Z_STREAM_END)
                    break;
                Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at Deflate(2) [{0}]", compressingStream.Message));
            }

            rc = compressingStream.EndDeflate();
            Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at EndDeflate() [{0}]", compressingStream.Message));

            ZlibCodec decompressingStream = new ZlibCodec();

            decompressingStream.InputBuffer = compressedBytes;
            decompressingStream.NextIn = 0;
            decompressingStream.OutputBuffer = decompressedBytes;
            decompressingStream.NextOut = 0;

            rc = decompressingStream.InitializeInflate();
            Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at InitializeInflate() [{0}]", decompressingStream.Message));
            //CheckForError(decompressingStream, rc, "inflateInit");

            while (decompressingStream.TotalBytesOut < decompressedBytes.Length && decompressingStream.TotalBytesIn < bufferSize)
            {
                decompressingStream.AvailableBytesIn = decompressingStream.AvailableBytesOut = 1; /* force small buffers */
                rc = decompressingStream.Inflate(ZlibConstants.Z_NO_FLUSH);
                if (rc == ZlibConstants.Z_STREAM_END)
                    break;
                Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at Inflate() [{0}]", decompressingStream.Message));
                //CheckForError(decompressingStream, rc, "inflate");
            }

            rc = decompressingStream.EndInflate();
            Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at EndInflate() [{0}]", decompressingStream.Message));
            //CheckForError(decompressingStream, rc, "inflateEnd");

            int j = 0;
            for (; j < decompressedBytes.Length; j++)
                if (decompressedBytes[j] == 0)
                    break;

            Assert.AreEqual<int>(TextToCompress.Length, j, String.Format("Unequal lengths"));

            int i = 0;
            for (i = 0; i < j; i++)
                if (TextToCompress[i] != decompressedBytes[i])
                    break;

            Assert.AreEqual<int>(j, i, String.Format("Non-identical content"));

            var result = System.Text.ASCIIEncoding.ASCII.GetString(decompressedBytes, 0, j);

            TestContext.WriteLine("orig length: {0}", TextToCompress.Length);
            TestContext.WriteLine("compressed length: {0}", compressingStream.TotalBytesOut);
            TestContext.WriteLine("decompressed length: {0}", decompressingStream.TotalBytesOut);
            TestContext.WriteLine("result length: {0}", result.Length);
            TestContext.WriteLine("result of inflate:\n{0}", result);
            return;
        }



        [TestMethod]
        public void Zlib_BasicDictionaryDeflateInflate()
        {

            int rc;
            int comprLen = 40000;
            int uncomprLen = comprLen;
            byte[] uncompr = new byte[uncomprLen];
            byte[] compr = new byte[comprLen];
            //long dictId;

            ZlibCodec compressor = new ZlibCodec();
            rc = compressor.InitializeDeflate(CompressionLevel.LEVEL9_BEST_COMPRESSION);
            Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at InitializeDeflate() [{0}]", compressor.Message));

            string dictionaryWord = "hello ";
            byte[] dictionary = System.Text.ASCIIEncoding.ASCII.GetBytes(dictionaryWord);
            string TextToCompress = "hello, hello!  How are you, Joe? ";
            byte[] BytesToCompress = System.Text.ASCIIEncoding.ASCII.GetBytes(TextToCompress);

            rc = compressor.SetDictionary(dictionary);
            Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at SetDeflateDictionary() [{0}]", compressor.Message));

            long dictId = compressor.Adler32;

            compressor.OutputBuffer = compr;
            compressor.NextOut = 0;
            compressor.AvailableBytesOut = comprLen;

            compressor.InputBuffer = BytesToCompress;
            compressor.NextIn = 0;
            compressor.AvailableBytesIn = BytesToCompress.Length;

            rc = compressor.Deflate(ZlibConstants.Z_FINISH);
            Assert.AreEqual<int>(ZlibConstants.Z_STREAM_END, rc, String.Format("at Deflate() [{0}]", compressor.Message));

            rc = compressor.EndDeflate();
            Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at EndDeflate() [{0}]", compressor.Message));


            ZlibCodec decompressor = new ZlibCodec();

            decompressor.InputBuffer = compr;
            decompressor.NextIn = 0;
            decompressor.AvailableBytesIn = comprLen;

            rc = decompressor.InitializeInflate();
            Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at InitializeInflate() [{0}]", decompressor.Message));

            decompressor.OutputBuffer = uncompr;
            decompressor.NextOut = 0;
            decompressor.AvailableBytesOut = uncomprLen;

            while (true)
            {
                rc = decompressor.Inflate(ZlibConstants.Z_NO_FLUSH);
                if (rc == ZlibConstants.Z_STREAM_END)
                {
                    break;
                }
                if (rc == ZlibConstants.Z_NEED_DICT)
                {
                    Assert.AreEqual<long>(dictId, decompressor.Adler32, "Unexpected Dictionary");
                    rc = decompressor.SetDictionary(dictionary);
                }
                Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at Inflate/SetInflateDictionary() [{0}]", decompressor.Message));
            }

            rc = decompressor.EndInflate();
            Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at EndInflate() [{0}]", decompressor.Message));

            int j = 0;
            for (; j < uncompr.Length; j++)
                if (uncompr[j] == 0)
                    break;

            Assert.AreEqual<int>(TextToCompress.Length, j, String.Format("Unequal lengths"));

            int i = 0;
            for (i = 0; i < j; i++)
                if (TextToCompress[i] != uncompr[i])
                    break;

            Assert.AreEqual<int>(j, i, String.Format("Non-identical content"));

            var result = System.Text.ASCIIEncoding.ASCII.GetString(uncompr, 0, j);

            TestContext.WriteLine("orig length: {0}", TextToCompress.Length);
            TestContext.WriteLine("compressed length: {0}", compressor.TotalBytesOut);
            TestContext.WriteLine("uncompressed length: {0}", decompressor.TotalBytesOut);
            TestContext.WriteLine("result length: {0}", result.Length);
            TestContext.WriteLine("result of inflate:\n{0}", result);
        }

        [TestMethod]
        public void Zlib_TestFlushSync()
        {
            int rc;
            int bufferSize = 40000;
            byte[] CompressedBytes = new byte[bufferSize];
            byte[] DecompressedBytes = new byte[bufferSize];
            string TextToCompress = "This is the text that will be compressed.";
            byte[] BytesToCompress = System.Text.ASCIIEncoding.ASCII.GetBytes(TextToCompress);

            ZlibCodec compressor = new ZlibCodec(CompressionMode.Compress);

            compressor.InputBuffer = BytesToCompress;
            compressor.NextIn = 0;
            compressor.AvailableBytesIn = 3;

            compressor.OutputBuffer = CompressedBytes;
            compressor.NextOut = 0;
            compressor.AvailableBytesOut = CompressedBytes.Length;

            rc = compressor.Deflate(ZlibConstants.Z_FULL_FLUSH);

            CompressedBytes[3]++; // force an error in first compressed block // dinoch - ??
            compressor.AvailableBytesIn = TextToCompress.Length - 3;

            rc = compressor.Deflate(ZlibConstants.Z_FINISH);
            Assert.AreEqual<int>(ZlibConstants.Z_STREAM_END, rc, String.Format("at Deflate() [{0}]", compressor.Message));

            rc = compressor.EndDeflate();
            bufferSize = (int)(compressor.TotalBytesOut);

            ZlibCodec decompressor = new ZlibCodec(CompressionMode.Decompress);

            decompressor.InputBuffer = CompressedBytes;
            decompressor.NextIn = 0;
            decompressor.AvailableBytesIn = 2;

            decompressor.OutputBuffer = DecompressedBytes;
            decompressor.NextOut = 0;
            decompressor.AvailableBytesOut = DecompressedBytes.Length;

            rc = decompressor.Inflate(ZlibConstants.Z_NO_FLUSH);
            decompressor.AvailableBytesIn = bufferSize - 2;

            rc = decompressor.SyncInflate();

            bool gotException = false;
            try
            {
                rc = decompressor.Inflate(ZlibConstants.Z_FINISH);
            }
            catch (ZlibException ex1)
            {
                TestContext.WriteLine("Got Expected Exception: " + ex1);
                gotException = true;
            }

            Assert.IsTrue(gotException, "inflate should report DATA_ERROR");

            rc = decompressor.EndInflate();
            Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at EndInflate() [{0}]", decompressor.Message));

            int j = 0;
            for (; j < DecompressedBytes.Length; j++)
                if (DecompressedBytes[j] == 0)
                    break;

            var result = System.Text.ASCIIEncoding.ASCII.GetString(DecompressedBytes, 0, j);

            Assert.AreEqual<int>(TextToCompress.Length, result.Length + 3, "Strings are unequal lengths");

            Console.WriteLine("orig length: {0}", TextToCompress.Length);
            Console.WriteLine("compressed length: {0}", compressor.TotalBytesOut);
            Console.WriteLine("uncompressed length: {0}", decompressor.TotalBytesOut);
            Console.WriteLine("result length: {0}", result.Length);
            Console.WriteLine("result of inflate:\n(Thi){0}", result);
        }

        [TestMethod]
        public void Zlib_Codec_TestLargeDeflateInflate()
        {
            int rc;
            int j;
            int bufferSize = 80000;
            byte[] compressedBytes = new byte[bufferSize];
            byte[] workBuffer = new byte[bufferSize / 4];

            ZlibCodec compressingStream = new ZlibCodec();

            rc = compressingStream.InitializeDeflate(CompressionLevel.LEVEL1_BEST_SPEED);
            Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at InitializeDeflate() [{0}]", compressingStream.Message));

            compressingStream.OutputBuffer = compressedBytes;
            compressingStream.AvailableBytesOut = compressedBytes.Length;
            compressingStream.NextOut = 0;
            System.Random rnd = new Random();

            for (int k = 0; k < 4; k++)
            {
                switch (k)
                {
                    case 0:
                        // At this point, workBuffer is all zeroes, so it should compress very well.
                        break;

                    case 1:
                        // switch to no compression, keep same workBuffer (all zeroes):
                        compressingStream.SetDeflateParams(CompressionLevel.NONE, CompressionStrategy.DEFAULT);
                        break;

                    case 2:
                        // Insert data into workBuffer, and switch back to compressing mode.
                        // we'll use lengths of the same random byte:
                        for (int i = 0; i < workBuffer.Length / 1000; i++)
                        {
                            byte b = (byte)rnd.Next();
                            int n = 500 + rnd.Next(500);
                            for (j = 0; j < n; j++)
                                workBuffer[j + i] = b;
                            i += j - 1;
                        }
                        compressingStream.SetDeflateParams(CompressionLevel.LEVEL9_BEST_COMPRESSION, CompressionStrategy.FILTERED);
                        break;

                    case 3:
                        // insert totally random data into the workBuffer
                        rnd.NextBytes(workBuffer);
                        break;
                }

                compressingStream.InputBuffer = workBuffer;
                compressingStream.NextIn = 0;
                compressingStream.AvailableBytesIn = workBuffer.Length;
                rc = compressingStream.Deflate(ZlibConstants.Z_NO_FLUSH);
                Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at Deflate({0}) [{1}]", k, compressingStream.Message));

                if (k == 0)
                    Assert.AreEqual<int>(0, compressingStream.AvailableBytesIn, "Deflate should be greedy.");

                TestContext.WriteLine("Stage {0}: uncompressed/compresssed bytes so far:  ({1,6}/{2,6})",
                      k, compressingStream.TotalBytesIn, compressingStream.TotalBytesOut);
            }

            rc = compressingStream.Deflate(ZlibConstants.Z_FINISH);
            Assert.AreEqual<int>(ZlibConstants.Z_STREAM_END, rc, String.Format("at Deflate() [{0}]", compressingStream.Message));

            rc = compressingStream.EndDeflate();
            Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at EndDeflate() [{0}]", compressingStream.Message));

            TestContext.WriteLine("Final: uncompressed/compressed bytes: ({0,6},{1,6})",
                  compressingStream.TotalBytesIn, compressingStream.TotalBytesOut);

            ZlibCodec decompressingStream = new ZlibCodec(CompressionMode.Decompress);

            decompressingStream.InputBuffer = compressedBytes;
            decompressingStream.NextIn = 0;
            decompressingStream.AvailableBytesIn = bufferSize;

            // upon inflating, we overwrite the decompressedBytes buffer repeatedly
            int nCycles = 0;
            while (true)
            {
                decompressingStream.OutputBuffer = workBuffer;
                decompressingStream.NextOut = 0;
                decompressingStream.AvailableBytesOut = workBuffer.Length;
                rc = decompressingStream.Inflate(ZlibConstants.Z_NO_FLUSH);

                nCycles++;

                if (rc == ZlibConstants.Z_STREAM_END)
                    break;

                Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at Inflate() [{0}] TotalBytesOut={1}",
                                       decompressingStream.Message, decompressingStream.TotalBytesOut));
            }

            rc = decompressingStream.EndInflate();
            Assert.AreEqual<int>(ZlibConstants.Z_OK, rc, String.Format("at EndInflate() [{0}]", decompressingStream.Message));

            Assert.AreEqual<int>(4 * workBuffer.Length, (int)decompressingStream.TotalBytesOut);

            TestContext.WriteLine("compressed length: {0}", compressingStream.TotalBytesOut);
            TestContext.WriteLine("decompressed length (expected): {0}", 4 * workBuffer.Length);
            TestContext.WriteLine("decompressed length (actual)  : {0}", decompressingStream.TotalBytesOut);
            TestContext.WriteLine("decompression cycles: {0}", nCycles);
        }



        [TestMethod]
        public void Zlib_ZlibStream()
        {
            System.IO.MemoryStream msSinkCompressed;
            System.IO.MemoryStream msSinkDecompressed;
            ZlibStream zOut;
            String helloOriginal = "Hello, World!  This String will be compressed...";

            // first, compress:
            msSinkCompressed = new System.IO.MemoryStream();
            zOut = new ZlibStream(msSinkCompressed, CompressionMode.Compress, CompressionLevel.LEVEL9_BEST_COMPRESSION, true);
            CopyStream(StringToMemoryStream(helloOriginal), zOut);
            zOut.Close();

            // at this point, msSinkCompressed contains the compressed bytes

            // now, decompress:
            msSinkDecompressed = new System.IO.MemoryStream();
            zOut = new ZlibStream(msSinkDecompressed, CompressionMode.Decompress);
            msSinkCompressed.Position = 0;
            CopyStream(msSinkCompressed, zOut);

            string result = MemoryStreamToString(msSinkDecompressed);
            TestContext.WriteLine("decompressed: {0}", result);
            Assert.AreEqual<String>(helloOriginal, result);
        }


        [TestMethod]
        public void Zlib_CodecTest()
        {
            int sz = _rnd.Next(50000) + 50000;
            string FileName = System.IO.Path.Combine(TopLevelDir, "Zlib_CodecTest.txt");
            CreateAndFillFileText(FileName, sz);

            byte[] UncompressedBytes = ReadFile(FileName);

            foreach (Ionic.Zlib.CompressionLevel level in Enum.GetValues(typeof(Ionic.Zlib.CompressionLevel)))
            {
                TestContext.WriteLine("\n\n+++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                TestContext.WriteLine("trying compression level '{0}'", level.ToString());
                byte[] CompressedBytes = DeflateBuffer(UncompressedBytes, level);
                byte[] DecompressedBytes = InflateBuffer(CompressedBytes, UncompressedBytes.Length);
                CompareBuffers(UncompressedBytes, DecompressedBytes);
            }
            System.Threading.Thread.Sleep(2000);
        }



        private byte[] ReadFile(string f)
        {
            System.IO.FileInfo fi = new System.IO.FileInfo(f);

            byte[] buffer = new byte[fi.Length];
            //DecompressedBytes = new byte[fi.Length];

            using (var readStream = System.IO.File.OpenRead(f))
            {
                readStream.Read(buffer, 0, buffer.Length);
            }
            return buffer;
        }




        private byte[] InflateBuffer(byte[] b, int length)
        {
            int bufferSize = 1024;
            byte[] buffer = new byte[bufferSize];
            ZlibCodec decompressor = new ZlibCodec();
            byte[] DecompressedBytes = new byte[length];
            TestContext.WriteLine("\n============================================");
            TestContext.WriteLine("Size of Buffer to Inflate: {0} bytes.", b.Length);
            MemoryStream ms = new MemoryStream(DecompressedBytes);

            int rc = decompressor.InitializeInflate();

            decompressor.InputBuffer = b;
            decompressor.NextIn = 0;
            decompressor.AvailableBytesIn = b.Length;

            decompressor.OutputBuffer = buffer;

            // pass 1: deflate 
            do
            {
                decompressor.NextOut = 0;
                decompressor.AvailableBytesOut = buffer.Length;
                rc = decompressor.Inflate(ZlibConstants.Z_NO_FLUSH);

                if (rc != ZlibConstants.Z_OK && rc != ZlibConstants.Z_STREAM_END)
                    throw new Exception("inflating: " + decompressor.Message);

                //TestContext.WriteLine("got {0} decompressed bytes.", buffer.Length - decompressor.AvailableBytesOut);
                ms.Write(decompressor.OutputBuffer, 0, buffer.Length - decompressor.AvailableBytesOut);

                //TestContext.WriteLine("TBO({0}).", decompressor.TotalBytesOut);
                // at this point the OutputBuffer contains a batch of compressed bytes.
            }
            while (decompressor.AvailableBytesIn > 0 || decompressor.AvailableBytesOut == 0);

            // pass 2: finish and flush
            do
            {
                decompressor.NextOut = 0;
                decompressor.AvailableBytesOut = buffer.Length;
                rc = decompressor.Inflate(ZlibConstants.Z_FINISH);

                if (rc != ZlibConstants.Z_STREAM_END && rc != ZlibConstants.Z_OK)
                    throw new Exception("inflating: " + decompressor.Message);

                //TestContext.WriteLine("got {0} decompressed bytes.", buffer.Length - decompressor.AvailableBytesOut);
                if (buffer.Length - decompressor.AvailableBytesOut > 0)
                {
                    ms.Write(buffer, 0, buffer.Length - decompressor.AvailableBytesOut);
                }

                //TestContext.WriteLine("TBO({0}).", decompressor.TotalBytesOut);
            }
            while (decompressor.AvailableBytesIn > 0 || decompressor.AvailableBytesOut == 0);


            decompressor.EndInflate();
            TestContext.WriteLine("TBO({0}).", decompressor.TotalBytesOut);
            return DecompressedBytes;
        }




        private void CompareBuffers(byte[] a, byte[] b)
        {
            TestContext.WriteLine("\n============================================");
            TestContext.WriteLine("Comparing...");

            if (a.Length != b.Length)
                throw new Exception(String.Format("not equal size ({0}!={1})", a.Length, b.Length));

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    throw new Exception("not equal");
            }
        }



        private byte[] DeflateBuffer(byte[] b, CompressionLevel level)
        {
            int bufferSize = 1024;
            byte[] buffer = new byte[bufferSize];
            ZlibCodec compressor = new ZlibCodec();

            TestContext.WriteLine("\n============================================");
            TestContext.WriteLine("Size of Buffer to Deflate: {0} bytes.", b.Length);
            MemoryStream ms = new MemoryStream();

            int rc = compressor.InitializeDeflate(level);

            compressor.InputBuffer = b;
            compressor.NextIn = 0;
            compressor.AvailableBytesIn = b.Length;

            compressor.OutputBuffer = buffer;

            // pass 1: deflate 
            do
            //&&  < bufferSize)
            {
                compressor.NextOut = 0;
                compressor.AvailableBytesOut = buffer.Length;
                rc = compressor.Deflate(ZlibConstants.Z_NO_FLUSH);

                if (rc != ZlibConstants.Z_OK && rc != ZlibConstants.Z_STREAM_END)
                    throw new Exception("deflating: " + compressor.Message);

                //totalBytes += (CompressedBytes.Length - compressor.AvailableBytesOut);
                Console.WriteLine("got {0} compressed bytes.", buffer.Length - compressor.AvailableBytesOut);
                ms.Write(compressor.OutputBuffer, 0, buffer.Length - compressor.AvailableBytesOut);

                Console.WriteLine("TBO({0}).", compressor.TotalBytesOut);
                // at this point the OutputBuffer contains a batch of compressed bytes.
            }
            while (compressor.AvailableBytesIn > 0 || compressor.AvailableBytesOut == 0);

            // pass 2: finish and flush
            do
            {
                compressor.NextOut = 0;
                compressor.AvailableBytesOut = buffer.Length;
                rc = compressor.Deflate(ZlibConstants.Z_FINISH);

                if (rc != ZlibConstants.Z_STREAM_END && rc != ZlibConstants.Z_OK)
                    throw new Exception("deflating: " + compressor.Message);

                Console.WriteLine("got {0} compressed bytes.", buffer.Length - compressor.AvailableBytesOut);
                if (buffer.Length - compressor.AvailableBytesOut > 0)
                {
                    ms.Write(buffer, 0, buffer.Length - compressor.AvailableBytesOut);
                }

                Console.WriteLine("TBO({0}).", compressor.TotalBytesOut);
            }
            while (compressor.AvailableBytesIn > 0 || compressor.AvailableBytesOut == 0);


            compressor.EndDeflate();
            Console.WriteLine("TBO({0}).", compressor.TotalBytesOut);

            ms.Seek(0, SeekOrigin.Begin);
            byte[] c = new byte[compressor.TotalBytesOut];
            ms.Read(c, 0, c.Length);
            return c;
        }


        private const int WORKING_BUFFER_SIZE = 4000;


        [TestMethod]
        public void Zlib_Streams()
        {
            byte[] working = new byte[WORKING_BUFFER_SIZE];
            int n = -1;
            Int32[] Sizes = { 8000, 88000, 188000, 388000, 580000, 1580000 };

            for (int p = 0; p < Sizes.Length; p++)
            {
                // both binary and text files
                for (int m = 0; m < 2; m++)
                {
                    string FileToCompress = null;
                    int sz = _rnd.Next(Sizes[p]) + Sizes[p];
                    FileToCompress = System.IO.Path.Combine(TopLevelDir, String.Format("Zlib_Streams.{0}.{1}", p, (m == 0) ? "txt" : "bin"));
                    Assert.IsFalse(System.IO.File.Exists(FileToCompress), "The temporary file '{0}' already exists.", FileToCompress);
                    TestContext.WriteLine("Creating file {0}   {1} bytes", FileToCompress, sz);
                    if (m == 0)
                        CreateAndFillFileText(FileToCompress, sz);
                    else
                        _CreateAndFillBinary(FileToCompress, sz, false);

                    int crc1 = DoCrc(FileToCompress);
                    TestContext.WriteLine("Initial CRC: 0x{0:X8}", crc1);

                    // try both GZipStream and DeflateStream
                    for (int k = 0; k < 2; k++)
                    {
                        // compress with Ionic and System.IO.Compression
                        for (int i = 0; i < 2; i++)
                        {
                            string CompressedFile = String.Format("{0}.{1}.{2}.compressed", FileToCompress,
                                              (k == 0) ? "GZIP" : "DEFLATE",
                                              (i == 0) ? "Ionic" : "BCL");

                            using (var input = System.IO.File.OpenRead(FileToCompress))
                            {
                                using (var raw = System.IO.File.Create(CompressedFile))
                                {
                                    Stream compressor = null;
                                    try
                                    {
                                        int x = k + i * 2;
                                        switch (x)
                                        {
                                            case 0: // k == 0, i == 0
                                                compressor = new Ionic.Zlib.GZipStream(raw, CompressionMode.Compress, true);
                                                break;
                                            case 1: // k == 1, i == 0
                                                compressor = new Ionic.Zlib.DeflateStream(raw, CompressionMode.Compress, true);
                                                break;
                                            case 2: // k == 0, i == 1
                                                compressor = new System.IO.Compression.GZipStream(raw, System.IO.Compression.CompressionMode.Compress, true);
                                                break;
                                            case 3: // k == 1, i == 1
                                                compressor = new System.IO.Compression.DeflateStream(raw, System.IO.Compression.CompressionMode.Compress, true);
                                                break;
                                        }
                                        //TestContext.WriteLine("Compress with: {0} ..", compressor.GetType().FullName);

                                        TestContext.WriteLine("........{0} ...", System.IO.Path.GetFileName(CompressedFile));

                                        n = -1;
                                        while (n != 0)
                                        {
                                            if (n > 0)
                                            {
                                                compressor.Write(working, 0, n);
                                            }
                                            n = input.Read(working, 0, working.Length);
                                        }

                                    }
                                    finally
                                    {
                                        if (compressor != null)
                                            compressor.Dispose();
                                    }
                                }
                            }

                            // now, decompress with Ionic and System.IO.Compression
                            for (int j = 0; j < 2; j++)
                            {
                                using (var input = System.IO.File.OpenRead(CompressedFile))
                                {
                                    Stream decompressor = null;
                                    try
                                    {
                                        int x = k + j * 2;
                                        switch (x)
                                        {
                                            case 0: // k == 0, j == 0
                                                decompressor = new Ionic.Zlib.GZipStream(input, CompressionMode.Decompress, true);
                                                break;
                                            case 1: // k == 1, j == 0
                                                decompressor = new Ionic.Zlib.DeflateStream(input, CompressionMode.Decompress, true);
                                                break;
                                            case 2: // k == 0, j == 1
                                                decompressor = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress, true);
                                                break;
                                            case 3: // k == 1, j == 1
                                                decompressor = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress, true);
                                                break;
                                        }

                                        //TestContext.WriteLine("Decompress: {0} ...", decompressor.GetType().FullName);
                                        string DecompressedFile =
                                            String.Format("{0}.{1}.decompressed", CompressedFile, (j == 0) ? "Ionic" : "BCL");

                                        TestContext.WriteLine("........{0} ...", System.IO.Path.GetFileName(DecompressedFile));

                                        using (var s2 = System.IO.File.Create(DecompressedFile))
                                        {
                                            n = -1;
                                            while (n != 0)
                                            {
                                                n = decompressor.Read(working, 0, working.Length);
                                                if (n > 0)
                                                    s2.Write(working, 0, n);
                                            }
                                        }

                                        int crc2 = DoCrc(DecompressedFile);
                                        Assert.AreEqual<Int32>(crc1, crc2);

                                    }
                                    finally
                                    {
                                        if (decompressor != null)
                                            decompressor.Dispose();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            TestContext.WriteLine("Done.");
        }



        private int DoCrc(string filename)
        {
            byte[] working = new byte[WORKING_BUFFER_SIZE];
            int n = -1;
            int result = 0;
            using (System.IO.Stream a = System.IO.File.OpenRead(filename))
            {
                using (var b = new Ionic.Zlib.CrcCalculatorStream(a))
                {
                    n = -1;
                    while (n != 0)
                        n = b.Read(working, 0, working.Length);

                    //TestContext.WriteLine("File:{0}  CRC32:0x{1:X8}", System.IO.Path.GetFileName(filename), b.Crc32);
                    result = b.Crc32;
                }
            }

            return result;
        }



        private static void _CreateAndFillBinary(string Filename, Int64 size, bool zeroes)
        {
            Int64 bytesRemaining = size;
            System.Random rnd = new System.Random();
            // fill with binary data
            byte[] Buffer = new byte[20000];
            using (System.IO.Stream fileStream = new System.IO.FileStream(Filename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                while (bytesRemaining > 0)
                {
                    int sizeOfChunkToWrite = (bytesRemaining > Buffer.Length) ? Buffer.Length : (int)bytesRemaining;
                    if (!zeroes) rnd.NextBytes(Buffer);
                    fileStream.Write(Buffer, 0, sizeOfChunkToWrite);
                    bytesRemaining -= sizeOfChunkToWrite;
                }
                fileStream.Close();
            }
        }


        internal static void CreateAndFillFileText(string Filename, Int64 size)
        {
            Int64 bytesRemaining = size;
            System.Random rnd = new System.Random();
            // fill the file with text data
            using (System.IO.StreamWriter sw = System.IO.File.CreateText(Filename))
            {
                do
                {
                    // pick a word at random
                    string selectedWord = LoremIpsumWords[rnd.Next(LoremIpsumWords.Length)];
                    if (bytesRemaining < selectedWord.Length + 1)
                    {
                        sw.Write(selectedWord.Substring(0, (int)bytesRemaining));
                        bytesRemaining = 0;
                    }
                    else
                    {
                        sw.Write(selectedWord);
                        sw.Write(" ");
                        bytesRemaining -= (selectedWord.Length + 1);
                    }
                } while (bytesRemaining > 0);
                sw.Close();
            }
        }



        internal static string LoremIpsum =
"Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Integer " +
"vulputate, nibh non rhoncus euismod, erat odio pellentesque lacus, sit " +
"amet convallis mi augue et odio. Phasellus cursus urna facilisis " +
"quam. Suspendisse nec metus et sapien scelerisque euismod. Nullam " +
"molestie sem quis nisl. Fusce pellentesque, ante sed semper egestas, sem " +
"nulla vestibulum nulla, quis sollicitudin leo lorem elementum " +
"wisi. Aliquam vestibulum nonummy orci. Sed in dolor sed enim ullamcorper " +
"accumsan. Duis vel nibh. Class aptent taciti sociosqu ad litora torquent " +
"per conubia nostra, per inceptos hymenaeos. Sed faucibus, enim sit amet " +
"venenatis laoreet, nisl elit posuere est, ut sollicitudin tortor velit " +
"ut ipsum. Aliquam erat volutpat. Phasellus tincidunt vehicula " +
"eros. Curabitur vitae erat. " +
"\n " +
"Quisque pharetra lacus quis sapien. Duis id est non wisi sagittis " +
"adipiscing. Nulla facilisi. Etiam quam erat, lobortis eu, facilisis nec, " +
"blandit hendrerit, metus. Fusce hendrerit. Nunc magna libero, " +
"sollicitudin non, vulputate non, ornare id, nulla.  Suspendisse " +
"potenti. Nullam in mauris. Curabitur et nisl vel purus vehicula " +
"sodales. Class aptent taciti sociosqu ad litora torquent per conubia " +
"nostra, per inceptos hymenaeos. Cum sociis natoque penatibus et magnis " +
"dis parturient montes, nascetur ridiculus mus. Donec semper, arcu nec " +
"dignissim porta, eros odio tempus pede, et laoreet nibh arcu et " +
"nisl. Morbi pellentesque eleifend ante. Morbi dictum lorem non " +
"ante. Nullam et augue sit amet sapien varius mollis. " +
"\n " +
"Nulla erat lorem, fringilla eget, ultrices nec, dictum sed, " +
"sapien. Aliquam libero ligula, porttitor scelerisque, lobortis nec, " +
"dignissim eu, elit. Etiam feugiat, dui vitae laoreet faucibus, tellus " +
"urna molestie purus, sit amet pretium lorem pede in erat.  Ut non libero " +
"et sapien porttitor eleifend. Vestibulum ante ipsum primis in faucibus " +
"orci luctus et ultrices posuere cubilia Curae; In at lorem et lacus " +
"feugiat iaculis. Nunc tempus eros nec arcu tristique egestas. Quisque " +
"metus arcu, pretium in, suscipit dictum, bibendum sit amet, " +
"mauris. Aliquam non urna. Suspendisse eget diam. Aliquam erat " +
"volutpat. In euismod aliquam lorem. Mauris dolor nisl, consectetuer sit " +
"amet, suscipit sodales, rutrum in, lorem. Nunc nec nisl. Nulla ante " +
"libero, aliquam porttitor, aliquet at, imperdiet sed, diam. Pellentesque " +
"tincidunt nisl et ipsum. Suspendisse purus urna, semper quis, laoreet " +
"in, vestibulum vel, arcu. Nunc elementum eros nec mauris. " +
"\n " +
"Vivamus congue pede at quam. Aliquam aliquam leo vel turpis. Ut " +
"commodo. Integer tincidunt sem a risus. Cras aliquam libero quis " +
"arcu. Integer posuere. Nulla malesuada, wisi ac elementum sollicitudin, " +
"libero libero molestie velit, eu faucibus est ante eu libero. Sed " +
"vestibulum, dolor ac ultricies consectetuer, tellus risus interdum diam, " +
"a imperdiet nibh eros eget mauris. Donec faucibus volutpat " +
"augue. Phasellus vitae arcu quis ipsum ultrices fermentum. Vivamus " +
"ultricies porta ligula. Nullam malesuada. Ut feugiat urna non " +
"turpis. Vivamus ipsum. Vivamus eleifend condimentum risus. Curabitur " +
"pede. Maecenas suscipit pretium tortor. Integer pellentesque. " +
"\n " +
"Mauris est. Aenean accumsan purus vitae ligula. Lorem ipsum dolor sit " +
"amet, consectetuer adipiscing elit. Nullam at mauris id turpis placerat " +
"accumsan. Sed pharetra metus ut ante. Aenean vel urna sit amet ante " +
"pretium dapibus. Sed nulla. Sed nonummy, lacus a suscipit semper, erat " +
"wisi convallis mi, et accumsan magna elit laoreet sem. Nam leo est, " +
"cursus ut, molestie ac, laoreet id, mauris. Suspendisse auctor nibh. " +
"\n";

        static string[] LoremIpsumWords;



    }
}
