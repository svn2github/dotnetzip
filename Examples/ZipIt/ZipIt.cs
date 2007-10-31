// ZipIt.cs
// 
// ----------------------------------------------------------------------
// Copyright (c) 2006, 2007 Microsoft Corporation.  All rights reserved.
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

public class ZipIt
{

    private static void Usage()
    {
        Console.WriteLine("usage:\n  ZipIt <ZipFileToCreate> [<directory> | <file> ...]");
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

        try
        {
            // ZipFile zip = null;
            if (System.IO.File.Exists(args[0]))
            {
                // The zipfile already exists
            }
            using (ZipFile zip = new ZipFile(args[0]))
            {
                for (int i = 1; i < args.Length; i++)
                {
                    zip.AddItem(args[i], true); // will add Files or Dirs, recurses subdirectories
                }
                zip.Save();
            }

        }
        catch (System.Exception ex1)
        {
            System.Console.Error.WriteLine("exception: " + ex1);
        }

    }
}
