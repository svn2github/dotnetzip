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
	    string UsageMessage= 
		"Zipit.exe:  zip up a directory, file, or a set of them, into a zipfile.\n" +
		"            Depends on Ionic's DotNetZip library. This is version {0} of the utility.\n" +
		"usage:\n   ZipIt.exe <ZipFileToCreate> [arguments]\n" +
		"\narguments: \n" +
        "  -utf8                 - use UTF-8 encoding for non-ASCII characters in comments and\n" + 
        "                          filenames. The default is to use IBM437.\n" +
        "  -p <password>         - apply the specified password for all succeeding files added.\n" +
		"                          use \"\" to reset the password to nil.\n" +
		"  -c <comment>          - use the given comment for the archive or, on \n" + 
		"  -d <path>             - use the given directory path in the archive for\n" + 
		"                          succeeding items added to the archive.\n" +
		"  -s <entry> 'string'   - insert an entry of the given name into the \n" + 
		"                          archive, with the given string as its content.\n" +
		"  -flat                 - store the files in a flat dir structure; do not use the \n" + 
		"                          directory paths from the source files.\n" +
		"  <directory> | <file>  - add the directory or file to the archive.";

	    Console.WriteLine(UsageMessage,
			      System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
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

        // because the comments and filenames on zip entries may be UTF-8
	    // System.Console.OutputEncoding = new System.Text.UTF8Encoding();

	    try
	    {
		ZipEntry e=null;
		string entryComment= null;
		string entryDirectoryPathInArchive = null;

		using (ZipFile zip = new ZipFile(args[0]))
		{
		    zip.StatusMessageTextWriter = System.Console.Out;
		    for (int i = 1; i < args.Length; i++)
		    {
			switch (args[i])
			{
			case "-p":
			    i++;
			    if (args.Length <= i) Usage();
			    zip.Password= (args[i] == "") ? null : args[i];
			    break;

			case "-flat":
			    entryDirectoryPathInArchive= "";
			    break;

            case "-utf8":
                zip.UseUnicode = true;
                break;

			case "-s":
			    i++;
			    if (args.Length <= i) Usage();
			    string entryName = args[i];
			    i++;
			    if (args.Length <= i) Usage();
			    string content = args[i];
			    e= zip.AddFileFromString(content, entryName, "");
			    if (entryComment != null)
			    {
				e.Comment = entryComment;
				entryComment= null;
			    }
			    break;

			case "-c":
			    i++;
			    if (args.Length <= i) Usage();
			    if (zip.Comment == null) zip.Comment = args[i];
			    else entryComment = args[i];
			    break;

			case "-d":
			    i++;
			    if (args.Length <= i) Usage();
			    entryDirectoryPathInArchive = args[i];
			    break;


			default: 
			    // UpdateItem will add Files or Dirs, recurses subdirectories
			    zip.UpdateItem(args[i],entryDirectoryPathInArchive); 

			    // try to add a comment if we have one
			    if (entryComment != null)
			    {
				// can only add a comment if the thing just added was a file. 
				if (zip.EntryFileNames.Contains(args[i]))
				{
				    e = zip[args[i]];
				    e.Comment = entryComment;
				}
				else 
				    Console.WriteLine("Warning: zipit.exe: ignoring comment; cannot add a comment to a directory.");

				// reset the comment
				entryComment= null;
			    }
			    break;
			}
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