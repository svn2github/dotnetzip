// Shared.cs
//
// Copyright (c) 2006, 2007 Microsoft Corporation.  All rights reserved.
//
// Part of an implementation of a zipfile class library. 
// See the file ZipFile.cs for further information.
//
// Tue, 27 Mar 2007  15:30


using System;

namespace Ionic.Utils.Zip
{
    /// <summary>
    /// Collects general purpose utility methods.
    /// </summary>
  public class Shared
    {
      /// <summary>
      /// Round the given DateTime value to an even second value.  Round up in the case of odd seconds. 
      /// This is most nautrally an extension method for the DateTime class but this library is 
      /// built for .NET 2.0, not for .NET 3.5;  This means extension methods are a no-no.  
      /// </summary>
      /// <param name="source">The DateTime value to round</param>
      /// <returns>The ruonded DateTime value</returns>
      public static DateTime RoundToEvenSecond(DateTime source)
      {
          // round to nearest second:
          if ((source.Second % 2) == 1)
              source += new TimeSpan(0, 0, 1);

          DateTime dtRounded = new DateTime(source.Year, source.Month, source.Day, source.Hour, source.Minute, source.Second);
          //if (source.Millisecond >= 500) dtRounded = dtRounded.AddSeconds(1);
          return dtRounded;
      }

      /// <summary>
      /// Utility routine for transforming path names. 
      /// </summary>
      /// <param name="pathname">source path.</param>
      /// <returns>transformed path</returns>
      public static string TrimVolumeAndSwapSlashes(string pathname)
      {
          return (((pathname[1] == ':') && (pathname[2] == '\\')) ? pathname.Substring(3) : pathname)
              .Replace('\\', '/');
      }

        internal static byte[] AsciiStringToByteArray(string data)
        {
            byte[] a = System.Text.Encoding.ASCII.GetBytes(data);
            return a;
        }

        internal static string StringFromBuffer(byte[] buf, int start, int maxlength)
        {
            int i;
            char[] c = new char[maxlength];
            for (i = 0; (i < maxlength) && (i < buf.Length) && (buf[i] != 0); i++)
            {
                c[i] = (char)buf[i]; // System.BitConverter.ToChar(buf, start+i*2);
            }
            string s = new System.String(c, 0, i);
            return s;
        }

        internal static int ReadSignature(System.IO.Stream s)
        {
            int n = 0;
            byte[] sig = new byte[4];
            n = s.Read(sig, 0, sig.Length);
            if (n != sig.Length) throw new BadReadException("Could not read signature - no data!");
            int signature = (((sig[3] * 256 + sig[2]) * 256) + sig[1]) * 256 + sig[0];
            return signature;
        }

        /// <summary>
        /// Finds a signature in the zip stream. This is useful for finding 
        /// the end of a zip entry, for example. 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="SignatureToFind"></param>
        /// <returns></returns>
        protected internal static long FindSignature(System.IO.Stream s, int SignatureToFind)
        {
            long startingPosition = s.Position;

            int BATCH_SIZE = 1024;
            byte[] targetBytes = new byte[4];
            targetBytes[0] = (byte)(SignatureToFind >> 24);
            targetBytes[1] = (byte)((SignatureToFind & 0x00FF0000) >> 16);
            targetBytes[2] = (byte)((SignatureToFind & 0x0000FF00) >> 8);
            targetBytes[3] = (byte)(SignatureToFind & 0x000000FF);
            byte[] batch = new byte[BATCH_SIZE];
            int n = 0;
            bool success = false;
            do
            {
                n = s.Read(batch, 0, batch.Length);
                if (n != 0)
                {
                    for (int i = 0; i < n; i++)
                    {
                        if (batch[i] == targetBytes[3])
                        {
                            s.Seek(i - n, System.IO.SeekOrigin.Current);
                            int sig = ReadSignature(s);
                            success = (sig == SignatureToFind);
                            if (!success) s.Seek(-3, System.IO.SeekOrigin.Current);
                            break; // out of for loop
                        }
                    }
                }
                else break;
                if (success) break;
            } while (true);
            if (!success)
            {
                s.Seek(startingPosition, System.IO.SeekOrigin.Begin);
                return -1;  // or throw?
            }

            // subtract 4 for the signature.
            long bytesRead = (s.Position - startingPosition) - 4;
            // number of bytes read, should be the same as compressed size of file            
            return bytesRead;
        }


      internal 
       static DateTime PackedToDateTime(Int32 packedDateTime)
        {
            Int16 packedTime = (Int16)(packedDateTime & 0x0000ffff);
            Int16 packedDate = (Int16)((packedDateTime & 0xffff0000) >> 16);

            int year = 1980 + ((packedDate & 0xFE00) >> 9);
            int month = (packedDate & 0x01E0) >> 5;
	    int day = packedDate & 0x001F;

            int hour = (packedTime & 0xF800) >> 11;
            int minute = (packedTime & 0x07E0) >> 5;
            //int second = packedTime & 0x001F;
            int second = (packedTime & 0x001F) * 2;

            DateTime d = System.DateTime.Now;
            try { d = new System.DateTime(year, month, day, hour, minute, second, 0); }
            catch
            {
                Console.Write("\nInvalid date/time?:\nyear: {0} ", year);
                Console.Write("month: {0} ", month);
                Console.WriteLine("day: {0} ", day);
                Console.WriteLine("HH:MM:SS= {0}:{1}:{2}", hour, minute, second);
            }

            return d;
        }

      
      internal 
       static Int32 DateTimeToPacked(DateTime time)
        {
            UInt16 packedDate = (UInt16)((time.Day & 0x0000001F) | ((time.Month << 5) & 0x000001E0) | (((time.Year - 1980) << 9) & 0x0000FE00));
            UInt16 packedTime = (UInt16)((time.Second/2 & 0x0000001F) | ((time.Minute << 5) & 0x000007E0) | ((time.Hour << 11) & 0x0000F800));

	    // for debugging only
//             int hour = (packedTime & 0xF800) >> 11;
//             int minute = (packedTime & 0x07E0) >> 5;
//             int second = (packedTime & 0x001F)*2;

// 	    Console.WriteLine("regly      = {0:D2}:{1:d2}:{2:D2}", time.Hour, time.Minute, time.Second);
// 	    Console.WriteLine("msdos-ized = {0:D2}:{1:d2}:{2:D2}", hour, minute, second);
// 	    // end debugging stuff


	    Int32 result=  (Int32)(((UInt32)(packedDate << 16)) | packedTime);
	    return  result;
        }


public static System.IO.MemoryStream StringToMemoryStream(string s)
{
  System.IO.MemoryStream m = new   System.IO.MemoryStream();
  System.IO.StreamWriter sw = new   System.IO.StreamWriter(m);
  sw.Write(s);
  sw.Flush();
  return m;
}


    }


    /// <summary>
    /// A write-only Stream, used for bookkeeping on ASP.NET output streams.
    /// </summary>
    internal class CountingOutputStream : System.IO.Stream
    {
        private System.IO.Stream _s;
	private int _bytesWritten;
        /// <summary>
        /// The  constructor.
        /// </summary>
        /// <param name="s">The underlying stream</param>
        public CountingOutputStream(System.IO.Stream s)
            : base()
        {
            _s = s;
	    _bytesWritten= 0;
        }

	public int BytesWritten
	{
	  get {return _bytesWritten;}
	}


        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
	  _s.Write( buffer,  offset,  count);
	  _bytesWritten += count;
        }

        public override bool CanRead
        {
            get { return false; }
        }
        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
	  _s.Flush();
        }

        public override long Length
        {
            get { return _bytesWritten; }
        }

        public override long Position
        {
	  get { return _bytesWritten; }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
	  return _s.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
	  _s.SetLength(value);
        }
    }


}
