// CommandLineSelfExtractorStub.cs
// ------------------------------------------------------------------
//
// Copyright (c)  2008, 2009 Dino Chiesa.  
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
// Time-stamp: <2009-June-02 01:31:23>
//
// ------------------------------------------------------------------
//
// This is a the source module that implements the stub of a
// command-line self-extracting Zip archive - the code included in all
// command-line SFX files.  This code is included as a resource into the
// DotNetZip DLL, and then is compiled at runtime when a SFX is saved. 
//
// ------------------------------------------------------------------


namespace Ionic.Zip
{

    // include the using statements inside the namespace decl, because
    // source code will be concatenated together before compilation. 
    using System;
    using System.Reflection;
    using System.Resources;
    using System.IO;
    using Ionic.Zip;

    public class SelfExtractor
    {
        const string DllResourceName = "Ionic.Zip.dll";

        string TargetDirectory = "@@EXTRACTLOCATION";
        string PostUnpackCmdLine = "@@POST_UNPACK_CMD_LINE";
        bool WantOverwrite = false;
        bool ListOnly = false;
        bool Verbose = false;
        bool wantUsage = false;
        string Password = null;
        
        private bool PostUnpackCmdLineIsSet()
        {
            // What is going on here?
            // The PostUnpackCmdLine is initialized to a particular value, then
            // we test to see if it begins with the first two chars of that value,
            // and ends with the last part of the value.  Why?

            // Here's the thing.  In order to insure the code is right, this module has
            // to compile as it is, as a standalone module.  But then, inside
            // DotNetZip, when generating an SFX, we do a text.Replace on the source
            // code, potentially replacing @@POST_UNPACK_CMD_LINE with an actual value.
            // The test here checks to see if it has been set. 

            return !(PostUnpackCmdLine.StartsWith("@@") && 
                     PostUnpackCmdLine.EndsWith("POST_UNPACK_CMD_LINE"));
        }

        private bool TargetDirectoryIsSet()
        {
            return !(TargetDirectory.StartsWith("@@") && 
                     TargetDirectory.EndsWith("EXTRACTLOCATION"));
        }

        // ctor
        private SelfExtractor() { }

        // ctor
        public SelfExtractor(string[] args)
        {

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-p":
                        i++;
                        if (args.Length <= i) Usage();
                        if (Password != null) Usage();
                        Password = args[i];
                        break;
                    case "-o":
                        WantOverwrite = true;
                        break;
                    case "-l":
                        ListOnly = true;
                        break;
                    case "-?":
                        wantUsage = true;
                        break;
                    case "-v":
                        Verbose = true;
                        break;
                    default:
                        // positional args
                        if (!TargetDirectoryIsSet())
                            TargetDirectory = args[i];
                        else
                            Usage();
                        break;
                }
            }

            if (wantUsage)
                Usage();

            if (!ListOnly && !TargetDirectoryIsSet())
                TargetDirectory = ".";  // cwd

            if (ListOnly && (WantOverwrite || Verbose))
            {
                Console.WriteLine("Inconsistent options.\n");
                Usage();
            }
        }



        static SelfExtractor()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Resolver);
        }

        static System.Reflection.Assembly Resolver(object sender, ResolveEventArgs args)
        {
            Assembly a1 = Assembly.GetExecutingAssembly();
            Stream s = a1.GetManifestResourceStream("Ionic.Zip.dll");
            byte[] block = new byte[s.Length];
            s.Read(block, 0, block.Length);
            Assembly a2 = Assembly.Load(block);
            return a2;
        }


        public int Run()
        {
            if (wantUsage) return 0;
            //string currentPassword = null;

            // There are only two embedded resources.
            // One of them is the zip dll.  The other is the zip archive.
            // We load the resouce that is NOT the DLL, as the zip archive.
            Assembly a = Assembly.GetExecutingAssembly();


            int rc = 0;
            try
            {
                // workitem 7067
                using (global::Ionic.Zip.ZipFile zip = global::Ionic.Zip.ZipFile.Read(a.Location))
                {
                    bool header = true;
                    foreach (global::Ionic.Zip.ZipEntry entry in zip)
                    {
                        if (ListOnly || Verbose)
                        {
                            if (header)
                            {
                                System.Console.WriteLine("Extracting Zip file: {0}", zip.Name);
                                if ((zip.Comment != null) && (zip.Comment != ""))
                                    System.Console.WriteLine("Comment: {0}", zip.Comment);

                                System.Console.WriteLine("\n{1,-22} {2,9}  {3,5}   {4,9}  {5,3} {6,8} {0}",
                                             "Filename", "Modified", "Size", "Ratio", "Packed", "pw?", "CRC");
                                System.Console.WriteLine(new System.String('-', 80));
                                header = false;
                            }

                            System.Console.WriteLine("{1,-22} {2,9} {3,5:F0}%   {4,9}  {5,3} {6:X8} {0}",
                                         entry.FileName,
                                         entry.LastModified.ToString("yyyy-MM-dd HH:mm:ss"),
                                         entry.UncompressedSize,
                                         entry.CompressionRatio,
                                         entry.CompressedSize,
                                         (entry.UsesEncryption) ? "Y" : "N",
                                         entry.Crc32);

                        }

                        if (!ListOnly)
                        {
                            if (entry.Encryption == global::Ionic.Zip.EncryptionAlgorithm.None)
                            {
                                try
                                {
                                    entry.Extract(TargetDirectory, WantOverwrite);
                                }
                                catch (Exception ex1)
                                {
                                    Console.WriteLine("Failed to extract entry {0} -- {1}", entry.FileName, ex1.ToString());
                                    rc++;
                                    break;
                                }
                            }
                            else
                            {
                                if (Password == null)
                                    Console.WriteLine("Cannot extract entry {0} without a password.", entry.FileName);
                                else
                                {
                                    try
                                    {
                                        entry.ExtractWithPassword(TargetDirectory, WantOverwrite, Password);
                                    }
                                    catch (Exception ex2)
                                    {
                                        // probably want a retry here in the case of bad password.
                                        Console.WriteLine("Failed to extract entry {0} -- {1}", entry.FileName, ex2.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("The self-extracting zip file is corrupted.");
                return 4;
            }

            if (rc != 0) return rc;
            
            // potentially execute the embedded command
            if (PostUnpackCmdLineIsSet())
            {
                if (ListOnly)
                {
                    Console.WriteLine("\nExecute on unpack: {0}", PostUnpackCmdLine);
                }
                else
                {
                    try
                    {
                        string[] args = PostUnpackCmdLine.Split( new char[] {' '}, 2);

                        Directory.SetCurrentDirectory(TargetDirectory);
                        System.Diagnostics.Process p = null;
                        if (args.Length > 1)
                            p = System.Diagnostics.Process.Start(args[0], args[1]);
                    
                        else if (args.Length == 1)
                            p = System.Diagnostics.Process.Start(args[0]);
                        // else, nothing.

                        if (p!=null)
                        {
                            p.WaitForExit();
                            rc = p.ExitCode;
                        }
                                             
                    }
                    catch
                    {
                        rc = 5;
                    }
                    
                }
            }

            return rc;
        }


        private static void Usage()
        {
            Assembly a = Assembly.GetExecutingAssembly();
            string s = Path.GetFileName(a.Location);
            Console.WriteLine("DotNetZip Command-Line Self Extractor, see http://www.codeplex.com/DotNetZip");
            Console.WriteLine("usage:\n  {0} [-o] [-v] [-p password] <directory>", s);
            Console.WriteLine("    Extracts entries from the archive.");
            Console.WriteLine("    -o   - overwrite any existing files upon extraction.");
            Console.WriteLine("    -v   - verbose.");

            Console.WriteLine("\n  {0} -l", s);
            Console.WriteLine("    Lists entries in the archive.");
            Environment.Exit(1);
        }



        public static int Main(string[] args)
        {
            int rc = 0;
            try
            {
                SelfExtractor me = new SelfExtractor(args);
                rc = me.Run();
            }
            catch (System.Exception exc1)
            {
                Console.WriteLine("Exception while extracting: {0}", exc1.ToString());
                rc = 255;
            }
            return rc;
        }

    }
}
