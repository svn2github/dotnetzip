// Extensions.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa.  
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
// last saved (in emacs): 
// Time-stamp: <2009-June-18 18:45:59>
//
// ------------------------------------------------------------------
//
// This module defines various extension methods used within DotNetZip.
//
//


using System;

#pragma warning disable 1591
// This nearly empty namespace is necessary to allow extension methods
// to work in a .NET 2.0 compile. 
namespace System.Runtime.CompilerServices
{
    public class ExtensionAttribute : Attribute { }
}



namespace Ionic.Zip
{
    public static class Extensions
    {
        // Workitem 7889: handle ERROR_LOCK_VIOLATION during read
        public static int ReadWithRetry(this System.IO.Stream s, byte[] buffer, int offset, int count, string FileName)
        {
            int n = 0;
            bool done = false;
            int retries= 0;
            do
            {
                try 
                {
                    n= s.Read(buffer, offset, count);
                    done = true;
                }
                catch (System.IO.IOException ioexc1)
                {
                    uint hresult = unchecked((uint)System.Runtime.InteropServices.Marshal.GetHRForException(ioexc1));
                    if (hresult != 0x80070021)
                        throw new System.IO.IOException(String.Format("Cannot read file {0}", FileName), ioexc1);
                    retries++;
                    if (retries > 10)
                        throw new System.IO.IOException(String.Format("Cannot read file {0}, at offset 0x{1:X8} after 10 retries", FileName, offset), ioexc1);
                    System.Threading.Thread.Sleep(250 + retries * 20);
                }
            }
            while (!done);
            
            return n;
        }
    }

    
}