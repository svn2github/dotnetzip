/* -*-mode:java; c-basic-offset:2; -*- */
using System;
using Ionic.Zlib;

public class test_stream_deflate_inflate
{

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



    static void CopyStream(System.IO.Stream src, System.IO.Stream dest)
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


    [STAThread]
    public static void Main(System.String[] args)
    {
        try
        {
            System.IO.MemoryStream msSinkCompressed;
            System.IO.MemoryStream msSinkDecompressed;
            ZlibStream zOut;
            String helloOriginal = "Hello, World!  This String will be compressed...";

            // first, compress:
            msSinkCompressed = new System.IO.MemoryStream();
            zOut = new ZlibStream(msSinkCompressed, CompressionMode.Compress, CompressionLevel.LEVEL9_BEST_COMPRESSION);
            CopyStream(StringToMemoryStream(helloOriginal), zOut);
            zOut.Close();

            // at this point, msSinkCompressed contains the compressed bytes


            // now, decompress:
            msSinkDecompressed = new System.IO.MemoryStream();
            zOut = new ZlibStream(msSinkDecompressed, CompressionMode.Decompress);
            CopyStream(msSinkCompressed, zOut);

            System.Console.Out.WriteLine("decompressed: {0}", MemoryStreamToString(msSinkDecompressed));
        }
        catch (System.Exception e1)
        {
            Console.WriteLine("Exception: " + e1);
        }
    }
}