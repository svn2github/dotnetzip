using System;
using System.Text;
using System.Collections.Generic;
using Ionic.Zlib;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ionic.Zlib.Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class UnitTest1
    {
        public UnitTest1()
        {
            //
            // TODO: Add constructor logic here
            //
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
        #endregion

        [TestMethod]
        public void BasicDeflateAndInflate()
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
        public void BasicDictionaryDeflateInflate()
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

            Console.WriteLine("orig length: {0}", TextToCompress.Length);
            Console.WriteLine("compressed length: {0}", compressor.TotalBytesOut);
            Console.WriteLine("uncompressed length: {0}", decompressor.TotalBytesOut);
            Console.WriteLine("result length: {0}", result.Length);
            Console.WriteLine("result of inflate:\n{0}", result);
        }

        [TestMethod]
        public void TestFlushSync()
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
        public void TestLargeDeflateInflate()
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
        public void TestStreamCompression()
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

            string result= MemoryStreamToString(msSinkDecompressed);
            TestContext.WriteLine("decompressed: {0}", result);
            Assert.AreEqual<String>(helloOriginal, result);
        }

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

    }
}
