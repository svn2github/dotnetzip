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
//     csc /debug+ /target:exe /r:Ionic.Zip.dll /out:ZipIt.exe ZipIt.cs 
//
// Fri, 23 Feb 2007  11:51
//

using System;
using Ionic.Zip;

namespace Ionic.Zip.Examples
{

    public class ZipIt
    {
        private static void Usage()
        {
            string UsageMessage =
            "Zipit.exe:  zip up a directory, file, or a set of them, into a zipfile.\n" +
            "            Depends on Ionic's DotNetZip library. This is version {0} of the utility.\n" +
            "usage:\n   ZipIt.exe <ZipFileToCreate> [arguments]\n" +
            "\narguments: \n" +
            "  -utf8                 - use UTF-8 encoding for entries with comments or\n" +
            "                          filenames that cannot be encoded with the default IBM437\n" +
            "                          code page.\n" +
            "  -aes                  - use WinZip-compatible AES 256-bit encryption for entries\n" +
            "                          subsequently added to the archive. Requires a password.\n" +
            "  -sfx [w|c]            - create a self-extracting archive, either a Windows or console app.\n" +
            "  -64                   - use ZIP64 extensions, for large files or large numbers of files.\n" +
            "  -cp <codepage>        - use the specified numeric codepage for entries with comments \n" +
            "                          or filenames that cannot be encoded with the default IBM437\n" +
            "                          code page.\n" +
            "  -p <password>         - apply the specified password for all succeeding files added.\n" +
            "                          use \"\" to reset the password to nil.\n" +
            "  -c <comment>          - use the given comment for the next file added to the archive.\n" +
            "  -zc <comment>         - use the given comment for the archive.\n" +
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

            //if (!args[0].EndsWith(".zip"))
            //{
            //    Console.WriteLine("The filename must end with .zip!\n");
            //    Usage();
            //}
            if (System.IO.File.Exists(args[0]))
            {
                System.Console.Error.WriteLine("That zip file ({0}) already exists.", args[0]);
            }

            // because the comments and filenames on zip entries may be UTF-8
            // System.Console.OutputEncoding = new System.Text.UTF8Encoding();

            try
            {
                Nullable<SelfExtractorFlavor> flavor = null;
                int codePage = 0;
                ZipEntry e = null;
                string entryComment = null;
                string entryDirectoryPathInArchive = "";

                using (ZipFile zip = new ZipFile(args[0])) // read/update an existing zip, or create a new one.
                {
                    zip.StatusMessageTextWriter = System.Console.Out;
                    for (int i = 1; i < args.Length; i++)
                    {
                        switch (args[i])
                        {
                            case "-p":
                                i++;
                                if (args.Length <= i) Usage();
                                zip.Password = (args[i] == "") ? null : args[i];
                                break;

                            case "-flat":
                                entryDirectoryPathInArchive = "";
                                break;

                            case "-aes":
                                zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                                break;

                            case "-sfx":
                                i++;
                                if (args.Length <= i) Usage();
                                if (args[i] != "w" && args[i] != "c") Usage();
                                flavor = new Nullable<SelfExtractorFlavor>
                                    ((args[i] == "w") ? SelfExtractorFlavor.WinFormsApplication : SelfExtractorFlavor.ConsoleApplication);
                                break;

                            case "-utf8":
                                zip.UseUnicodeAsNecessary = true;
                                break;

                            case "-64":
                                zip.UseZip64WhenSaving = Zip64Option.Always;
                                break;

                            case "-s":
                                i++;
                                if (args.Length <= i) Usage();
                                string entryName = args[i];
                                i++;
                                if (args.Length <= i) Usage();
                                string content = args[i];
                                e = zip.AddFileFromString(entryName, entryDirectoryPathInArchive, content);
                                if (entryComment != null)
                                {
                                    e.Comment = entryComment;
                                    entryComment = null;
                                }
                                break;

                            case "-c":
                                i++;
                                if (args.Length <= i) Usage();
                                entryComment = args[i];  // for the next entry
                                break;

                            case "-zc":
                                i++;
                                if (args.Length <= i) Usage();
                                zip.Comment = args[i];
                                break;

                            case "-cp":
                                i++;
                                if (args.Length <= i) Usage();
                                System.Int32.TryParse(args[i], out codePage);
                                if (codePage != 0)
                                    zip.ProvisionalAlternateEncoding = System.Text.Encoding.GetEncoding(codePage);
                                break;

                            case "-d":
                                i++;
                                if (args.Length <= i) Usage();
                                entryDirectoryPathInArchive = args[i];
                                break;


                            default:
                                // UpdateItem will add Files or Dirs, recurses subdirectories
                                zip.UpdateItem(args[i], entryDirectoryPathInArchive);

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
                                    entryComment = null;
                                }
                                break;
                        }
                    }

                    if (!flavor.HasValue)
                        zip.Save();
                    else 
                        zip.SaveSelfExtractor(args[0], flavor.Value);

                }
            }
            catch (System.Exception ex1)
            {
                System.Console.Error.WriteLine("Exception: " + ex1);
            }
        }
    }
}