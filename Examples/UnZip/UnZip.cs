// UnZip.cs
// 
// ----------------------------------------------------------------------
// Copyright (c) 2006, 2007 Microsoft Corporation.  All rights reserved.
//
// This example is released under the Microsoft Public License . 
// See the license.txt file accompanying this release for 
// full details. 
//
// ----------------------------------------------------------------------
//
// This command-line utility unzips a zipfile into the specified directory. 
//
// compile with:
//     csc /target:exe /r:Ionic.Utils.Zip.dll /out:UnZip.exe UnZip.cs 
//
// Wed, 29 Mar 2006  14:36
//


using System;
using Ionic.Utils.Zip;

namespace Ionic.Utils.Zip.Examples
{
    public class UnZip
    {

        private static void Usage()
        {
            Console.WriteLine("usage:\n" +
                      "  unzip <zipfile> [<unpackdirectory>]\n" +
                      "     unzips all files in the archive to the specified directory. If no \n" +
                      "     directory is provided, this utility uses the current directory.\n\n" +
                      "  unzip - <zipfile> <entry>\n" +
                      "     unzip the specified entry from the archive to the console.\n\n" +
                      "  unzip -l <zipfile>\n" +
                      "     lists the entries in the zip archive.\n"
                      );
            Environment.Exit(1);
        }


        public static void Main(String[] args)
        {
            int i = 0;
            string zipfile = null;
            string targdir = ".";
            string entryToExtract = null;
            bool WantExtract = true;

            if (args.Length == 0) Usage();

            if (args[0] == "-")
            {
                i++;
                if (args.Length <= i) Usage();

                zipfile = args[i];
                i++;
                if (args.Length <= i) Usage();

                entryToExtract = args[i];
                i++;
            }
            else
            {
                if (args[0] == "-l")
                {
                    i++;
                    WantExtract = false;
                }
                if (args.Length <= i) Usage();

                zipfile = args[i];
                i++;
                if (args.Length > i)
                {
                    targdir = args[i];
                    i++;
                }
            }

            if (!System.IO.File.Exists(zipfile))
            {
                Console.WriteLine("That zip file does not exist!\n");
                Usage();
            }

            try
            {
                using (ZipFile zip = ZipFile.Read(zipfile))
                {
                    if (entryToExtract != null)
                    {
                        zip.Extract(entryToExtract, Console.OpenStandardOutput());
                    }
                    else
                    {
                        // extract all

                        // The logic here does the same thing as the
                        // ExtractAll() method on the ZipFile class.  But in
                        // this case we *could* have control over it, for
                        // example only extract files of a certain type, or
                        // whose names matched a certain pattern, or whose
                        // lastmodified times fit a certain condition, etc.
                        // We can also display status for each entry, as here.

                        bool header = true;
                        foreach (ZipEntry e in zip)
                        {
                            if (header)
                            {
                                System.Console.WriteLine("Zipfile: {0}", zip.Name);
                                System.Console.WriteLine("Version Needed: 0x{0:X2}", e.VersionNeeded);
                                System.Console.WriteLine("BitField: 0x{0:X2}", e.BitField);
                                System.Console.WriteLine("Compression Method: 0x{0:X2}", e.CompressionMethod);
                                System.Console.WriteLine("\n{1,-22} {2,-6} {3,4}   {4,-8}  {0}",
                                             "Filename", "Modified", "Size", "Ratio", "Packed");
                                System.Console.WriteLine(new System.String('-', 72));
                                header = false;
                            }

                            System.Console.WriteLine("{1,-22} {2,-6} {3,4:F0}%   {4,-8}  {0}",
                                         e.FileName,
                                         e.LastModified.ToString("yyyy-MM-dd HH:mm:ss"),
                                         e.UncompressedSize,
                                         e.CompressionRatio,
                                         e.CompressedSize);

                            if (WantExtract) e.Extract(targdir);
                        }
                    }
                } // end using(), the underlying file is closed.
            }
            catch (System.Exception ex1)
            {
                System.Console.Error.WriteLine("exception: " + ex1);
            }

            Console.WriteLine();
        }
    }
}