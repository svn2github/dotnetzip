// BZip2OutputStream.cs
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
// Last Saved: <2011-July-24 09:26:39>
//
// ------------------------------------------------------------------
//
// This module defines the BZip2OutputStream class, which is a compressing
// stream that handles BZIP2. This code is derived from Apache commons source code.
// The license below applies to the original Apache code.
//
// ------------------------------------------------------------------


/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

using System;
using System.IO;


namespace Ionic.BZip2
{
    /// <summary>
    ///   A write-only decorator stream that compresses data as it is
    ///   written using the BZip2 algorithm.
    /// </summary>
    public class BZip2OutputStream : System.IO.Stream
    {
        private static readonly int SETMASK = (1 << 21);
        private static readonly int CLEARMASK = (~SETMASK);
        private static readonly byte GREATER_ICOST = 15;
        private static readonly byte LESSER_ICOST = 0;
        private static readonly int SMALL_THRESH = 20;
        private static readonly int DEPTH_THRESH = 10;
        private static readonly int WORK_FACTOR = 30;

        int totalBytesWritten;
        bool leaveOpen;

        /**
         * Index of the last char in the block, so the block size == last + 1.
         */
        private int last;

        /**
         * Index in fmap[] of original string after sorting.
         */
        private int origPtr;

        /**
         * Always: in the range 0 .. 9. The current block size is 100000 * this
         * number.
         */
        private int blockSize100k;

        private bool blockRandomised;

        private int bsBuff;
        private int bsLive;
        private readonly CRC32 crc = Ionic.BZip2.CRC32.Create();
        private int nInUse;
        private int nMTF;

        /*
         * Used when sorting. If too many long comparisons happen, we stop sorting,
         * randomise the block slightly, and try again.
         */
        private int workDone;
        private int workLimit;
        private bool firstAttempt;

        private int currentByte = -1;
        private int runLength = 0;

        private int blockCRC;
        private int combinedCRC;
        private int outBlockFillThreshold;

        /**
         * All memory intensive stuff.
         */
        private CompressionState cstate;
        private Stream output;

        /**
         * Knuth's increments seem to work better than Incerpi-Sedgewick here.
         * Possibly because the number of elems to sort is usually small, typically
         * &lt;= 20.
         */
        private static readonly int[] increments = { 1, 4, 13, 40, 121, 364, 1093, 3280,
                                                     9841, 29524, 88573, 265720, 797161,
                                                     2391484 };

        /// <summary>
        ///   Constructs a new <c>BZip2OutputStream</c>, that sends its
        ///   compressed output to the given output stream.
        /// </summary>
        ///
        /// <example>
        ///
        ///   This example reads a file, then compresses it with bzip2 file,
        ///   and writes the compressed data into a newly created file.
        ///
        ///   <code>
        ///   var fname = "logfile.log";
        ///   using (var fs = File.OpenRead(fname))
        ///   {
        ///       var outFname = fname + ".bz2";
        ///       using (var output = File.Create(outFname))
        ///       {
        ///           using (var compressor = new Ionic.BZip2.BZip2OutputStream(output))
        ///           {
        ///               byte[] buffer = new byte[2048];
        ///               int n;
        ///               while ((n = fs.Read(buffer, 0, buffer.Length)) > 0)
        ///               {
        ///                   compressor.Write(buffer, 0, n);
        ///               }
        ///           }
        ///       }
        ///   }
        ///   </code>
        /// </example>
        public BZip2OutputStream(Stream output)
            : this(output, BZip2.MaxBlockSize, false)
        {
        }


        /// <summary>
        ///   Constructs a new <c>BZip2OutputStream</c> with specified blocksize.
        /// </summary>
        ///   <param name = "output">the destination stream.</param>
        ///   <param name = "blockSize">the blockSize in units of 100000 bytes.</param>
        public BZip2OutputStream(Stream output, int blockSize)
            : this(output, blockSize, false)
        {
        }


        /// <summary>
        ///   Constructs a new <c>CBZip2OutputStream</c>.
        /// </summary>
        ///   <param name = "output">the destination stream.</param>
        /// <param name = "leaveOpen">
        ///   whether to leave the captive stream open upon closing this stream.
        /// </param>
        public BZip2OutputStream(Stream output, bool leaveOpen)
            : this(output, BZip2.MaxBlockSize, leaveOpen)
        {
        }


        /// <summary>
        ///   Constructs a new <c>CBZip2OutputStream</c> with specified blocksize.
        /// </summary>
        ///
        /// <param name = "output">the destination stream.</param>
        /// <param name = "blockSize">the blockSize in units of 100000 bytes.</param>
        /// <param name = "leaveOpen">
        ///   whether to leave the captive stream open upon closing this stream.
        /// </param>
        public BZip2OutputStream(Stream output, int blockSize, bool leaveOpen)
        {
            if (blockSize < BZip2.MinBlockSize)
                throw new ArgumentException("blockSize(" + blockSize
                                            + ") < 1");

            if (blockSize > BZip2.MaxBlockSize)
                throw new ArgumentException("blockSize(" + blockSize
                                            + ") > 9");

            this.blockSize100k = blockSize;
            this.output = output;
            this.leaveOpen = leaveOpen;
            init();
        }


        /**
         * Chooses a blocksize based on the given length of the data to compress.
         *
         */
        public static int chooseBlockSize(long inputLength)
        {
            return (inputLength > 0)
                ? (int) Math.Min((inputLength / 132000) + 1, BZip2.MaxBlockSize)
                : BZip2.MaxBlockSize;
        }


        /// <summary>
        ///   Write a single byte to the output stream.
        /// </summary>
        public override void WriteByte(byte b)
        {
            if (this.output == null)
                throw new IOException("the stream is closed");

            write0(b);
        }


        /* add_pair_to_block ( EState* s ) */
        private void AddRunToOutputBlock()
        {
            int previousLast = this.last;

            if (previousLast < this.outBlockFillThreshold)
            {
                byte b = (byte) this.currentByte;
                byte[] block = this.cstate.block;
                this.cstate.inUse[b] = true;
                int runLengthShadow = this.runLength;
                this.crc.UpdateCRC(b, runLengthShadow);

                switch (runLengthShadow)
                {
                    case 1:
                        block[previousLast + 2] = b;
                        this.last = previousLast + 1;
                        break;

                    case 2:
                        block[previousLast + 2] = b;
                        block[previousLast + 3] = b;
                        this.last = previousLast + 2;
                        break;

                    case 3:
                        block[previousLast + 2] = b;
                        block[previousLast + 3] = b;
                        block[previousLast + 4] = b;
                        this.last = previousLast + 3;
                        break;

                    default:
                        runLengthShadow -= 4;
                        this.cstate.inUse[runLengthShadow] = true;
                        block[previousLast + 2] = b;
                        block[previousLast + 3] = b;
                        block[previousLast + 4] = b;
                        block[previousLast + 5] = b;
                        block[previousLast + 6] = (byte) runLengthShadow;
                        this.last = previousLast + 5;
                        break;
                }

                totalBytesWritten += this.last - previousLast;
            }
            else {
                endBlock();
                initBlock();
                AddRunToOutputBlock();
            }
        }


        void finish()
        {
            if (this.output != null)
            {
                try
                {
                    if (this.runLength > 0)
                    {
                        AddRunToOutputBlock();
                    }
                    this.currentByte = -1;
                    endBlock();
                    endCompression();
                }
                finally
                {
                    this.output = null;
                    this.cstate = null;
                }
            }
        }

        /// <summary>
        ///   Close the stream.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This may or may not close the underlying stream.  Check the
        ///     constructors that accept a bool value.
        ///   </para>
        /// </remarks>
        public override void Close()
        {
            if (output != null)
            {
                Stream o = this.output;
                finish();
                if (!leaveOpen)
                    o.Close();
            }
        }

        /// <summary>
        ///   Flush the stream.
        /// </summary>
        public override void Flush()
        {
            if (this.output != null)
                this.output.Flush();
        }

        /**
         * Writes magic bytes like BZ on the first position of the stream
         * and bytes indiciating the file-format, which is
         * huffmanised, followed by a digit indicating blockSize100k.
         * @throws IOException if the magic bytes could not been written
         */
        private void init()
        {
            bsPutUByte('B');
            bsPutUByte('Z');

            this.cstate = new CompressionState(this.blockSize100k);

            // huffmanised magic bytes
            bsPutUByte('h');
            bsPutUByte('0' + this.blockSize100k);

            this.combinedCRC = 0;
            initBlock();
        }



        private void initBlock()
        {
            // blockNo++;
            this.crc.Reset();
            this.last = -1;
            // ch = 0;

            bool[] inUse = this.cstate.inUse;
            for (int i = 256; --i >= 0;)
            {
                inUse[i] = false;
            }

            // 20 is just a paranoia constant. The
            this.outBlockFillThreshold = (this.blockSize100k * BZip2.BlockSizeMultiple) - 20;
        }


        private void endBlock()
        {
            this.blockCRC = this.crc.Crc32Result;
            this.combinedCRC = (this.combinedCRC << 1) | (this.combinedCRC >> 31);
            this.combinedCRC ^= this.blockCRC;

            // empty block at end of file
            if (this.last == -1)
                return;

            /* sort the block and establish posn of original string */
            blockSort();

            /*
             * A 6-byte block header, the value chosen arbitrarily as 0x314159265359
             * :-). A 32 bit value does not really give a strong enough guarantee
             * that the value will not appear by chance in the compressed
             * datastream. Worst-case probability of this event, for a 900k block,
             * is about 2.0e-3 for 32 bits, 1.0e-5 for 40 bits and 4.0e-8 for 48
             * bits. For a compressed file of size 100Gb -- about 100000 blocks --
             * only a 48-bit marker will do. NB: normal compression/ decompression
             * donot rely on these statistical properties. They are only important
             * when trying to recover blocks from damaged files.
             */
            bsPutUByte(0x31);
            bsPutUByte(0x41);
            bsPutUByte(0x59);
            bsPutUByte(0x26);
            bsPutUByte(0x53);
            bsPutUByte(0x59);

            /* Now the block's CRC, so it is in a known place. */
            bsPutInt(this.blockCRC);

            /* Now a single bit indicating randomisation. */
            if (this.blockRandomised)
                bsW(1, 1);
            else
                bsW(1, 0);

            /* Finally, block's contents proper. */
            moveToFrontCodeAndSend();
        }

        private void endCompression()
        {
            // Now another magic 48-bit number, 0x177245385090, to indicate the end
            // of the last block. (sqrt(pi), if you want to know)
            bsPutUByte(0x17);
            bsPutUByte(0x72);
            bsPutUByte(0x45);
            bsPutUByte(0x38);
            bsPutUByte(0x50);
            bsPutUByte(0x90);

            bsPutInt(this.combinedCRC);
            bsFinishedWithStream();
        }


        /// <summary>
        ///   the blocksize parameter specified at construction time.
        /// </summary>
        public int BlockSize
        {
            get
            {
                return this.blockSize100k;
            }
        }


        /// <summary>
        ///   Write data to the stream.
        /// </summary>
        /// <remarks>
        ///
        /// <para>
        ///   Use the <c>BZip2OutputStream</c> to compress data while writing:
        ///   create a <c>BZip2OutputStream</c> with a writable output stream.
        ///   Then call <c>Write()</c> on that <c>BZip2OutputStream</c>, providing
        ///   uncompressed data as input.  The data sent to the output stream will
        ///   be the compressed form of the input data.
        /// </para>
        ///
        /// <para>
        ///   A <c>BZip2OutputStream</c> can be used only for <c>Write()</c> not for <c>Read()</c>.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <param name="buffer">The buffer holding data to write to the stream.</param>
        /// <param name="offset">the offset within that data array to find the first byte to write.</param>
        /// <param name="count">the number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset < 0)
                throw new IndexOutOfRangeException(String.Format("offset ({0}) must be > 0", offset));
            if (count < 0)
                throw new IndexOutOfRangeException(String.Format("count ({0}) must be > 0", count));
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException(String.Format("offset({0}) count({1}) bLength({2})",
                                                                 offset, count, buffer.Length));
            if (this.output == null)
                throw new IOException("the stream is not open");

            for (int hi = offset + count; offset < hi;)
                write0(buffer[offset++]);
        }


        private void write0(byte b)
        {
            // handle run-length-encoding
            if (this.currentByte != -1)
            {
                if (this.currentByte == b)
                {
                    if (++this.runLength > 254)
                    {
                        AddRunToOutputBlock();
                        this.currentByte = -1;
                        this.runLength = 0;
                    }
                    // else nothing to do
                }
                else
                {
                    AddRunToOutputBlock();
                    this.runLength = 1;
                    this.currentByte = b;
                }
            }
            else
            {
                this.currentByte = b;
                this.runLength++;
            }
        }


        private static void hbAssignCodes(int[] code,  byte[] length,
                                          int minLen, int maxLen,
                                          int alphaSize)
        {
            int vec = 0;
            for (int n = minLen; n <= maxLen; n++)
            {
                for (int i = 0; i < alphaSize; i++)
                {
                    if ((length[i] & 0xff) == n)
                    {
                        code[i] = vec;
                        vec++;
                    }
                }
                vec <<= 1;
            }
        }


        private void bsFinishedWithStream()
        {
            while (this.bsLive > 0)
            {
                byte ch = (byte)(this.bsBuff >> 24 & 0xff);
                this.output.WriteByte(ch); // write 8-bit
                this.bsBuff <<= 8;
                this.bsLive -= 8;
            }
        }

        private void bsW(int n,  int v)
        {
            Stream outShadow = this.output;
            int bsLiveShadow = this.bsLive;
            int bsBuffShadow = this.bsBuff;

            while (bsLiveShadow >= 8)
            {
                outShadow.WriteByte ((byte)(bsBuffShadow >> 24 & 0xff));
                bsBuffShadow <<= 8;
                bsLiveShadow -= 8;
            }

            this.bsBuff = bsBuffShadow | (v << (32 - bsLiveShadow - n));
            this.bsLive = bsLiveShadow + n;
        }

        private void bsPutUByte(int c)
        {
            bsW(8, c);
        }

        private void bsPutInt(int u)
        {
            bsW(8, (u >> 24) & 0xff);
            bsW(8, (u >> 16) & 0xff);
            bsW(8, (u >> 8) & 0xff);
            bsW(8, u & 0xff);
        }

        private void sendMTFValues()
        {
            byte[][] len = this.cstate.sendMTFValues_len;
            int alphaSize = this.nInUse + 2;

            for (int t = BZip2.NGroups; --t >= 0;)
            {
                byte[] len_t = len[t];
                for (int v = alphaSize; --v >= 0;)
                {
                    len_t[v] = GREATER_ICOST;
                }
            }

            /* Decide how many coding tables to use */
            // assert (this.nMTF > 0) : this.nMTF;
            int nGroups = (this.nMTF < 200) ? 2 : (this.nMTF < 600) ? 3
                : (this.nMTF < 1200) ? 4 : (this.nMTF < 2400) ? 5 : 6;

            /* Generate an initial set of coding tables */
            sendMTFValues0(nGroups, alphaSize);

            /*
             * Iterate up to N_ITERS times to improve the tables.
             */
            int nSelectors = sendMTFValues1(nGroups, alphaSize);

            /* Compute MTF values for the selectors. */
            sendMTFValues2(nGroups, nSelectors);

            /* Assign actual codes for the tables. */
            sendMTFValues3(nGroups, alphaSize);

            /* Transmit the mapping table. */
            sendMTFValues4();

            /* Now the selectors. */
            sendMTFValues5(nGroups, nSelectors);

            /* Now the coding tables. */
            sendMTFValues6(nGroups, alphaSize);

            /* And finally, the block data proper */
            sendMTFValues7(nSelectors);
        }

        private void sendMTFValues0(int nGroups, int alphaSize)
        {
            byte[][] len = this.cstate.sendMTFValues_len;
            int[] mtfFreq = this.cstate.mtfFreq;

            int remF = this.nMTF;
            int gs = 0;

            for (int nPart = nGroups; nPart > 0; nPart--)
            {
                int tFreq = remF / nPart;
                int ge = gs - 1;
                int aFreq = 0;

                for (int a = alphaSize - 1; (aFreq < tFreq) && (ge < a);)
                {
                    aFreq += mtfFreq[++ge];
                }

                if ((ge > gs) && (nPart != nGroups) && (nPart != 1)
                    && (((nGroups - nPart) & 1) != 0))
                {
                    aFreq -= mtfFreq[ge--];
                }

                byte[] len_np = len[nPart - 1];
                for (int v = alphaSize; --v >= 0;)
                {
                    if ((v >= gs) && (v <= ge))
                    {
                        len_np[v] = LESSER_ICOST;
                    }
                    else {
                        len_np[v] = GREATER_ICOST;
                    }
                }

                gs = ge + 1;
                remF -= aFreq;
            }
        }


        private static void hbMakeCodeLengths(byte[] len,  int[] freq,
                                              CompressionState state1, int alphaSize,
                                              int maxLen)
        {
            /*
             * Nodes and heap entries run from 1. Entry 0 for both the heap and
             * nodes is a sentinel.
             */
            int[] heap = state1.heap;
            int[] weight = state1.weight;
            int[] parent = state1.parent;

            for (int i = alphaSize; --i >= 0;)
            {
                weight[i + 1] = (freq[i] == 0 ? 1 : freq[i]) << 8;
            }

            for (bool tooLong = true; tooLong;)
            {
                tooLong = false;

                int nNodes = alphaSize;
                int nHeap = 0;
                heap[0] = 0;
                weight[0] = 0;
                parent[0] = -2;

                for (int i = 1; i <= alphaSize; i++)
                {
                    parent[i] = -1;
                    nHeap++;
                    heap[nHeap] = i;

                    int zz = nHeap;
                    int tmp = heap[zz];
                    while (weight[tmp] < weight[heap[zz >> 1]])
                    {
                        heap[zz] = heap[zz >> 1];
                        zz >>= 1;
                    }
                    heap[zz] = tmp;
                }

                while (nHeap > 1)
                {
                    int n1 = heap[1];
                    heap[1] = heap[nHeap];
                    nHeap--;

                    int yy = 0;
                    int zz = 1;
                    int tmp = heap[1];

                    while (true)
                    {
                        yy = zz << 1;

                        if (yy > nHeap)
                        {
                            break;
                        }

                        if ((yy < nHeap)
                            && (weight[heap[yy + 1]] < weight[heap[yy]]))
                        {
                            yy++;
                        }

                        if (weight[tmp] < weight[heap[yy]])
                        {
                            break;
                        }

                        heap[zz] = heap[yy];
                        zz = yy;
                    }

                    heap[zz] = tmp;

                    int n2 = heap[1];
                    heap[1] = heap[nHeap];
                    nHeap--;

                    yy = 0;
                    zz = 1;
                    tmp = heap[1];

                    while (true)
                    {
                        yy = zz << 1;

                        if (yy > nHeap)
                        {
                            break;
                        }

                        if ((yy < nHeap)
                            && (weight[heap[yy + 1]] < weight[heap[yy]]))
                        {
                            yy++;
                        }

                        if (weight[tmp] < weight[heap[yy]])
                        {
                            break;
                        }

                        heap[zz] = heap[yy];
                        zz = yy;
                    }

                    heap[zz] = tmp;
                    nNodes++;
                    parent[n1] = parent[n2] = nNodes;

                    int weight_n1 = weight[n1];
                    int weight_n2 = weight[n2];
                    weight[nNodes] = (int) (((uint)weight_n1 & 0xffffff00U)
                                            + ((uint)weight_n2 & 0xffffff00U))
                        | (1 + (((weight_n1 & 0x000000ff)
                                 > (weight_n2 & 0x000000ff))
                                ? (weight_n1 & 0x000000ff)
                                : (weight_n2 & 0x000000ff)));

                    parent[nNodes] = -1;
                    nHeap++;
                    heap[nHeap] = nNodes;

                    tmp = 0;
                    zz = nHeap;
                    tmp = heap[zz];
                    int weight_tmp = weight[tmp];
                    while (weight_tmp < weight[heap[zz >> 1]])
                    {
                        heap[zz] = heap[zz >> 1];
                        zz >>= 1;
                    }
                    heap[zz] = tmp;

                }

                for (int i = 1; i <= alphaSize; i++)
                {
                    int j = 0;
                    int k = i;

                    for (int parent_k; (parent_k = parent[k]) >= 0;)
                    {
                        k = parent_k;
                        j++;
                    }

                    len[i - 1] = (byte) j;
                    if (j > maxLen)
                    {
                        tooLong = true;
                    }
                }

                if (tooLong)
                {
                    for (int i = 1; i < alphaSize; i++)
                    {
                        int j = weight[i] >> 8;
                        j = 1 + (j >> 1);
                        weight[i] = j << 8;
                    }
                }
            }
        }


        private int sendMTFValues1(int nGroups, int alphaSize)
        {
            CompressionState dataShadow = this.cstate;
            int[][] rfreq = dataShadow.sendMTFValues_rfreq;
            int[] fave = dataShadow.sendMTFValues_fave;
            short[] cost = dataShadow.sendMTFValues_cost;
            char[] sfmap = dataShadow.sfmap;
            byte[] selector = dataShadow.selector;
            byte[][] len = dataShadow.sendMTFValues_len;
            byte[] len_0 = len[0];
            byte[] len_1 = len[1];
            byte[] len_2 = len[2];
            byte[] len_3 = len[3];
            byte[] len_4 = len[4];
            byte[] len_5 = len[5];
            int nMTFShadow = this.nMTF;

            int nSelectors = 0;

            for (int iter = 0; iter < BZip2.N_ITERS; iter++)
            {
                for (int t = nGroups; --t >= 0;)
                {
                    fave[t] = 0;
                    int[] rfreqt = rfreq[t];
                    for (int i = alphaSize; --i >= 0;)
                    {
                        rfreqt[i] = 0;
                    }
                }

                nSelectors = 0;

                for (int gs = 0; gs < this.nMTF;)
                {
                    /* Set group start & end marks. */

                    /*
                     * Calculate the cost of this group as coded by each of the
                     * coding tables.
                     */

                    int ge = Math.Min(gs + BZip2.G_SIZE - 1, nMTFShadow - 1);

                    if (nGroups == BZip2.NGroups)
                    {
                        // unrolled version of the else-block

                        int[] c = new int[6];

                        for (int i = gs; i <= ge; i++)
                        {
                            int icv = sfmap[i];
                            c[0] += len_0[icv] & 0xff;
                            c[1] += len_1[icv] & 0xff;
                            c[2] += len_2[icv] & 0xff;
                            c[3] += len_3[icv] & 0xff;
                            c[4] += len_4[icv] & 0xff;
                            c[5] += len_5[icv] & 0xff;
                        }

                        cost[0] = (short) c[0];
                        cost[1] = (short) c[1];
                        cost[2] = (short) c[2];
                        cost[3] = (short) c[3];
                        cost[4] = (short) c[4];
                        cost[5] = (short) c[5];
                    }
                    else
                    {
                        for (int t = nGroups; --t >= 0;)
                        {
                            cost[t] = 0;
                        }

                        for (int i = gs; i <= ge; i++)
                        {
                            int icv = sfmap[i];
                            for (int t = nGroups; --t >= 0;)
                            {
                                cost[t] += (short) (len[t][icv] & 0xff);
                            }
                        }
                    }

                    /*
                     * Find the coding table which is best for this group, and
                     * record its identity in the selector table.
                     */
                    int bt = -1;
                    for (int t = nGroups, bc = 999999999; --t >= 0;)
                    {
                        int cost_t = cost[t];
                        if (cost_t < bc)
                        {
                            bc = cost_t;
                            bt = t;
                        }
                    }

                    fave[bt]++;
                    selector[nSelectors] = (byte) bt;
                    nSelectors++;

                    /*
                     * Increment the symbol frequencies for the selected table.
                     */
                    int[] rfreq_bt = rfreq[bt];
                    for (int i = gs; i <= ge; i++)
                    {
                        rfreq_bt[sfmap[i]]++;
                    }

                    gs = ge + 1;
                }

                /*
                 * Recompute the tables based on the accumulated frequencies.
                 */
                for (int t = 0; t < nGroups; t++)
                {
                    hbMakeCodeLengths(len[t], rfreq[t], this.cstate, alphaSize, 20);
                }
            }

            return nSelectors;
        }

        private void sendMTFValues2(int nGroups, int nSelectors)
        {
            // assert (nGroups < 8) : nGroups;

            CompressionState dataShadow = this.cstate;
            byte[] pos = dataShadow.sendMTFValues2_pos;

            for (int i = nGroups; --i >= 0;)
            {
                pos[i] = (byte) i;
            }

            for (int i = 0; i < nSelectors; i++)
            {
                byte ll_i = dataShadow.selector[i];
                byte tmp = pos[0];
                int j = 0;

                while (ll_i != tmp)
                {
                    j++;
                    byte tmp2 = tmp;
                    tmp = pos[j];
                    pos[j] = tmp2;
                }

                pos[0] = tmp;
                dataShadow.selectorMtf[i] = (byte) j;
            }
        }

        private void sendMTFValues3(int nGroups, int alphaSize)
        {
            int[][] code = this.cstate.sendMTFValues_code;
            byte[][] len = this.cstate.sendMTFValues_len;

            for (int t = 0; t < nGroups; t++)
            {
                int minLen = 32;
                int maxLen = 0;
                byte[] len_t = len[t];
                for (int i = alphaSize; --i >= 0;)
                {
                    int l = len_t[i] & 0xff;
                    if (l > maxLen)
                    {
                        maxLen = l;
                    }
                    if (l < minLen)
                    {
                        minLen = l;
                    }
                }

                // assert (maxLen <= 20) : maxLen;
                // assert (minLen >= 1) : minLen;

                hbAssignCodes(code[t], len[t], minLen, maxLen, alphaSize);
            }
        }

        private void sendMTFValues4()
        {
            bool[] inUse = this.cstate.inUse;
            bool[] inUse16 = this.cstate.sentMTFValues4_inUse16;

            for (int i = 16; --i >= 0;)
            {
                inUse16[i] = false;
                int i16 = i * 16;
                for (int j = 16; --j >= 0;)
                {
                    if (inUse[i16 + j])
                    {
                        inUse16[i] = true;
                    }
                }
            }

            for (int i = 0; i < 16; i++)
            {
                bsW(1, inUse16[i] ? 1 : 0);
            }

            Stream outShadow = this.output;
            int bsLiveShadow = this.bsLive;
            int bsBuffShadow = this.bsBuff;

            for (int i = 0; i < 16; i++)
            {
                if (inUse16[i])
                {
                    int i16 = i * 16;
                    for (int j = 0; j < 16; j++)
                    {
                        // inlined: bsW(1, inUse[i16 + j] ? 1 : 0);
                        while (bsLiveShadow >= 8)
                        {
                            byte b = (byte) (bsBuffShadow >> 24);
                            outShadow.WriteByte(b);
                            bsBuffShadow <<= 8;
                            bsLiveShadow -= 8;
                        }
                        if (inUse[i16 + j])
                        {
                            bsBuffShadow |= 1 << (32 - bsLiveShadow - 1);
                        }
                        bsLiveShadow++;
                    }
                }
            }

            this.bsBuff = bsBuffShadow;
            this.bsLive = bsLiveShadow;
        }

        private void sendMTFValues5(int nGroups, int nSelectors)
        {
            bsW(3, nGroups);
            bsW(15, nSelectors);

            Stream outShadow = this.output;
            byte[] selectorMtf = this.cstate.selectorMtf;

            int bsLiveShadow = this.bsLive;
            int bsBuffShadow = this.bsBuff;

            for (int i = 0; i < nSelectors; i++)
            {
                for (int j = 0, hj = selectorMtf[i] & 0xff; j < hj; j++)
                {
                    // inlined: bsW(1, 1);
                    while (bsLiveShadow >= 8)
                    {
                        byte b = (byte) (bsBuffShadow >> 24);
                        outShadow.WriteByte(b);
                        bsBuffShadow <<= 8;
                        bsLiveShadow -= 8;
                    }
                    bsBuffShadow |= 1 << (32 - bsLiveShadow - 1);
                    bsLiveShadow++;
                }

                // inlined: bsW(1, 0);
                while (bsLiveShadow >= 8)
                {
                    byte b = (byte) (bsBuffShadow >> 24);
                    outShadow.WriteByte(b);
                    bsBuffShadow <<= 8;
                    bsLiveShadow -= 8;
                }
                // bsBuffShadow |= 0 << (32 - bsLiveShadow - 1);
                bsLiveShadow++;
            }

            this.bsBuff = bsBuffShadow;
            this.bsLive = bsLiveShadow;
        }

        private void sendMTFValues6(int nGroups, int alphaSize)

        {
            byte[][] len = this.cstate.sendMTFValues_len;
            Stream outShadow = this.output;

            int bsLiveShadow = this.bsLive;
            int bsBuffShadow = this.bsBuff;

            for (int t = 0; t < nGroups; t++)
            {
                byte[] len_t = len[t];
                int curr = len_t[0] & 0xff;

                // inlined: bsW(5, curr);
                while (bsLiveShadow >= 8)
                {
                    byte b = (byte) (bsBuffShadow >> 24);
                    outShadow.WriteByte(b);
                    bsBuffShadow <<= 8;
                    bsLiveShadow -= 8;
                }
                bsBuffShadow |= curr << (32 - bsLiveShadow - 5);
                bsLiveShadow += 5;

                for (int i = 0; i < alphaSize; i++)
                {
                    int lti = len_t[i] & 0xff;
                    while (curr < lti)
                    {
                        // inlined: bsW(2, 2);
                        while (bsLiveShadow >= 8)
                        {
                            byte b = (byte) (bsBuffShadow >> 24);
                            outShadow.WriteByte(b);

                            bsBuffShadow <<= 8;
                            bsLiveShadow -= 8;
                        }
                        bsBuffShadow |= 2 << (32 - bsLiveShadow - 2);
                        bsLiveShadow += 2;

                        curr++; /* 10 */
                    }

                    while (curr > lti)
                    {
                        // inlined: bsW(2, 3);
                        while (bsLiveShadow >= 8)
                        {
                            byte b = (byte) (bsBuffShadow >> 24);
                            outShadow.WriteByte(b);
                            bsBuffShadow <<= 8;
                            bsLiveShadow -= 8;
                        }
                        bsBuffShadow |= 3 << (32 - bsLiveShadow - 2);
                        bsLiveShadow += 2;

                        curr--; /* 11 */
                    }

                    // inlined: bsW(1, 0);
                    while (bsLiveShadow >= 8)
                    {
                        byte b = (byte) (bsBuffShadow >> 24);
                        outShadow.WriteByte(b);
                        bsBuffShadow <<= 8;
                        bsLiveShadow -= 8;
                    }
                    // bsBuffShadow |= 0 << (32 - bsLiveShadow - 1);
                    bsLiveShadow++;
                }
            }

            this.bsBuff = bsBuffShadow;
            this.bsLive = bsLiveShadow;
        }

        private void sendMTFValues7(int nSelectors)
        {
            CompressionState dataShadow = this.cstate;
            byte[][] len = dataShadow.sendMTFValues_len;
            int[][] code = dataShadow.sendMTFValues_code;
            Stream outShadow = this.output;
            byte[] selector = dataShadow.selector;
            char[] sfmap = dataShadow.sfmap;
            int nMTFShadow = this.nMTF;

            int selCtr = 0;

            int bsLiveShadow = this.bsLive;
            int bsBuffShadow = this.bsBuff;

            for (int gs = 0; gs < nMTFShadow;)
            {
                int ge = Math.Min(gs + BZip2.G_SIZE - 1, nMTFShadow - 1);
                int selector_selCtr = selector[selCtr] & 0xff;
                int[] code_selCtr = code[selector_selCtr];
                byte[] len_selCtr = len[selector_selCtr];

                while (gs <= ge)
                {
                    int sfmap_i = sfmap[gs];

                    //
                    // inlined: bsW(len_selCtr[sfmap_i] & 0xff,
                    // code_selCtr[sfmap_i]);
                    //
                    while (bsLiveShadow >= 8)
                    {
                        byte b = (byte) (bsBuffShadow >> 24);
                        outShadow.WriteByte(b);
                        bsBuffShadow <<= 8;
                        bsLiveShadow -= 8;
                    }
                    int n = len_selCtr[sfmap_i] & 0xFF;
                    bsBuffShadow |= code_selCtr[sfmap_i] << (32 - bsLiveShadow - n);
                    bsLiveShadow += n;

                    gs++;
                }

                gs = ge + 1;
                selCtr++;
            }

            this.bsBuff = bsBuffShadow;
            this.bsLive = bsLiveShadow;
        }

        private void moveToFrontCodeAndSend()
        {
            bsW(24, this.origPtr);
            generateMTFValues();
            sendMTFValues();
        }

        /**
         * This is the most hammered method of this class.
         *
         * <p>
         * This is the version using unrolled loops.
         * </p>
         */
        private bool mainSimpleSort(CompressionState dataShadow, int lo,
                                    int hi, int d)
        {
            int bigN = hi - lo + 1;
            if (bigN < 2)
            {
                return this.firstAttempt && (this.workDone > this.workLimit);
            }

            int hp = 0;
            while (increments[hp] < bigN)
                hp++;

            int[] fmap = dataShadow.fmap;
            char[] quadrant = dataShadow.quadrant;
            byte[] block = dataShadow.block;
            int lastShadow = this.last;
            int lastPlus1 = lastShadow + 1;
            bool firstAttemptShadow = this.firstAttempt;
            int workLimitShadow = this.workLimit;
            int workDoneShadow = this.workDone;

            // Following block contains unrolled code which could be shortened by
            // coding it in additional loops.

            // HP:
            while (--hp >= 0)
            {
                int h = increments[hp];
                int mj = lo + h - 1;

                for (int i = lo + h; i <= hi;)
                {
                    // copy
                    for (int k = 3; (i <= hi) && (--k >= 0); i++)
                    {
                        int v = fmap[i];
                        int vd = v + d;
                        int j = i;

                        // for (int a;
                        // (j > mj) && mainGtU((a = fmap[j - h]) + d, vd,
                        // block, quadrant, lastShadow);
                        // j -= h) {
                        // fmap[j] = a;
                        // }
                        //
                        // unrolled version:

                        // start inline mainGTU
                        bool onceRunned = false;
                        int a = 0;

                        HAMMER: while (true)
                        {
                            if (onceRunned)
                            {
                                fmap[j] = a;
                                if ((j -= h) <= mj)
                                {
                                    goto END_HAMMER;
                                }
                            }
                            else {
                                onceRunned = true;
                            }

                            a = fmap[j - h];
                            int i1 = a + d;
                            int i2 = vd;

                            // following could be done in a loop, but
                            // unrolled it for performance:
                            if (block[i1 + 1] == block[i2 + 1])
                            {
                                if (block[i1 + 2] == block[i2 + 2])
                                {
                                    if (block[i1 + 3] == block[i2 + 3])
                                    {
                                        if (block[i1 + 4] == block[i2 + 4])
                                        {
                                            if (block[i1 + 5] == block[i2 + 5])
                                            {
                                                if (block[(i1 += 6)] == block[(i2 += 6)])
                                                {
                                                    int x = lastShadow;
                                                    X: while (x > 0)
                                                    {
                                                        x -= 4;

                                                        if (block[i1 + 1] == block[i2 + 1])
                                                        {
                                                            if (quadrant[i1] == quadrant[i2])
                                                            {
                                                                if (block[i1 + 2] == block[i2 + 2])
                                                                {
                                                                    if (quadrant[i1 + 1] == quadrant[i2 + 1])
                                                                    {
                                                                        if (block[i1 + 3] == block[i2 + 3])
                                                                        {
                                                                            if (quadrant[i1 + 2] == quadrant[i2 + 2])
                                                                            {
                                                                                if (block[i1 + 4] == block[i2 + 4])
                                                                                {
                                                                                    if (quadrant[i1 + 3] == quadrant[i2 + 3])
                                                                                    {
                                                                                        if ((i1 += 4) >= lastPlus1)
                                                                                        {
                                                                                            i1 -= lastPlus1;
                                                                                        }
                                                                                        if ((i2 += 4) >= lastPlus1)
                                                                                        {
                                                                                            i2 -= lastPlus1;
                                                                                        }
                                                                                        workDoneShadow++;
                                                                                        goto X;
                                                                                    }
                                                                                    else if ((quadrant[i1 + 3] > quadrant[i2 + 3]))
                                                                                    {
                                                                                        goto HAMMER;
                                                                                    }
                                                                                    else {
                                                                                        goto END_HAMMER;
                                                                                    }
                                                                                }
                                                                                else if ((block[i1 + 4] & 0xff) > (block[i2 + 4] & 0xff))
                                                                                {
                                                                                    goto HAMMER;
                                                                                }
                                                                                else {
                                                                                    goto END_HAMMER;
                                                                                }
                                                                            }
                                                                            else if ((quadrant[i1 + 2] > quadrant[i2 + 2]))
                                                                            {
                                                                                goto HAMMER;
                                                                            }
                                                                            else {
                                                                                goto END_HAMMER;
                                                                            }
                                                                        }
                                                                        else if ((block[i1 + 3] & 0xff) > (block[i2 + 3] & 0xff))
                                                                        {
                                                                            goto HAMMER;
                                                                        }
                                                                        else {
                                                                            goto END_HAMMER;
                                                                        }
                                                                    }
                                                                    else if ((quadrant[i1 + 1] > quadrant[i2 + 1]))
                                                                    {
                                                                        goto HAMMER;
                                                                    }
                                                                    else {
                                                                        goto END_HAMMER;
                                                                    }
                                                                }
                                                                else if ((block[i1 + 2] & 0xff) > (block[i2 + 2] & 0xff))
                                                                {
                                                                    goto HAMMER;
                                                                }
                                                                else {
                                                                    goto END_HAMMER;
                                                                }
                                                            }
                                                            else if ((quadrant[i1] > quadrant[i2]))
                                                            {
                                                                goto HAMMER;
                                                            }
                                                            else {
                                                                goto END_HAMMER;
                                                            }
                                                        }
                                                        else if ((block[i1 + 1] & 0xff) > (block[i2 + 1] & 0xff))
                                                        {
                                                            goto HAMMER;
                                                        }
                                                        else {
                                                            goto END_HAMMER;
                                                        }

                                                    }
                                                    goto END_HAMMER;
                                                } // while x > 0
                                                else {
                                                    if ((block[i1] & 0xff) > (block[i2] & 0xff))
                                                    {
                                                        goto HAMMER;
                                                    }
                                                    else {
                                                        goto END_HAMMER;
                                                    }
                                                }
                                            }
                                            else if ((block[i1 + 5] & 0xff) > (block[i2 + 5] & 0xff))
                                            {
                                                goto HAMMER;
                                            }
                                            else {
                                                goto END_HAMMER;
                                            }
                                        }
                                        else if ((block[i1 + 4] & 0xff) > (block[i2 + 4] & 0xff))
                                        {
                                            goto HAMMER;
                                        }
                                        else {
                                            goto END_HAMMER;
                                        }
                                    }
                                    else if ((block[i1 + 3] & 0xff) > (block[i2 + 3] & 0xff))
                                    {
                                        goto HAMMER;
                                    }
                                    else {
                                        goto END_HAMMER;
                                    }
                                }
                                else if ((block[i1 + 2] & 0xff) > (block[i2 + 2] & 0xff))
                                {
                                    goto HAMMER;
                                }
                                else {
                                    goto END_HAMMER;
                                }
                            }
                            else if ((block[i1 + 1] & 0xff) > (block[i2 + 1] & 0xff))
                            {
                                goto HAMMER;
                            }
                            else {
                                goto END_HAMMER;
                            }

                        } // HAMMER

                        END_HAMMER:
                        // end inline mainGTU

                        fmap[j] = v;
                    }

                    if (firstAttemptShadow && (i <= hi)
                        && (workDoneShadow > workLimitShadow))
                    {
                        goto END_HP;
                    }
                }
            }
            END_HP:

            this.workDone = workDoneShadow;
            return firstAttemptShadow && (workDoneShadow > workLimitShadow);
        }



        private static void vswap(int[] fmap, int p1, int p2, int n)
        {
            n += p1;
            while (p1 < n)
            {
                int t = fmap[p1];
                fmap[p1++] = fmap[p2];
                fmap[p2++] = t;
            }
        }

        private static byte med3(byte a, byte b, byte c)
        {
            return (a < b) ? (b < c ? b : a < c ? c : a) : (b > c ? b : a > c ? c
                                                            : a);
        }

        private void blockSort()
        {
            this.workLimit = WORK_FACTOR * this.last;
            this.workDone = 0;
            this.blockRandomised = false;
            this.firstAttempt = true;
            mainSort();

            if (this.firstAttempt && (this.workDone > this.workLimit))
            {
                randomiseBlock();
                this.workLimit = this.workDone = 0;
                this.firstAttempt = false;
                mainSort();
            }

            int[] fmap = this.cstate.fmap;
            this.origPtr = -1;
            for (int i = 0, lastShadow = this.last; i <= lastShadow; i++)
            {
                if (fmap[i] == 0)
                {
                    this.origPtr = i;
                    break;
                }
            }

            // assert (this.origPtr != -1) : this.origPtr;
        }

        /**
         * Method "mainQSort3", file "blocksort.c", BZip2 1.0.2
         */
        private void mainQSort3(CompressionState dataShadow, int loSt,
                                int hiSt, int dSt)
        {
            int[] stack_ll = dataShadow.stack_ll;
            int[] stack_hh = dataShadow.stack_hh;
            int[] stack_dd = dataShadow.stack_dd;
            int[] fmap = dataShadow.fmap;
            byte[] block = dataShadow.block;

            stack_ll[0] = loSt;
            stack_hh[0] = hiSt;
            stack_dd[0] = dSt;

            for (int sp = 1; --sp >= 0;)
            {
                int lo = stack_ll[sp];
                int hi = stack_hh[sp];
                int d = stack_dd[sp];

                if ((hi - lo < SMALL_THRESH) || (d > DEPTH_THRESH))
                {
                    if (mainSimpleSort(dataShadow, lo, hi, d))
                    {
                        return;
                    }
                }
                else {
                    int d1 = d + 1;
                    int med = med3(block[fmap[lo] + d1],
                                   block[fmap[hi] + d1], block[fmap[(lo + hi) >> 1] + d1]) & 0xff;

                    int unLo = lo;
                    int unHi = hi;
                    int ltLo = lo;
                    int gtHi = hi;

                    while (true)
                    {
                        while (unLo <= unHi)
                        {
                            int n = (block[fmap[unLo] + d1] & 0xff)
                                - med;
                            if (n == 0)
                            {
                                int temp = fmap[unLo];
                                fmap[unLo++] = fmap[ltLo];
                                fmap[ltLo++] = temp;
                            }
                            else if (n < 0)
                            {
                                unLo++;
                            }
                            else {
                                break;
                            }
                        }

                        while (unLo <= unHi)
                        {
                            int n = (block[fmap[unHi] + d1] & 0xff)
                                - med;
                            if (n == 0)
                            {
                                int temp = fmap[unHi];
                                fmap[unHi--] = fmap[gtHi];
                                fmap[gtHi--] = temp;
                            }
                            else if (n > 0)
                            {
                                unHi--;
                            }
                            else {
                                break;
                            }
                        }

                        if (unLo <= unHi)
                        {
                            int temp = fmap[unLo];
                            fmap[unLo++] = fmap[unHi];
                            fmap[unHi--] = temp;
                        }
                        else {
                            break;
                        }
                    }

                    if (gtHi < ltLo)
                    {
                        stack_ll[sp] = lo;
                        stack_hh[sp] = hi;
                        stack_dd[sp] = d1;
                        sp++;
                    }
                    else {
                        int n = ((ltLo - lo) < (unLo - ltLo)) ? (ltLo - lo)
                            : (unLo - ltLo);
                        vswap(fmap, lo, unLo - n, n);
                        int m = ((hi - gtHi) < (gtHi - unHi)) ? (hi - gtHi)
                            : (gtHi - unHi);
                        vswap(fmap, unLo, hi - m + 1, m);

                        n = lo + unLo - ltLo - 1;
                        m = hi - (gtHi - unHi) + 1;

                        stack_ll[sp] = lo;
                        stack_hh[sp] = n;
                        stack_dd[sp] = d;
                        sp++;

                        stack_ll[sp] = n + 1;
                        stack_hh[sp] = m - 1;
                        stack_dd[sp] = d1;
                        sp++;

                        stack_ll[sp] = m;
                        stack_hh[sp] = hi;
                        stack_dd[sp] = d;
                        sp++;
                    }
                }
            }
        }

        private void mainSort()
        {
            CompressionState dataShadow = this.cstate;
            int[] runningOrder = dataShadow.mainSort_runningOrder;
            int[] copy = dataShadow.mainSort_copy;
            bool[] bigDone = dataShadow.mainSort_bigDone;
            int[] ftab = dataShadow.ftab;
            byte[] block = dataShadow.block;
            int[] fmap = dataShadow.fmap;
            char[] quadrant = dataShadow.quadrant;
            int lastShadow = this.last;
            int workLimitShadow = this.workLimit;
            bool firstAttemptShadow = this.firstAttempt;

            // Set up the 2-byte frequency table
            for (int i = 65537; --i >= 0;)
            {
                ftab[i] = 0;
            }

            /*
             * In the various block-sized structures, live data runs from 0 to
             * last+NUM_OVERSHOOT_BYTES inclusive. First, set up the overshoot area
             * for block.
             */
            for (int i = 0; i < BZip2.NUM_OVERSHOOT_BYTES; i++)
            {
                block[lastShadow + i + 2] = block[(i % (lastShadow + 1)) + 1];
            }
            for (int i = lastShadow + BZip2.NUM_OVERSHOOT_BYTES +1; --i >= 0;)
            {
                quadrant[i] = '\0';
            }
            block[0] = block[lastShadow + 1];

            // Complete the initial radix sort:

            int c1 = block[0] & 0xff;
            for (int i = 0; i <= lastShadow; i++)
            {
                int c2 = block[i + 1] & 0xff;
                ftab[(c1 << 8) + c2]++;
                c1 = c2;
            }

            for (int i = 1; i <= 65536; i++)
                ftab[i] += ftab[i - 1];

            c1 = block[1] & 0xff;
            for (int i = 0; i < lastShadow; i++)
            {
                int c2 = block[i + 2] & 0xff;
                fmap[--ftab[(c1 << 8) + c2]] = i;
                c1 = c2;
            }

            fmap[--ftab[((block[lastShadow + 1] & 0xff) << 8) + (block[1] & 0xff)]] = lastShadow;

            /*
             * Now ftab contains the first loc of every small bucket. Calculate the
             * running order, from smallest to largest big bucket.
             */
            for (int i = 256; --i >= 0;)
            {
                bigDone[i] = false;
                runningOrder[i] = i;
            }

            for (int h = 364; h != 1;)
            {
                h /= 3;
                for (int i = h; i <= 255; i++)
                {
                    int vv = runningOrder[i];
                    int a = ftab[(vv + 1) << 8] - ftab[vv << 8];
                    int b = h - 1;
                    int j = i;
                    for (int ro = runningOrder[j - h]; (ftab[(ro + 1) << 8] - ftab[ro << 8]) > a; ro = runningOrder[j
                                                                                                                    - h])
                    {
                        runningOrder[j] = ro;
                        j -= h;
                        if (j <= b)
                        {
                            break;
                        }
                    }
                    runningOrder[j] = vv;
                }
            }

            /*
             * The main sorting loop.
             */
            for (int i = 0; i <= 255; i++)
            {
                /*
                 * Process big buckets, starting with the least full.
                 */
                int ss = runningOrder[i];

                // Step 1:
                /*
                 * Complete the big bucket [ss] by quicksorting any unsorted small
                 * buckets [ss, j]. Hopefully previous pointer-scanning phases have
                 * already completed many of the small buckets [ss, j], so we don't
                 * have to sort them at all.
                 */
                for (int j = 0; j <= 255; j++)
                {
                    int sb = (ss << 8) + j;
                    int ftab_sb = ftab[sb];
                    if ((ftab_sb & SETMASK) != SETMASK)
                    {
                        int lo = ftab_sb & CLEARMASK;
                        int hi = (ftab[sb + 1] & CLEARMASK) - 1;
                        if (hi > lo)
                        {
                            mainQSort3(dataShadow, lo, hi, 2);
                            if (firstAttemptShadow
                                && (this.workDone > workLimitShadow))
                            {
                                return;
                            }
                        }
                        ftab[sb] = ftab_sb | SETMASK;
                    }
                }

                // Step 2:
                // Now scan this big bucket so as to synthesise the
                // sorted order for small buckets [t, ss] for all t != ss.

                for (int j = 0; j <= 255; j++)
                {
                    copy[j] = ftab[(j << 8) + ss] & CLEARMASK;
                }

                for (int j = ftab[ss << 8] & CLEARMASK, hj = (ftab[(ss + 1) << 8] & CLEARMASK); j < hj; j++)
                {
                    int fmap_j = fmap[j];
                    c1 = block[fmap_j] & 0xff;
                    if (!bigDone[c1])
                    {
                        fmap[copy[c1]] = (fmap_j == 0) ? lastShadow : (fmap_j - 1);
                        copy[c1]++;
                    }
                }

                for (int j = 256; --j >= 0;)
                    ftab[(j << 8) + ss] |= SETMASK;

                // Step 3:
                /*
                 * The ss big bucket is now done. Record this fact, and update the
                 * quadrant descriptors. Remember to update quadrants in the
                 * overshoot area too, if necessary. The "if (i < 255)" test merely
                 * skips this updating for the last bucket processed, since updating
                 * for the last bucket is pointless.
                 */
                bigDone[ss] = true;

                if (i < 255)
                {
                    int bbStart = ftab[ss << 8] & CLEARMASK;
                    int bbSize = (ftab[(ss + 1) << 8] & CLEARMASK) - bbStart;
                    int shifts = 0;

                    while ((bbSize >> shifts) > 65534)
                    {
                        shifts++;
                    }

                    for (int j = 0; j < bbSize; j++)
                    {
                        int a2update = fmap[bbStart + j];
                        char qVal = (char) (j >> shifts);
                        quadrant[a2update] = qVal;
                        if (a2update < BZip2.NUM_OVERSHOOT_BYTES)
                        {
                            quadrant[a2update + lastShadow + 1] = qVal;
                        }
                    }
                }

            }
        }

        private void randomiseBlock()
        {
            bool[] inUse = this.cstate.inUse;
            byte[] block = this.cstate.block;
            int lastShadow = this.last;

            for (int i = 256; --i >= 0;)
                inUse[i] = false;

            int rNToGo = 0;
            int rTPos = 0;
            for (int i = 0, j = 1; i <= lastShadow; i = j, j++)
            {
                if (rNToGo == 0)
                {
                    rNToGo = (char) Rand.Rnums(rTPos);
                    if (++rTPos == 512)
                    {
                        rTPos = 0;
                    }
                }

                rNToGo--;
                block[j] ^= (byte) ((rNToGo == 1) ? 1 : 0);

                // handle 16 bit signed numbers
                inUse[block[j] & 0xff] = true;
            }

            this.blockRandomised = true;
        }

        private void generateMTFValues()
        {
            int lastShadow = this.last;
            CompressionState dataShadow = this.cstate;
            bool[] inUse = dataShadow.inUse;
            byte[] block = dataShadow.block;
            int[] fmap = dataShadow.fmap;
            char[] sfmap = dataShadow.sfmap;
            int[] mtfFreq = dataShadow.mtfFreq;
            byte[] unseqToSeq = dataShadow.unseqToSeq;
            byte[] yy = dataShadow.generateMTFValues_yy;

            // make maps
            int nInUseShadow = 0;
            for (int i = 0; i < 256; i++)
            {
                if (inUse[i])
                {
                    unseqToSeq[i] = (byte) nInUseShadow;
                    nInUseShadow++;
                }
            }
            this.nInUse = nInUseShadow;

            int eob = nInUseShadow + 1;

            for (int i = eob; i >= 0; i--)
            {
                mtfFreq[i] = 0;
            }

            for (int i = nInUseShadow; --i >= 0;)
            {
                yy[i] = (byte) i;
            }

            int wr = 0;
            int zPend = 0;

            for (int i = 0; i <= lastShadow; i++)
            {
                byte ll_i = unseqToSeq[block[fmap[i]] & 0xff];
                byte tmp = yy[0];
                int j = 0;

                while (ll_i != tmp)
                {
                    j++;
                    byte tmp2 = tmp;
                    tmp = yy[j];
                    yy[j] = tmp2;
                }
                yy[0] = tmp;

                if (j == 0)
                {
                    zPend++;
                }
                else
                {
                    if (zPend > 0)
                    {
                        zPend--;
                        while (true)
                        {
                            if ((zPend & 1) == 0)
                            {
                                sfmap[wr] = BZip2.RUNA;
                                wr++;
                                mtfFreq[BZip2.RUNA]++;
                            }
                            else
                            {
                                sfmap[wr] = BZip2.RUNB;
                                wr++;
                                mtfFreq[BZip2.RUNB]++;
                            }

                            if (zPend >= 2)
                            {
                                zPend = (zPend - 2) >> 1;
                            }
                            else
                            {
                                break;
                            }
                        }
                        zPend = 0;
                    }
                    sfmap[wr] = (char) (j + 1);
                    wr++;
                    mtfFreq[j + 1]++;
                }
            }

            if (zPend > 0)
            {
                zPend--;
                while (true)
                {
                    if ((zPend & 1) == 0)
                    {
                        sfmap[wr] = BZip2.RUNA;
                        wr++;
                        mtfFreq[BZip2.RUNA]++;
                    }
                    else
                    {
                        sfmap[wr] = BZip2.RUNB;
                        wr++;
                        mtfFreq[BZip2.RUNB]++;
                    }

                    if (zPend >= 2)
                    {
                        zPend = (zPend - 2) >> 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            sfmap[wr] = (char) eob;
            mtfFreq[eob]++;
            this.nMTF = wr + 1;
        }




        /// <summary>
        /// Indicates whether the stream can be read.
        /// </summary>
        /// <remarks>
        /// The return value depends on whether the captive stream supports reading.
        /// </remarks>
        public override bool CanRead
        {
            get { return false; }
        }

        /// <summary>
        /// Indicates whether the stream supports Seek operations.
        /// </summary>
        /// <remarks>
        /// Always returns false.
        /// </remarks>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        /// Indicates whether the stream can be written.
        /// </summary>
        /// <remarks>
        /// The return value depends on whether the captive stream supports writing.
        /// </remarks>
        public override bool CanWrite
        {
            get
            {
                if (this.output == null) throw new ObjectDisposedException("BZip2Stream");
                return this.output.CanWrite;
            }
        }

        /// <summary>
        /// Reading this property always throws a <see cref="NotImplementedException"/>.
        /// </summary>
        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// The position of the stream pointer.
        /// </summary>
        ///
        /// <remarks>
        ///   Setting this property always throws a <see
        ///   cref="NotImplementedException"/>. Reading will return the
        ///   total number of uncompressed bytes written through.
        /// </remarks>
        public override long Position
        {
            get
            {
                return this.totalBytesWritten;
            }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Calling this method always throws a <see cref="NotImplementedException"/>.
        /// </summary>
        /// <param name="offset">this is irrelevant, since it will always throw!</param>
        /// <param name="origin">this is irrelevant, since it will always throw!</param>
        /// <returns>irrelevant!</returns>
        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calling this method always throws a <see cref="NotImplementedException"/>.
        /// </summary>
        /// <param name="value">this is irrelevant, since it will always throw!</param>
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calling this method always throws a <see cref="NotImplementedException"/>.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }





        private class CompressionState
        {
            // with blockSize 900k
            public readonly bool[] inUse = new bool[256]; // 256 byte
            public readonly byte[] unseqToSeq = new byte[256]; // 256 byte
            public readonly int[] mtfFreq = new int[BZip2.MaxAlphaSize]; // 1032 byte
            public readonly byte[] selector = new byte[BZip2.MaxSelectors]; // 18002 byte
            public readonly byte[] selectorMtf = new byte[BZip2.MaxSelectors]; // 18002 byte

            public readonly byte[] generateMTFValues_yy = new byte[256]; // 256 byte
            public byte[][] sendMTFValues_len;

            // byte
            public int[][] sendMTFValues_rfreq;

            // byte
            public readonly int[] sendMTFValues_fave = new int[BZip2.NGroups]; // 24 byte
            public readonly short[] sendMTFValues_cost = new short[BZip2.NGroups]; // 12 byte
            public int[][] sendMTFValues_code;

            // byte
            public readonly byte[] sendMTFValues2_pos = new byte[BZip2.NGroups]; // 6 byte
            public readonly bool[] sentMTFValues4_inUse16 = new bool[16]; // 16 byte

            public readonly int[] stack_ll = new int[BZip2.QSORT_STACK_SIZE]; // 4000 byte
            public readonly int[] stack_hh = new int[BZip2.QSORT_STACK_SIZE]; // 4000 byte
            public readonly int[] stack_dd = new int[BZip2.QSORT_STACK_SIZE]; // 4000 byte

            public readonly int[] mainSort_runningOrder = new int[256]; // 1024 byte
            public readonly int[] mainSort_copy = new int[256]; // 1024 byte
            public readonly bool[] mainSort_bigDone = new bool[256]; // 256 byte

            public int[] heap = new int[BZip2.MaxAlphaSize + 2]; // 1040 byte
            public int[] weight = new int[BZip2.MaxAlphaSize * 2]; // 2064 byte
            public int[] parent = new int[BZip2.MaxAlphaSize * 2]; // 2064 byte

            public readonly int[] ftab = new int[65537]; // 262148 byte
            // ------------
            // 333408 byte

            public byte[] block; // 900021 byte
            public int[] fmap; // 3600000 byte
            public char[] sfmap; // 3600000 byte

            // ------------
            // 8433529 byte
            // ============

            /**
             * Array instance identical to sfmap, both are used only
             * temporarily and independently, so we do not need to allocate
             * additional memory.
             */
            public char[] quadrant;

            public CompressionState(int blockSize100k)
            {
                int n = blockSize100k * BZip2.BlockSizeMultiple;
                this.block = new byte[(n + 1 + BZip2.NUM_OVERSHOOT_BYTES)];
                this.fmap = new int[n];
                this.sfmap = new char[2 * n];
                this.quadrant = this.sfmap;
                this.sendMTFValues_len = BZip2.InitRectangularArray<byte>(BZip2.NGroups,BZip2.MaxAlphaSize);
                this.sendMTFValues_rfreq = BZip2.InitRectangularArray<int>(BZip2.NGroups,BZip2.MaxAlphaSize);
                this.sendMTFValues_code = BZip2.InitRectangularArray<int>(BZip2.NGroups,BZip2.MaxAlphaSize);
            }

        }



    }

}
