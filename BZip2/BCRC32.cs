// BCRC32.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2011 Dino Chiesa.
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License.
// See the file License.txt for the license details.
// More info on: http://dotnetzip.codeplex.com
//
// ------------------------------------------------------------------
//
// Last Saved: <2011-July-23 21:12:37>
//
// ------------------------------------------------------------------
//
// This module defines the BCRC32 class, which is a CRC-32 class that
// can do the BZip2 CRC32 algorithm, including bit reversal. The bit
// reversal is what distinguishes this CRC-32 from the CRC-32 that is
// used in PKZIP files, or GZIP files.
//
// ------------------------------------------------------------------


using System;

namespace Ionic.BZip2
{
    /// <summary>
    ///   Computes CRC-32 using the BZIP conventions - a polynomial of 0xEDB88320, and
    ///   reversed bits.
    /// </summary>
    public class CRC32
    {
        /// <summary>
        ///   Indicates the total number of bytes read on the CRC stream.
        ///   This is used when writing the ZipDirEntry when compressing files.
        /// </summary>
        public Int64 TotalBytesRead
        {
            get
            {
                return _TotalBytesRead;
            }
        }

        /// <summary>
        /// Indicates the current CRC for all blocks slurped in.
        /// </summary>
        public Int32 Crc32Result
        {
            get
            {
                // return one's complement of the running result
                return unchecked((Int32)(~_register));
            }
        }

        /// <summary>
        /// Returns the CRC32 for the specified stream.
        /// </summary>
        /// <param name="input">The stream over which to calculate the CRC32</param>
        /// <returns>the CRC32 calculation</returns>
        public Int32 GetCrc32(System.IO.Stream input)
        {
            return GetCrc32AndCopy(input, null);
        }

        /// <summary>
        /// Returns the CRC32 for the specified stream, and writes the input into the
        /// output stream.
        /// </summary>
        /// <param name="input">The stream over which to calculate the CRC32</param>
        /// <param name="output">The stream into which to deflate the input</param>
        /// <returns>the CRC32 calculation</returns>
        public Int32 GetCrc32AndCopy(System.IO.Stream input, System.IO.Stream output)
        {
            if (input == null)
                throw new Exception("The input stream must not be null.");

            unchecked
            {
                //UInt32 crc32Result;
                //crc32Result = 0xFFFFFFFF;
                byte[] buffer = new byte[BUFFER_SIZE];
                int readSize = BUFFER_SIZE;

                _TotalBytesRead = 0;
                int count = input.Read(buffer, 0, readSize);
                if (output != null) output.Write(buffer, 0, count);
                _TotalBytesRead += count;
                while (count > 0)
                {
                    SlurpBlock(buffer, 0, count);
                    count = input.Read(buffer, 0, readSize);
                    if (output != null) output.Write(buffer, 0, count);
                    _TotalBytesRead += count;
                }

                return (Int32)(~_register);
            }
        }


        /// <summary>
        /// Get the CRC32 for the given (word,byte) combo.  This is a computation
        /// defined by PKzip.
        /// </summary>
        /// <param name="W">The word to start with.</param>
        /// <param name="B">The byte to combine it with.</param>
        /// <returns>The CRC-ized result.</returns>
        public Int32 ComputeCrc32(Int32 W, byte B)
        {
            return _InternalComputeCrc32((UInt32)W, B);
        }

        internal Int32 _InternalComputeCrc32(UInt32 W, byte B)
        {
            return (Int32)(crc32Table[(W ^ B) & 0xFF] ^ (W >> 8));
        }

        /// <summary>
        /// Update the value for the running CRC32 using the given block of bytes.
        /// This is useful when using the CRC32() class in a Stream.
        /// </summary>
        /// <param name="block">block of bytes to slurp</param>
        /// <param name="offset">starting point in the block</param>
        /// <param name="count">how many bytes within the block to slurp</param>
        public void SlurpBlock(byte[] block, int offset, int count)
        {
            if (block == null)
                throw new Exception("The data buffer must not be null.");

            // bzip algorithm
            for (int i = 0; i < count; i++)
            {
                int x = offset + i;
                byte b = block[x];
                if (this.reverseBits)
                {
                    UInt32 temp = (_register >> 24) ^ b;
                    _register = (_register << 8) ^ crc32Table[temp];
                }
                else
                {
                    UInt32 temp = (_register & 0x000000FF) ^ b;
                    _register = (_register >> 8) ^ crc32Table[temp];
                }
            }
            _TotalBytesRead += count;
        }


        /// <summary>
        ///   Process one byte in the CRC.
        /// </summary>
        /// <param name = "b">the byte to include into the CRC .  </param>
        public void UpdateCRC(byte b)
        {
            if (this.reverseBits)
            {
                UInt32 temp = (_register >> 24) ^ b;
                _register = (_register << 8) ^ crc32Table[temp];
            }
            else
            {
                UInt32 temp = (_register & 0x000000FF) ^ b;
                _register = (_register >> 8) ^ crc32Table[temp];
            }
        }

        /// <summary>
        ///   Process a run of N 4-byte integers in the CRC.
        /// </summary>
        /// <param name = "inCh">the integer to include into the CRC .  </param>
        /// <param name = "repeat">the number of times that integer should be repeated. </param>
        public void UpdateCRC(int inCh, int repeat)
        {
            uint u = (uint) inCh;
            while (repeat-- > 0)
            {
                if (this.reverseBits)
                {
                    uint temp = (_register >> 24) ^ u;
                    _register = (_register << 8) ^ crc32Table[(temp >= 0)
                                                              ? temp
                                                              : (temp + 256)];
                }
                else
                {
                    UInt32 temp = (_register & 0x000000FF) ^ u;
                    _register = (_register >> 8) ^ crc32Table[(temp >= 0)
                                                              ? temp
                                                              : (temp + 256)];

                }
            }
        }



        private static uint ReverseBits(uint data)
        {
            unchecked
            {
                uint ret = data;
                ret = (ret & 0x55555555) << 1 | (ret >> 1) & 0x55555555;
                ret = (ret & 0x33333333) << 2 | (ret >> 2) & 0x33333333;
                ret = (ret & 0x0F0F0F0F) << 4 | (ret >> 4) & 0x0F0F0F0F;
                ret = (ret << 24) | ((ret & 0xFF00) << 8) | ((ret >> 8) & 0xFF00) | (ret >> 24);
                return ret;
            }
        }

        private static byte ReverseBits(byte data)
        {
            unchecked
            {
                uint u = (uint)data * 0x00020202;
                uint m = 0x01044010;
                uint s = u & m;
                uint t = (u << 2) & (m << 1);
                return (byte)((0x01001001 * (s + t)) >> 24);
            }
        }



        private static uint[] GenerateLookupTable(UInt32 dwPolynomial,
                                                  bool reverseBits)
        {
            uint[] crc32Table = new UInt32[256];
            unchecked
            {
                UInt32 dwCrc;
                byte i = 0;
                do
                {
                    dwCrc = i;
                    for (byte j = 8; j > 0; j--)
                    {
                        if ((dwCrc & 1) == 1)
                        {
                            dwCrc = (dwCrc >> 1) ^ dwPolynomial;
                        }
                        else
                        {
                            dwCrc >>= 1;
                        }
                    }
                    if (reverseBits)
                    {
                        crc32Table[ReverseBits(i)] = ReverseBits(dwCrc);
                    }
                    else
                    {
                        crc32Table[i] = dwCrc;
                    }
                    i++;
                } while (i!=0);
            }

#if VERBOSE
            Console.WriteLine();
            Console.WriteLine("private static readonly UInt32[] crc32Table = {");
            for (int i = 0; i < crc32Table.Length; i+=4)
            {
                Console.Write("   ");
                for (int j=0; j < 4; j++)
                {
                    Console.Write(" 0x{0:X8}U,", crc32Table[i+j]);
                }
                Console.WriteLine();
            }
            Console.WriteLine("};");
            Console.WriteLine();
#endif
            return crc32Table;
        }


        private uint gf2_matrix_times(uint[] matrix, uint vec)
        {
            uint sum = 0;
            int i=0;
            while (vec != 0)
            {
                if ((vec & 0x01)== 0x01)
                    sum ^= matrix[i];
                vec >>= 1;
                i++;
            }
            return sum;
        }

        private void gf2_matrix_square(uint[] square, uint[] mat)
        {
            for (int i = 0; i < 32; i++)
                square[i] = gf2_matrix_times(mat, mat[i]);
        }



        /// <summary>
        /// Combines the given CRC32 value with the current running total.
        /// </summary>
        /// <remarks>
        /// This is useful when using a divide-and-conquer approach to calculating a CRC.
        /// Multiple threads can each calculate a CRC32 on a segment of the data, and then
        /// combine the individual CRC32 values at the end.
        /// </remarks>
        /// <param name="crc">the crc value to be combined with this one</param>
        /// <param name="length">the length of data the CRC value was calculated on</param>
        public void Combine(int crc, int length)
        {
            uint[] even = new uint[32];     // even-power-of-two zeros operator
            uint[] odd = new uint[32];      // odd-power-of-two zeros operator

            if (length == 0)
                return;

            uint crc1= ~_register;
            uint crc2= (uint) crc;

            // put operator for one zero bit in odd
            odd[0] = 0xEDB88320;  // the CRC-32 polynomial
            uint row = 1;
            for (int i = 1; i < 32; i++)
            {
                odd[i] = row;
                row <<= 1;
            }

            // put operator for two zero bits in even
            gf2_matrix_square(even, odd);

            // put operator for four zero bits in odd
            gf2_matrix_square(odd, even);

            uint len2 = (uint) length;

            // apply len2 zeros to crc1 (first square will put the operator for one
            // zero byte, eight zero bits, in even)
            do {
                // apply zeros operator for this bit of len2
                gf2_matrix_square(even, odd);

                if ((len2 & 1)== 1)
                    crc1 = gf2_matrix_times(even, crc1);
                len2 >>= 1;

                if (len2 == 0)
                    break;

                // another iteration of the loop with odd and even swapped
                gf2_matrix_square(odd, even);
                if ((len2 & 1)==1)
                    crc1 = gf2_matrix_times(odd, crc1);
                len2 >>= 1;


            } while (len2 != 0);

            crc1 ^= crc2;

            _register= ~crc1;

            //return (int) crc1;
            return;
        }


        private CRC32()
        {
        }

        /// <summary>
        ///   Create an instance of the BZip2-compatible CRC-32 creator class.
        /// </summary>
        public static CRC32 Create()
        {
            return Create(true);
        }

        /// <summary>
        ///   Create an instance of the BZip2-compatible CRC-32 creator class,
        ///   specifying whether to reverse data bits or not.
        /// </summary>
        public static CRC32 Create(bool reverseBits)
        {
            var crc32 = new CRC32();
            crc32.reverseBits = reverseBits;
            crc32.crc32Table = GenerateLookupTable(0xEDB88320, reverseBits);
            return crc32;
        }

        /// <summary>
        ///   Reset the CRC-32 class - clear the CRC register.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Use this when employing a single CRC32 instance to compute
        ///     multiple, distinct CRCs on multiple, distinct data blocks.
        ///   </para>
        /// </remarks>
        public void Reset()
        {
            _register = 0xFFFFFFFFU;
        }


        // private member vars
        private Int64 _TotalBytesRead;
        private bool reverseBits;
        private UInt32[] crc32Table;
        private const int BUFFER_SIZE = 8192;
        private UInt32 _register = 0xFFFFFFFFU;
    }
}