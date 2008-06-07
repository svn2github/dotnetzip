// CreateSelfExtractor.cs
// ------------------------------------------------------------------
//
// Description goes here....
// 
// Author: Dinoch
// built on host: DINOCH-2
// Created Fri Jun 06 17:00:15 2008
//
// last saved: 
// Time-stamp: <Friday, June 06, 2008  17:35:41  (by dinoch)>
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



namespace Ionic.Utils.Zip
{

    public class CreateSelfExtractor
    {

        public CreateSelfExtractor() { }

        public void Run(string ZipFileToWrap)
        {
            try
            {
                if (!File.Exists(ZipFileToWrap))
                {
                    Console.WriteLine("The zip file {0} does not exist.", ZipFileToWrap);
                    return;
                }

                Assembly a1 = typeof(ZipFile).Assembly;
                //Console.WriteLine("DotNetZip assembly loc: {0}", a1.Location);

                Microsoft.CSharp.CSharpCodeProvider csharp = new Microsoft.CSharp.CSharpCodeProvider();

                System.CodeDom.Compiler.CompilerParameters cp =
                  new System.CodeDom.Compiler.CompilerParameters(new string[] { a1.Location });
                cp.GenerateInMemory = false;
                cp.GenerateExecutable = true;
                cp.IncludeDebugInformation = false;
                cp.OutputAssembly = ZipFileToWrap + ".exe";

                // add the zip file as an embedded resource
                cp.EmbeddedResources.Add(ZipFileToWrap);

                // add the Ionic.Utils.Zip DLL as an embedded resource
                cp.EmbeddedResources.Add(a1.Location);

                Assembly a2 = Assembly.GetExecutingAssembly();
                Stream s = a2.GetManifestResourceStream("Ionic.Utils.Zip.SelfExtractorStub.cs");
                var sb = new System.Text.StringBuilder();
                using (StreamReader sr = new StreamReader(s))
                {
                    while (sr.Peek() >= 0)                    
                        sb.Append(sr.ReadLine()).Append("\n");                    
                }
                string LiteralSource = sb.ToString();

                //Console.WriteLine("compiling source:\n{0}", LiteralSource);

                System.CodeDom.Compiler.CompilerResults cr = csharp.CompileAssemblyFromSource(cp, LiteralSource);
                if (cr == null)
                {
                    System.Console.WriteLine("Errors compiling!");
                    return;
                }

                foreach (string output in cr.Output)
                    System.Console.WriteLine(output);

                if (cr.Errors.Count != 0)
                {
                    Console.WriteLine("Errors compiling!");
                    return;
                }

                Console.WriteLine("Created self-extracting zip file {0}.", cr.PathToAssembly);

            }
            catch (Exception e1)
            {
                Console.WriteLine("\n****Exception: " + e1);
            }
            return;
        }


        private static void Usage()
        {
            Console.WriteLine("usage:\n  CreateSelfExtractor <Zipfile>");
            Environment.Exit(1);
        }



        public static void Main(string[] args)
        {
            try
            {
                if (args.Length != 1) Usage();

                CreateSelfExtractor me = new CreateSelfExtractor();
                me.Run(args[0]);
            }
            catch (System.Exception exc1)
            {
                Console.WriteLine("Exception while creating the self extracting archive: {0}", exc1.ToString());
            }
        }

    }
}
