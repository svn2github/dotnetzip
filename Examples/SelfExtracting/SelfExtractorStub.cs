// SelfExtractor.cs
// ------------------------------------------------------------------
//
// Description goes here....
// 
// Author: Dinoch
// built on host: DINOCH-2
// Created Fri Jun 06 14:51:31 2008
//
// last saved: 
// Time-stamp: <Friday, June 06, 2008  17:44:24  (by dinoch)>
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
using Ionic.Utils.Zip;


namespace Ionic.Utils.Zip
{

    public class SelfExtractor
    {
        // ctor
        public SelfExtractor() { }

        static SelfExtractor()
        {
            AppDomain.CurrentDomain.AssemblyResolve +=
          new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        }

        static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly a1 = Assembly.GetExecutingAssembly();
            Assembly a2 = null;

            Stream s = a1.GetManifestResourceStream("Ionic.Utils.Zip.dll");
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


        public void Run(string targetDirectory)
        {
            Assembly a = Assembly.GetExecutingAssembly();
            string zipname = Path.GetFileNameWithoutExtension(a.Location);
            Stream s = a.GetManifestResourceStream(zipname);

            using (ZipFile zip = ZipFile.Read(s))
            {
                zip.ExtractAll(targetDirectory);
            }
        }


        private static void Usage()
        {
            Assembly a = Assembly.GetExecutingAssembly();
            string s = Path.GetFileName(a.Location);
            Console.WriteLine("DotNetZip Self Extractor, see http://www.codeplex.com/DotNetZip");
            Console.WriteLine("usage:\n  {0} <directory>", s);

            Environment.Exit(1);
        }



        public static void Main(string[] args)
        {
            try
            {
                if (args.Length != 1) Usage();

                SelfExtractor me = new SelfExtractor();
                me.Run(args[0]);
            }
            catch (System.Exception exc1)
            {
                Console.WriteLine("Exception while extracting: {0}", exc1.ToString());
            }
        }

    }
}
