// ZipIt.cs
// 
// ----------------------------------------------------------------------
// Copyright (c) 2006, 2007, 2008 Microsoft Corporation.  All rights reserved.
//
// This example is released under the Microsoft Permissive License of
// October 2006.  See the license.txt file accompanying this release for 
// full details. 
//
// ----------------------------------------------------------------------
//
// This utility zips up a set of files and directories specified on the command line.
// It is like a generalized ZipDir tool (See ZipDir.cs).
//
// compile with:
//     csc /debug+ /target:exe /r:Ionic.Utils.Zip.dll /out:ZipIt.exe ZipIt.cs 
//
// Fri, 23 Feb 2007  11:51
//

using System;
using Ionic.Utils.Zip;

namespace Ionic.Utils.Zip.Examples
{

    public class ZipIt
    {
        private static void Usage()
        {
            Console.WriteLine("usage: ZipIt.exe <ZipFileToCreate> [-p <password> | <directory> | <file> ...]");
            Console.WriteLine("zip up a directory, file, or a set of them, into a zipfile.\n");
            Environment.Exit(1);
        }

        public static void Main(String[] args)
        {
            if (args.Length < 2) Usage();

            if (!args[0].EndsWith(".zip"))
            {
                Console.WriteLine("The filename must end with .zip!\n");
                Usage();
            }
            if (System.IO.File.Exists(args[0]))
            {
                System.Console.Error.WriteLine("That zip file ({0}) already exists.", args[0]);
            }

            try
            {
                using (ZipFile zip = new ZipFile(args[0]))
                {
                    zip.StatusMessageTextWriter = System.Console.Out;
                    for (int i = 1; i < args.Length; i++)
                    {
                        if (args[i] == "-p")
                        {
                            i++;
                            if (args.Length <= i) Usage();
                            zip.Password = args[i++];
                        }
                        zip.AddItem(args[i]); // will add Files or Dirs, recurses subdirectories
                    }
                    zip.Save();
                }
            }
            catch (System.Exception ex1)
            {
                System.Console.Error.WriteLine("Exception: " + ex1);
            }

        }
    }
}