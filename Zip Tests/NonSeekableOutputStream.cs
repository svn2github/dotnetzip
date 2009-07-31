// NonSeekableOutputStream.cs
// ------------------------------------------------------------------
//
// Need a non-seekable output stream to test ZIP construction.
// 
// Author: Admin
// built on host: DINOCH-2
// Created Thu Jul 30 22:32:18 2009
//
// last saved: 
// Time-stamp: <2009-July-30 22:47:07>
// ------------------------------------------------------------------
//
// Copyright (c) 2009 by Dino Chiesa
// All rights reserved!
//
// ------------------------------------------------------------------

using System;
using System.IO;


namespace Ionic.Zip.Tests
{
    public class NonSeekableOutputStream : Stream
    {
        protected Stream _s;
        // ctor
        public NonSeekableOutputStream (Stream s) : base()
        {
            if (!s.CanWrite)
                throw new NotSupportedException();
            _s = s;
        }


        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _s.Write(buffer, offset, count);
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
            get { return _s.Length; }
        }

        public override long Position
        {
            get { return _s.Position; }
            set { _s.Position = value; }
        }
        
        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            _s.SetLength(value);
        }
    }
}
