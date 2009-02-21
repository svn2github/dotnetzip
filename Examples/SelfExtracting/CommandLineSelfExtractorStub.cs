// CommandLineSelfExtractorStub.cs
// ------------------------------------------------------------------
//
// The stub for the command-line self-extractor....
// 
// Author: Dinoch
// built on host: DINOCH-2
// Created Fri Jun 06 14:51:31 2008
//
// last saved: 
// Time-stamp: <2009-February-20 11:47:22>
// ------------------------------------------------------------------
//
// Copyright (c) 2008 by Dino Chiesa
// All rights reserved!
// 
//
// ------------------------------------------------------------------

using System;
using System.Reflection;
using System.Resources;
using System.IO;
using Ionic.Zip;


namespace Ionic.Zip
{

    public class SelfExtractor
    {
        const string DllResourceName = "Ionic.Zip.dll";

        string TargetDirectory = null;
        bool WantOverwrite = false;
        bool ListOnly = false;
        bool Verbose = false;
        string Password = null;

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
                    case "-v":
                        Verbose = true;
                        break;
                    default:
                        // positional args
                        if (TargetDirectory == null)
                            TargetDirectory = args[i];
                        else
                            Usage();
                        break;
                }
            }

            if (!ListOnly && TargetDirectory == null)
            {
                Console.WriteLine("No target directory specified.\n");
                Usage();
            }
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
            Assembly a2 = null;

            Stream s = a1.GetManifestResourceStream("Ionic.Zip.dll");
            int n = 0;
            int totalBytesRead = 0;
            byte[] bytes = new byte[1024];
            do
            {
                n = s.Read(bytes, 0, bytes.Length);
                totalBytesRead += n;
            }
            while (n > 0);

            byte[] block = new byte[totalBytesRead];
            s.Seek(0, System.IO.SeekOrigin.Begin);
            s.Read(block, 0, block.Length);

            a2 = Assembly.Load(block);

            return a2;
        }


        public void Run()
        {
            //string currentPassword = null;

            // There are only two embedded resources.
            // One of them is the zip dll.  The other is the zip archive.
            // We load the resouce that is NOT the DLL, as the zip archive.
            Assembly a = Assembly.GetExecutingAssembly();

#if OLDSTYLE
            string[] x = a.GetManifestResourceNames();
            Stream s = null;
            foreach (string name in x)
            {
                if ((name != DllResourceName) && (name.EndsWith(".zip")))
                {
                    s = a.GetManifestResourceStream(name);
                    break;
                }
            }

            if (s == null)
            {
                Console.WriteLine("No Zip archive found.");
                return;
            }
#endif

            try
            {
#if OLDSTYLE
                using (global::Ionic.Zip.ZipFile zip = global::Ionic.Zip.ZipFile.Read(s))
#else
                // workitem 7067
                using (global::Ionic.Zip.ZipFile zip = global::Ionic.Zip.ZipFile.Read(a.Location))
#endif
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
                return;
            }

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



        public static void Main(string[] args)
        {
            try
            {
                SelfExtractor me = new SelfExtractor(args);
                me.Run();
            }
            catch (System.Exception exc1)
            {
                Console.WriteLine("Exception while extracting: {0}", exc1.ToString());
            }
        }

    }
}
