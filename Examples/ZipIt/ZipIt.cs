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
            //"  -c <comment>          - use the given comment for the next file added to the archive.\n" +
            //"  -flat                - store the files in a flat dir structure; do not use the \n" +
            //"                         directory paths from the source files.\n" +
            string UsageMessage =
            "Zipit.exe:  zip up a directory, file, or a set of them, into a zipfile.\n" +
            "            Depends on Ionic's DotNetZip library. This is version {0} of the utility.\n" +
            "usage:\n   ZipIt.exe <ZipFileToCreate> [arguments]\n" +
            "\narguments: \n" +
            "  <directory> | <file> - a directory or file to add to the archive.\n" +
            "  <selector>           - a file selection expression.  Examples: \n" +
            "                           *.txt \n" +
            "                           (name = *.txt) OR (name = *.xml) \n" +
            "                           (attrs = H) OR (name != *.xml) \n" +
            "                           (size > 1g) AND (mtime < 2009-06-29) \n" +
            "                           (ctime > 2009-04-29) AND (size < 10kb) \n" +
            "                         You must surround an expression that includes spaces with quotes.\n" +
            "  -64                  - use ZIP64 extensions, for large files or large numbers of files.\n" +
            "  -aes                 - use WinZip-compatible AES 256-bit encryption for entries\n" +
            "                         subsequently added to the archive. Requires a password.\n" +
            "  -cp <codepage>       - use the specified numeric codepage for entries with comments \n" +
            "                         or filenames that cannot be encoded with the default IBM437\n" +
            "                         code page.\n" +
            "  -d <path>            - use the given directory path in the archive for\n" +
            "                         succeeding items added to the archive.\n" +
            "  -D <path>            - find files in the given directory on disk.\n" +
            "  -j-                  - do not traverse NTFS junctions\n" +
            "  -j+                  - traverse NTFS junctions (default)\n" +
            "  -L <level>           - compression level, 0..9 (Default is 6).\n" +
            "  -p <password>        - apply the specified password for all succeeding files added.\n" +
            "                         use \"\" to reset the password to nil.\n" +
            "  -progress            - emit progress reports (good when creating large zips)\n" +
            "  -r-                  - don't recurse directories (default).\n" +
            "  -r+                  - recurse directories.\n" +
            "  -s <entry> 'string'  - insert an entry of the given name into the \n" +
            "                         archive, with the given string as its content.\n" +
            "  -sfx [w|c]           - create a self-extracting archive, either a Windows or console app.\n" +
            "  -split <maxsize>     - produce a split zip, with the specified maximum size. You can\n" +
            "                         optionally use kb or mb as a suffix to the size. \n" +
            "                         -split is not compatible with -sfx.\n" +
            "  -Tw+                 - store Windows-format extended times (default).\n" +
            "  -Tw-                 - don't store Windows-format extended times.\n" +
            "  -Tu+                 - store Unix-format extended times (default).\n" +
            "  -Tu-                 - don't store Unix-format extended times (default).\n" +
            "  -UTnow               - use uniform date/time, NOW, for all entries. \n" +
            "  -UTnewest            - use uniform date/time, newest entry, for all entries. \n" +
            "  -UToldest            - use uniform date/time, oldest entry, for all entries. \n" +
            "  -UT <datetime>       - use uniform date/time, specified, for all entries. \n" +
            "  -utf8                - use UTF-8 encoding for entries with comments or\n" +
            "                         filenames that cannot be encoded with the default IBM437\n" +
            "                         code page.\n" +
            "  -zc <comment>        - use the given comment for the archive.\n";

            Console.WriteLine(UsageMessage,
                      System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Environment.Exit(1);
        }


        static bool justHadByteUpdate= false;
        static bool isCanceled= false;
        static bool wantProgressReports = false;

        private static void SaveProgress(object sender, SaveProgressEventArgs e)
        {
            if (isCanceled)
            {
                e.Cancel = true;
                return;
            }
            if (!wantProgressReports) return;
            
            switch(e.EventType)
            {
                case ZipProgressEventType.Saving_Started:
                    Console.Error.WriteLine("Saving: {0}", e.ArchiveName);
                    break;
                
                case ZipProgressEventType.Saving_Completed:
                    justHadByteUpdate= false; 
                    Console.Error.WriteLine();
                    Console.Error.WriteLine("Done: {0}", e.ArchiveName);
                    break;

                case ZipProgressEventType.Saving_BeforeWriteEntry:
                    if (justHadByteUpdate) 
                        Console.Error.WriteLine();
                    Console.Error.WriteLine("  Writing: {0} ({1}/{2})",  
                                      e.CurrentEntry.FileName, e.EntriesSaved+1, e.EntriesTotal);
                    justHadByteUpdate= false;
                    break;
                    
                case ZipProgressEventType.Saving_AfterWriteEntry:
                    break;
        
                case ZipProgressEventType.Saving_EntryBytesRead:
                    if (justHadByteUpdate)
                        Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Error.Write("     {0}/{1} ({2:N0}%)", e.BytesTransferred, e.TotalBytesToTransfer,
                                  e.BytesTransferred / (0.01 * e.TotalBytesToTransfer ));
                    justHadByteUpdate= true;
                    break;
            }
        }


        static void CtrlC_Handler(object sender, ConsoleCancelEventArgs args)
        {
            isCanceled = true;
            Console.WriteLine("\nCtrl-C");
            //cleanupCompleted.WaitOne();
            // prevent the process from exiting until cleanup is done: 
            args.Cancel = true;
        }


        public static void Main(String[] args)
        {
            bool saveToStdout = false;
            if (args.Length < 2) Usage();

            if (args[0]=="-")
            {
                saveToStdout = true;                
            }
            else if (System.IO.File.Exists(args[0]))
            {
                System.Console.Error.WriteLine("That zip file ({0}) already exists.", args[0]);
            }
            

            // Because the comments and filenames on zip entries may be UTF-8
            // System.Console.OutputEncoding = new System.Text.UTF8Encoding();

            Console.CancelKeyPress += CtrlC_Handler;
            
            try
            {
                Nullable<SelfExtractorFlavor> flavor = null;
                int codePage = 0;
                ZipEntry e = null;
                int _UseUniformTimestamp = 0;
                DateTime _fixedTimestamp= System.DateTime.Now;
                //string entryComment = null;
                string entryDirectoryPathInArchive = "";
                string directoryOnDisk = null;
                bool recurseDirectories = false;
                
                using (ZipFile zip = new ZipFile(args[0])) // read/update an existing zip, or create a new one.
                {
                    zip.StatusMessageTextWriter = System.Console.Error;
                    zip.SaveProgress += SaveProgress;
                    for (int i = 1; i < args.Length; i++)
                    {
                        switch (args[i])
                        {
#if NONSENSE
                            case "-flat":
                                entryDirectoryPathInArchive = "";
                                break;
#endif

                            case "-64":
                                zip.UseZip64WhenSaving = Zip64Option.Always;
                                break;

                            case "-aes":
                                zip.Encryption = EncryptionAlgorithm.WinZipAes256;
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

                            case "-D":
                                i++;
                                if (args.Length <= i) Usage();
                                directoryOnDisk = args[i];
                                break;
                                
                            case "-j-":
                                zip.AddDirectoryWillTraverseReparsePoints = false;
                                break;
                                
                            case "-j+":
                                zip.AddDirectoryWillTraverseReparsePoints = true;
                                break;
                                    
                            case "-L":
                                i++;
                                if (args.Length <= i) Usage();
                                zip.CompressionLevel = (Ionic.Zlib.CompressionLevel)
                                    System.Int32.Parse(args[i]);
                                break;
                                
                            case "-p":
                                i++;
                                if (args.Length <= i) Usage();
                                zip.Password = (args[i] == "") ? null : args[i];
                                break;

                            case "-progress":
                                wantProgressReports = true;
                                break;

                            case "-r-":
                                recurseDirectories = false;
                                break;

                            case "-r+":
                                recurseDirectories = true;
                                break;

                            case "-s":
                                i++;
                                if (args.Length <= i) Usage();
                                string entryName = args[i];
                                i++;
                                if (args.Length <= i) Usage();
                                string content = args[i];
                                e = zip.AddEntry(System.IO.Path.Combine(entryDirectoryPathInArchive, entryName), content);
                                //                                 if (entryComment != null)
                                //                                 {
                                //                                     e.Comment = entryComment;
                                //                                     entryComment = null;
                                //                                 }
                                break;

                            case "-sfx":
                                i++;
                                if (args.Length <= i) Usage();
                                if (args[i] != "w" && args[i] != "c") Usage();
                                flavor = new Nullable<SelfExtractorFlavor>
                                    ((args[i] == "w") ? SelfExtractorFlavor.WinFormsApplication : SelfExtractorFlavor.ConsoleApplication);
                                break;

                            case "-split":
                                i++;
                                if (args.Length <= i) Usage();
                                if (args[i].EndsWith("K") || args[i].EndsWith("k"))
                                    zip.MaxOutputSegmentSize = Int32.Parse(args[i].Substring(0, args[i].Length - 1)) * 1024;
                                else if (args[i].EndsWith("M") || args[i].EndsWith("m"))
                                    zip.MaxOutputSegmentSize = Int32.Parse(args[i].Substring(0, args[i].Length - 1)) * 1024 * 1024;
                                else
                                    zip.MaxOutputSegmentSize = Int32.Parse(args[i]);
                                break;

                            case "-Tw+":
                                zip.EmitTimesInWindowsFormatWhenSaving = true;
                                break;

                            case "-Tw-":
                                zip.EmitTimesInWindowsFormatWhenSaving = false;
                                break;

                            case "-Tu+":
                                zip.EmitTimesInUnixFormatWhenSaving = true;
                                break;

                            case "-Tu-":
                                zip.EmitTimesInUnixFormatWhenSaving = false;
                                break;

                            case "-UTnow":
                                _UseUniformTimestamp = 1;
                                _fixedTimestamp = System.DateTime.UtcNow;
                                break;
                                
                            case "-UTnewest":
                                _UseUniformTimestamp = 2;
                                break;
                                
                            case "-UToldest":
                                _UseUniformTimestamp = 3;
                                break;
                                
                            case "-UT":
                                i++;
                                if (args.Length <= i) Usage();
                                _UseUniformTimestamp = 4;
                                try
                                {
                                    _fixedTimestamp= System.DateTime.Parse(args[i]);
                                }
                                catch
                                {
                                    throw new ArgumentException("-UT");
                                }
                                break;

                            case "-utf8":
                                zip.UseUnicodeAsNecessary = true;
                                break;

#if NOT
                            case "-c":
                                i++;
                                if (args.Length <= i) Usage();
                                entryComment = args[i];  // for the next entry
                                break;
#endif
                                
                            case "-zc":
                                i++;
                                if (args.Length <= i) Usage();
                                zip.Comment = args[i];
                                break;

                            default:
                                #if OLD
                                // UpdateItem will add Files or Dirs, recurses subdirectories
                                e = zip.UpdateItem(args[i], entryDirectoryPathInArchive);

                                // try to add a comment if we have one
                                if (entryComment != null)
                                {
                                    e.Comment = entryComment;
                                    // reset the comment
                                    entryComment = null;
                                }
                                #else
                                {
                                    bool wantRecurse = recurseDirectories || args[i].Contains("\\");
//                                         Console.WriteLine("spec({0})", args[i]);
//                                         Console.WriteLine("dir({0})", directoryOnDisk);
//                                         Console.WriteLine("dirInArc({0})", entryDirectoryPathInArchive);
//                                         Console.WriteLine("recurse({0})", recurseDirectories);

                                        zip.UpdateSelectedFiles(args[i], directoryOnDisk, entryDirectoryPathInArchive,
                                                                wantRecurse);
                                }
                                #endif
                                    
                                break;
                        }
                    }

                    if (_UseUniformTimestamp > 0)
                    {
                        if (_UseUniformTimestamp==2)
                        {
                            // newest
                            _fixedTimestamp = new System.DateTime(1601,1,1,0,0,0);
                            foreach(var entry in zip)
                            {
                                if (entry.LastModified > _fixedTimestamp)
                                    _fixedTimestamp = entry.LastModified;
                            }
                        }
                        else if (_UseUniformTimestamp==3)
                        {
                            // oldest
                            foreach(var entry in zip)
                            {
                                if (entry.LastModified < _fixedTimestamp)
                                    _fixedTimestamp = entry.LastModified;
                            }
                        }
                        
                        foreach(var entry in zip)
                        {
                            entry.LastModified = _fixedTimestamp;
                        }
                    }
                    
                    if (!flavor.HasValue)
                    {
                        if (saveToStdout)
                            zip.Save(Console.OpenStandardOutput());
                        else
                            zip.Save();
                    }
                    else
                    {
                        if (saveToStdout)
                            throw new Exception("Cannot save SFX to stdout, sorry! See http://dotnetzip.codeplex.com/WorkItem/View.aspx?WorkItemId=7246");
                        zip.SaveSelfExtractor(args[0], flavor.Value);
                        
                    }

                }
            }
            catch (System.Exception ex1)
            {
                System.Console.Error.WriteLine("Exception: " + ex1);
            }
        }
    }
}