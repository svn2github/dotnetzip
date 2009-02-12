// ConvertZipToSfx.cs
// ------------------------------------------------------------------
//
// This is a command-line tool that creates a self-extracting Zip archive, given a 
// standard zip archive.  
// It requires the .NET Framework 2.0 on the target machine in order to run. 
//
//
// The Visual Studio Project is a little weird.  There are code files that ARE NOT compiled
// during a normal build of the VS Solution.  They are marked as embedded resources.  These
// are the various "boilerplate" modules that are used in the self-extractor. These modules are:
//   WinFormsSelfExtractorStub.cs
//   WinFormsSelfExtractorStub.Designer.cs
//   CommandLineSelfExtractorStub.cs
//   PasswordDialog.cs
//   PasswordDialog.Designer.cs
//
// At design time, if you want to modify the way the GUI looks, you have to mark those modules
// to have a "compile" build action.  Then tweak em, test, etc.  Then again mark them as 
// "Embedded resource". 
//
//
// Author: Dinoch
// built on host: DINOCH-2
//
// ------------------------------------------------------------------
//
// Copyright (c) 2008 by Dino Chiesa
// All rights reserved!
// 
//
// ------------------------------------------------------------------

using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;

using Ionic.Zip;

namespace Ionic.Zip.Examples
{

    public class ConvertZipToSfx
    {
        private ConvertZipToSfx() { }

        public ConvertZipToSfx(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-extractdir":
                        if (i >= args.Length - 1 || ExtractDir != null)
                        {
                            Usage();
                            return;
                        }
                        ExtractDir = args[++i];
                        break;
                    case "-cmdline":
                        flavor = Ionic.Zip.SelfExtractorFlavor.ConsoleApplication;
                        break;
                    default:
                        // positional args
                        if (ZipFileToConvert == null)
                            ZipFileToConvert = args[i];
                        else
                        {
                            Usage();
                            return;
                        }
                        break;
                }
            }
        }

        string ZipFileToConvert = null;
        string ExtractDir = null;
        SelfExtractorFlavor flavor = Ionic.Zip.SelfExtractorFlavor.WinFormsApplication;

        public void Run()
        {
            if (ZipFileToConvert == null)
            {
                Console.WriteLine("No zipfile specified.\n");
                Usage();
                return;
            }

            if (!System.IO.File.Exists(ZipFileToConvert))
            {
                Console.WriteLine("That zip file does not exist!\n");
                Usage();
                return;
            }

            Convert();
        }



        private void Convert()
        {
            string TargetName = ZipFileToConvert.Replace(".zip", ".exe");

            Console.WriteLine("Converting file {0} to SFX {1}", ZipFileToConvert, TargetName);

            using (ZipFile zip = ZipFile.Read(ZipFileToConvert))
            {
                zip.StatusMessageTextWriter = Console.Out;
		zip.SaveSelfExtractor(TargetName, flavor, ExtractDir);
            }
        }


        private static void Usage()
        {
            Console.WriteLine("usage:\n  CreateSelfExtractor [-cmdline] [-extractdir <xxxx>]  <Zipfile>");
        }



        public static void Main(string[] args)
        {
            try
            {
                ConvertZipToSfx me = new ConvertZipToSfx(args);
                me.Run();
            }
            catch (System.Exception exc1)
            {
                Console.WriteLine("Exception while creating the self extracting archive: {0}", exc1.ToString());
            }
        }


    }
}
