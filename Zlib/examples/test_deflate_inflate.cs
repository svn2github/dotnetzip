using System;
using Ionic.Zlib;

// Test deflate() with small buffers

class Test_deflate_inflate
{

    internal readonly string TextToCompress = "Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Integer vulputate, nibh non rhoncus euismod, erat odio pellentesque lacus, sit amet convallis mi augue et odio. Phasellus cursus urna facilisis quam. Suspendisse nec metus et sapien scelerisque euismod. Nullam molestie sem quis nisl. Fusce pellentesque, ante sed semper egestas, sem nulla vestibulum nulla, quis sollicitudin leo lorem elementum wisi. Aliquam vestibulum nonummy orci. Sed in dolor sed enim ullamcorper accumsan. Duis vel nibh. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos hymenaeos. Sed faucibus, enim sit amet venenatis laoreet, nisl elit posuere est, ut sollicitudin tortor velit ut ipsum. Aliquam erat volutpat. Phasellus tincidunt vehicula eros. Curabitur vitae erat.";

    [STAThread]
    public static void Main(System.String[] args)
    {
        try
        {
            var x = new Test_deflate_inflate();
            x.Run();
        }
        catch (System.Exception e1)
        {
            Console.WriteLine("Exception: " + e1);
        }
    }

    private void Run()
    {
        int rc;
        int bufferSize = 40000;
        byte[] CompressedBytes = new byte[bufferSize];
        byte[] DecompressedBytes = new byte[bufferSize];

        ZlibCodec compressor = new ZlibCodec();

        rc = compressor.InitializeDeflate(CompressionLevel.DEFAULT);
        CheckForError(compressor, rc, "deflateInit");

        compressor.InputBuffer = System.Text.ASCIIEncoding.ASCII.GetBytes(TextToCompress);
        compressor.NextIn = 0;

        compressor.OutputBuffer = CompressedBytes;
        compressor.NextOut = 0;

        while (compressor.TotalBytesIn != TextToCompress.Length && compressor.TotalBytesOut < bufferSize)
        {
            compressor.AvailableBytesIn = compressor.AvailableBytesOut = 1; // force small buffers
            rc = compressor.Deflate(ZlibConstants.Z_NO_FLUSH);
            CheckForError(compressor, rc, "deflate");
        }

        while (true)
        {
            compressor.AvailableBytesOut = 1;
            rc = compressor.Deflate(ZlibConstants.Z_FINISH);
            if (rc == ZlibConstants.Z_STREAM_END)
                break;
            CheckForError(compressor, rc, "deflate");
        }

        rc = compressor.EndDeflate();
        CheckForError(compressor, rc, "deflateEnd");

        ZlibCodec decompressor = new ZlibCodec();

        decompressor.InputBuffer = CompressedBytes;
        decompressor.NextIn = 0;
        decompressor.OutputBuffer = DecompressedBytes;
        decompressor.NextOut = 0;

        rc = decompressor.InitializeInflate();
        CheckForError(decompressor, rc, "inflateInit");

        while (decompressor.TotalBytesOut < DecompressedBytes.Length && decompressor.TotalBytesIn < CompressedBytes.Length)
        {
            decompressor.AvailableBytesIn = decompressor.AvailableBytesOut = 1; /* force small buffers */
            rc = decompressor.Inflate(ZlibConstants.Z_NO_FLUSH);
            if (rc == ZlibConstants.Z_STREAM_END)
                break;
            CheckForError(decompressor, rc, "inflate");
        }

        rc = decompressor.EndInflate();
        CheckForError(decompressor, rc, "inflateEnd");

        int i = 0;
        for (; i < TextToCompress.Length; i++)
            if (TextToCompress[i] == 0)
                break;
        int j = 0;
        for (; j < DecompressedBytes.Length; j++)
            if (DecompressedBytes[j] == 0)
                break;

        if (i != j)
        {
            Console.WriteLine("bad inflate");
            System.Environment.Exit(1);
        }

        for (i = 0; i < j; i++)
            if (TextToCompress[i] != DecompressedBytes[i])
                break;
        if (i != j)
        {
            Console.WriteLine("bad inflate");
            System.Environment.Exit(1);
        }


        var result = System.Text.ASCIIEncoding.ASCII.GetString(DecompressedBytes, 0, j);

        Console.WriteLine("orig length: {0}", TextToCompress.Length);
        Console.WriteLine("compressed length: {0}", compressor.TotalBytesOut);
        Console.WriteLine("decompressed length: {0}", decompressor.TotalBytesOut);
        Console.WriteLine("result length: {0}", result.Length);
        Console.WriteLine("result of inflate:\n{0}", result);
        return;
    }


    internal static void CheckForError(ZlibCodec z, int err, System.String msg)
    {
        if (err != ZlibConstants.Z_OK)
        {
            if (z.Message != null)
                System.Console.Out.Write(z.Message + " ");
            System.Console.Out.WriteLine(msg + " error: " + err);
            System.Environment.Exit(1);
        }
    }
}