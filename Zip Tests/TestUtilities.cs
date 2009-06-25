// TestUtilities.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa and Microsoft Corporation.  
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
// Time-stamp: <2009-June-25 12:08:21>
//
// ------------------------------------------------------------------
//
// This module defines some utility classes used by the unit tests for
// DotNetZip.
//
// ------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using Ionic.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ionic.Zip.Tests.Utilities
{
    class TestUtilities
    {
        static System.Random _rnd;

        static TestUtilities()
        {
            _rnd = new System.Random();
            LoremIpsumWords = LoremIpsum.Split(" ".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);
        }

        #region Test Init and Cleanup

        internal static void Initialize(ref string CurrentDir, ref string TopLevelDir)
        {
            CurrentDir = Directory.GetCurrentDirectory();
            Assert.AreNotEqual<string>(Path.GetFileName(CurrentDir), "Temp", "at startup");
            TopLevelDir = TestUtilities.GenerateUniquePathname("tmp");
            Directory.CreateDirectory(TopLevelDir);

            Directory.SetCurrentDirectory(Path.GetDirectoryName(TopLevelDir));
        }

        internal static void Cleanup(string CurrentDir, List<String> FilesToRemove)
        {
            Assert.AreNotEqual<string>(Path.GetFileName(CurrentDir), "Temp", "at finish");
            Directory.SetCurrentDirectory(CurrentDir);
            IOException GotException = null;
            int Tries = 0;
            do
            {
                try
                {
                    GotException = null;
                    foreach (string filename in FilesToRemove)
                    {
                        if (Directory.Exists(filename))
                        {
                            // turn off any ReadOnly attributes
                            ClearReadOnly(filename);
                            Directory.Delete(filename, true);
                        }
                        if (File.Exists(filename))
                        {
                            File.Delete(filename);
                        }
                    }
                    Tries++;
                }
                catch (IOException ioexc)
                {
                    GotException = ioexc;
                    // use an backoff interval before retry
                    System.Threading.Thread.Sleep(200 * Tries);
                }
            } while ((GotException != null) && (Tries < 4));
            if (GotException != null) throw GotException;
        }

        private static void ClearReadOnly(string dirname)
        {
            foreach (var d in Directory.GetDirectories(dirname))
            {
                ClearReadOnly(d); // recurse
            }
            foreach (var f in Directory.GetFiles(dirname))
            {
                // clear ReadOnly and System attributes
                var a = File.GetAttributes(f);
                if ((a & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    a ^= FileAttributes.ReadOnly;
                    File.SetAttributes(f, a);
                }
                if ((a & FileAttributes.System) == FileAttributes.System)
                {
                    a ^= FileAttributes.System;
                    File.SetAttributes(f, a);
                }
            }
        }



        private static bool _pb2Set;
        private static bool _pb1Set;
        private static Ionic.CopyData.Transceiver _txrx;
        private static int _sizeBase;
        private static int _sizeRandom;
        
        private static void setup_SaveProgress(object sender, SaveProgressEventArgs e)
        {
            string msg; 
            switch (e.EventType)
            {
                case ZipProgressEventType.Saving_Started:
                    _txrx.Send("status saving started...");
                    _pb1Set = false;
                    //_txrx.Send(String.Format("pb1 max {0}", e.EntriesTotal));
                    //_txrx.Send("pb2 max 1");
                    break;

                case ZipProgressEventType.Saving_BeforeWriteEntry:
                    _txrx.Send(String.Format("status Compressing {0}", e.CurrentEntry.FileName));
                    if (!_pb1Set)
                    {
                        _txrx.Send(String.Format("pb 1 max {0}", e.EntriesTotal));
                        _pb1Set = true;
                    }
                    _pb2Set = false;

                    if (e.CurrentEntry.Source == ZipEntrySource.Stream &&
                        e.CurrentEntry.InputStream == Stream.Null)
                    {
                        System.IO.Stream s = new RandomTextInputStream(_sizeBase + _rnd.Next(_sizeRandom));
                        e.CurrentEntry.InputStream = s;
                    }
                    
                    break;
                    
                case ZipProgressEventType.Saving_EntryBytesRead:
                    if (!_pb2Set)
                    {
                        _txrx.Send(String.Format("pb 2 max {0}", e.TotalBytesToTransfer));
                        _pb2Set = true;
                    }
                    _txrx.Send(String.Format("status Saving {0} :: [{2}/{3}] ({1:N0}%)",
                                             e.CurrentEntry.FileName,
                                             ((double)e.BytesTransferred) / (0.01 * e.TotalBytesToTransfer),
                                             e.BytesTransferred, e.TotalBytesToTransfer));
                    msg = String.Format("pb 2 value {0}", e.BytesTransferred);
                    _txrx.Send(msg);
                    break;
                    
            case ZipProgressEventType.Saving_AfterWriteEntry:
                _txrx.Send("test Zip64 Test Setup"); // just in case it was missed
                _txrx.Send("pb 1 step");
                break;
                    
            case ZipProgressEventType.Saving_Completed:
                _txrx.Send("status Save completed");
                _pb1Set = false;
                _pb2Set = false;
                _txrx.Send("pb 1 max 1");
                _txrx.Send("pb 1 value 1");
                break;
            }
        }



        internal static string CreateHugeZipfile()
        {
            string HugeZipFile = TestUtilities.GenerateUniquePathname("tmp.zip");
            string TempDir = TestUtilities.GenerateUniquePathname("Zip64Tests.tmp");
            string CurrentDir = Directory.GetCurrentDirectory();
            
            Directory.CreateDirectory(TempDir);
            Directory.SetCurrentDirectory(Path.GetDirectoryName(TempDir));


            // create a huge ZIP64 archive with a true 64-bit offset.
            string testBin = TestUtilities.GetTestBinDir(CurrentDir);
            //string testBin = CurrentDir;
            string progressMonitorTool = Path.Combine(testBin, "Resources\\UnitTestProgressMonitor.exe");
            string requiredDll = Path.Combine(testBin, "Resources\\Ionic.CopyData.dll");
            
            Assert.IsTrue(File.Exists(progressMonitorTool), "progress monitor tool does not exist ({0})",  progressMonitorTool);
            Assert.IsTrue(File.Exists(requiredDll), "required DLL does not exist ({0})",  requiredDll);

            string progressChannel = "Zip64_Setup";
            // start the progress monitor
            string ignored;
            Exec_NoContext(progressMonitorTool, String.Format("-channel {0}", progressChannel), false, out ignored);

            // System.Reflection.Assembly.Load(requiredDll);

            System.Threading.Thread.Sleep(1000);
            _txrx = new Ionic.CopyData.Transceiver();
            _txrx.Channel = progressChannel;
            _txrx.Send("test Zip64 Test Setup");
            _txrx.Send("bars 3");
            System.Threading.Thread.Sleep(120);
            _txrx.Send("status Creating files");
            _txrx.Send(String.Format("pb 0 max {0}", 3));
            
            string ZipFileToCreate = Path.Combine(TempDir, "Zip64Data.zip");

            Directory.SetCurrentDirectory(TempDir);

            // create a directory with some files in it, to zip
            string dirToZip = "dir";
            Directory.CreateDirectory(dirToZip);

            // create a few files in that directory
            int numFilesToAdd = _rnd.Next(3) + 6;
            
            _sizeBase   = 0x20000000;
            _sizeRandom = 0x01000000;

 #if SKIP_THIS
     
     // Rather than create these large files with random content and
     // then zip them, we'll add entries from streams, and provide the
     // streams just-in-time.  This means no write for the original
     // file, nor read during zip saving, which avoids a ton of I/O, and
     // is faster.
     
            _txrx.Send(String.Format("pb 1 max {0}", numFilesToAdd));
            int baseSize = _rnd.Next(0x1f0000ff) + 80000;
            bool firstFileDone = false;
            string fileName = "";
            string firstBinary = null;
            string firstText = null;
            long fileSize = 0;
            
            Action<Int64> progressUpdate = (x) =>
                {
                    _txrx.Send(String.Format("pb 2 value {0}", x));
                    _txrx.Send(String.Format("status Creating {0}, [{1}/{2}] ({3:N0}%)", fileName, x, fileSize, ((double)x)/ (0.01 * fileSize) ));
                };

            // It takes a long time to create a large file. And we need
            // a bunch of them.  
            
            for (int i = 0; i < numFilesToAdd; i++)
            {
                int n = _rnd.Next(2);
                fileName = string.Format("Pippo{0}.{1}", i, (n==0) ? "bin" : "txt" );
                    if (i != 0)
                    {
                        int x = _rnd.Next(6);
                        if (x != 0)
                        {
                            string folderName = string.Format("folder{0}", x);
                            fileName = Path.Combine(folderName, fileName);
                            if (!Directory.Exists(Path.Combine(dirToZip, folderName)))
                                Directory.CreateDirectory(Path.Combine(dirToZip, folderName));
                        }
                    }
                fileName = Path.Combine(dirToZip, fileName);
                // first file is 2x larger
                fileSize = (firstFileDone) ? (baseSize + _rnd.Next(0x880000)) : (2*baseSize);
                _txrx.Send(String.Format("pb 2 max {0}", fileSize));
                if (n==0) 
                    TestUtilities.CreateAndFillFileBinary(fileName, fileSize, progressUpdate);
                else
                    TestUtilities.CreateAndFillFileText(fileName, fileSize, progressUpdate);
                firstFileDone = true;
                _txrx.Send("pb 1 step");
            }
#endif
            
            _txrx.Send("pb 0 step");
            
            // Add links to a few very large files into the same directory.
            // We do this because creating such large files will take a very very long time.

            _txrx.Send("status Creating links");
            var namesOfLargeFiles = new String[]
                {
                    "c:\\dinoch\\PST\\archive.pst",
                    "c:\\dinoch\\PST\\archive1.pst", 
                    "c:\\dinoch\\PST\\Lists.pst",
                    "c:\\dinoch\\PST\\OldStuff.pst",
                    "c:\\dinoch\\PST\\Personal1.pst",
                };
            
            string subdir = Path.Combine(dirToZip, "largelinks");
            Directory.CreateDirectory(subdir);
            Directory.SetCurrentDirectory(subdir);
            var w = System.Environment.GetEnvironmentVariable("Windir");
            Assert.IsTrue(Directory.Exists(w), "%windir% does not exist ({0})", w);
            var fsutil = Path.Combine(Path.Combine(w, "system32"), "fsutil.exe");
            Assert.IsTrue(File.Exists(fsutil), "fsutil.exe does not exist ({0})", fsutil);
            foreach (var f in namesOfLargeFiles)
            {
                Assert.IsTrue(File.Exists(f));
                string cmd = String.Format("hardlink create {0} {1}", Path.GetFileName(f), f);
                _txrx.Send("status " + cmd);
                Exec_NoContext(fsutil, cmd, out ignored);
                cmd = String.Format("hardlink create {0}-Copy1{1} {2}",
                                    Path.GetFileNameWithoutExtension(f),Path.GetExtension(f), f);
                Exec_NoContext(fsutil, cmd, out ignored);
            }

            Directory.SetCurrentDirectory(TempDir); // again

            // pb1 and pb2 will be set in the SaveProgress handler
            _txrx.Send("pb 0 step");
            _txrx.Send("status Saving the zip...");
            _pb1Set= false;
            using (ZipFile zip = new ZipFile())
            {
                zip.SaveProgress += setup_SaveProgress;
                zip.UpdateDirectory(dirToZip, "");
                zip.UseZip64WhenSaving = Zip64Option.Always;
                // use large buffer to speed up save for large files:
                zip.BufferSize = 1024 * 756;
                zip.CodecBufferSize = 1024 * 128;
                for (int i = 0; i < numFilesToAdd; i++)
                    zip.AddEntry("random"+i+".txt", "", Stream.Null);
                
                zip.Save(ZipFileToCreate);
            }

            _txrx.Send("pb 0 step");
            System.Threading.Thread.Sleep(120);
            
            Directory.Delete(dirToZip, true);
            _txrx.Send("pb 0 step");
            System.Threading.Thread.Sleep(120);

            _txrx.Send("stop");

            // restore the cwd:
            Directory.SetCurrentDirectory(CurrentDir);

            return ZipFileToCreate;
        }
        
        #endregion


        #region Helper methods

        internal static string TrimVolumeAndSwapSlashes(string pathName)
        {
            //return (((pathname[1] == ':') && (pathname[2] == '\\')) ? pathname.Substring(3) : pathname)
            //    .Replace('\\', '/');
            if (String.IsNullOrEmpty(pathName)) return pathName;
            if (pathName.Length < 2) return pathName.Replace('\\', '/');
            return (((pathName[1] == ':') && (pathName[2] == '\\')) ? pathName.Substring(3) : pathName)
                .Replace('\\', '/');
        }

        internal static DateTime RoundToEvenSecond(DateTime source)
        {
            // round to nearest second:
            if ((source.Second % 2) == 1)
                source += new TimeSpan(0, 0, 1);

            DateTime dtRounded = new DateTime(source.Year, source.Month, source.Day, source.Hour, source.Minute, source.Second);
            //if (source.Millisecond >= 500) dtRounded = dtRounded.AddSeconds(1);
            return dtRounded;
        }


        internal static void CreateAndFillFileText(string filename, Int64 size)
        {
            CreateAndFillFileText(filename, size, null);
        }

        
        internal static void CreateAndFillFileText(string filename, Int64 size, System.Action<Int64> update)
        {
            Int64 bytesRemaining = size;

            if (size > 128 * 1024)
            {
                RandomTextGenerator rtg = new RandomTextGenerator();
                
                // fill the file with text data, selecting 5 paragraphs at a time
                using (StreamWriter sw = File.CreateText(filename))
                {
                    do
                    {
                        string generatedText = rtg.Generate(32 * 1024);
                        sw.Write(generatedText);
                        sw.Write("\n\n");
                        bytesRemaining -= (generatedText.Length + 2);
                        
                        if (update != null)
                            update(size - bytesRemaining);

                    } while (bytesRemaining > 0);
                }
            }
            else
            {
                
            // fill the file with text data, selecting one word at a time
            using (StreamWriter sw = File.CreateText(filename))
            {
                do
                {
                    // pick a word at random
                    string selectedWord = LoremIpsumWords[_rnd.Next(LoremIpsumWords.Length)];
                    if (bytesRemaining < selectedWord.Length + 1)
                    {
                        sw.Write(selectedWord.Substring(0, (int)bytesRemaining));
                        bytesRemaining = 0;
                    }
                    else
                    {
                        sw.Write(selectedWord);
                        sw.Write(" ");
                        bytesRemaining -= (selectedWord.Length + 1);
                    }
                    if (update != null)
                        update(size - bytesRemaining);

                } while (bytesRemaining > 0);
                sw.Close();
            }
            }
        }

        internal static void CreateAndFillFileText(string Filename, string Line, Int64 size)
        {
            CreateAndFillFileText(Filename, Line, size, null);
        }

        
        internal static void CreateAndFillFileText(string Filename, string Line, Int64 size, System.Action<Int64> update)
        {
            Int64 bytesRemaining = size;
            // fill the file by repeatedly writing out the same line
            using (StreamWriter sw = File.CreateText(Filename))
            {
                do
                {
                    if (bytesRemaining < Line.Length + 2)
                    {
                        if (bytesRemaining == 1)
                            sw.Write(" ");
                        else if (bytesRemaining == 1)
                            sw.WriteLine();
                        else
                            sw.WriteLine(Line.Substring(0, (int)bytesRemaining - 2));
                        bytesRemaining = 0;
                    }
                    else
                    {
                        sw.WriteLine(Line);
                        bytesRemaining -= (Line.Length + 2);
                    }
                    if (update != null)
                        update(size - bytesRemaining);
                } while (bytesRemaining > 0);
                sw.Close();
            }
        }

        internal static void CreateAndFillFileBinary(string Filename, Int64 size)
        {
            _CreateAndFillBinary(Filename, size, false, null);
        }
        
        internal static void CreateAndFillFileBinary(string Filename, Int64 size, System.Action<Int64> update)
        {
            _CreateAndFillBinary(Filename, size, false, update);
        }
        
        internal static void CreateAndFillFileBinaryZeroes(string Filename, Int64 size, System.Action<Int64> update)
        {
            _CreateAndFillBinary(Filename, size, true, update);
        }

        delegate void ProgressUpdate(System.Int64 bytesXferred);

        private static void _CreateAndFillBinary(string filename, Int64 size, bool zeroes, System.Action<Int64> update)
        {
            Int64 bytesRemaining = size;
            // fill with binary data
            int sz = 65536 * 8;
            if (size < sz) sz = (int)size;
            byte[] buffer = new byte[sz];
            using (Stream fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                while (bytesRemaining > 0)
                {
                    int sizeOfChunkToWrite = (bytesRemaining > buffer.Length) ? buffer.Length : (int)bytesRemaining;
                    if (!zeroes) _rnd.NextBytes(buffer);
                    fileStream.Write(buffer, 0, sizeOfChunkToWrite);
                    bytesRemaining -= sizeOfChunkToWrite;
                    if (update != null)
                        update(size - bytesRemaining);
                }
                fileStream.Close();
            }
        }


        internal static void CreateAndFillFile(string filename, Int64 size)
        {
            //Assert.IsTrue(size > 0, "File size should be greater than zero.");
            if (size == 0)
                File.Create(filename);
            else if (_rnd.Next(2) == 0)
                CreateAndFillFileText(filename, size);
            else
                CreateAndFillFileBinary(filename, size);
        }

        internal static string CreateUniqueFile(string extension, string ContainingDirectory)
        {
            //string nameOfFileToCreate = GenerateUniquePathname(extension, ContainingDirectory);
            string nameOfFileToCreate = Path.Combine(ContainingDirectory, String.Format("{0}.{1}", Path.GetRandomFileName(), extension));
            var fs = File.Create(nameOfFileToCreate);
            fs.Close();
            return nameOfFileToCreate;
        }

        internal static string CreateUniqueFile(string extension)
        {
            return CreateUniqueFile(extension, null);
        }

        internal static string CreateUniqueFile(string extension, Int64 size)
        {
            return CreateUniqueFile(extension, null, size);
        }

        internal static string CreateUniqueFile(string extension, string ContainingDirectory, Int64 size)
        {
            //string fileToCreate = GenerateUniquePathname(extension, ContainingDirectory);
            string nameOfFileToCreate = Path.Combine(ContainingDirectory, String.Format("{0}.{1}", Path.GetRandomFileName(), extension));
            CreateAndFillFile(nameOfFileToCreate, size);
            return nameOfFileToCreate;
        }

        static System.Reflection.Assembly _a = null;
        private static System.Reflection.Assembly _MyAssembly
        {
            get
            {
                if (_a == null)
                {
                    _a = System.Reflection.Assembly.GetExecutingAssembly();
                }
                return _a;
            }
        }

        internal static string GenerateUniquePathname(string extension)
        {
            return GenerateUniquePathname(extension, null);
        }

        internal static string GenerateUniquePathname(string extension, string ContainingDirectory)
        {
            string candidate = null;
            String AppName = _MyAssembly.GetName().Name;

            string parentDir = (ContainingDirectory == null) ? System.Environment.GetEnvironmentVariable("TEMP") :
                ContainingDirectory;
            if (parentDir == null) return null;

            int index = 0;
            do
            {
                index++;
                string Name = String.Format("{0}-{1}-{2}.{3}",
                                            AppName, System.DateTime.Now.ToString("yyyyMMMdd-HHmmss"), index, extension);
                candidate = Path.Combine(parentDir, Name);
            } while (File.Exists(candidate));

            // this file/path does not exist.  It can now be created, as file or directory. 
            return candidate;
        }

        internal static int CountEntries(string zipfile)
        {
            int entries = 0;
            using (ZipFile zip = ZipFile.Read(zipfile))
            {
                foreach (ZipEntry e in zip)
                    if (!e.IsDirectory) entries++;
            }
            return entries;
        }


        internal static string CheckSumToString(byte[] checksum)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (byte b in checksum)
                sb.Append(b.ToString("x2").ToLower());
            return sb.ToString();
        }

        internal static byte[] ComputeChecksum(string filename)
        {
            byte[] hash = null;
            var _md5 = System.Security.Cryptography.MD5.Create();

            using (FileStream fs = File.Open(filename, FileMode.Open))
            {
                hash = _md5.ComputeHash(fs);
            }
            return hash;
        }

        private static char GetOneRandomPasswordChar()
        {
            const int range = 126 - 33;
            const int start = 33;
            return (char)(_rnd.Next(range) + start);
        }

        internal static string GenerateRandomPassword()
        {
            int length = _rnd.Next(22) + 12;
            return GenerateRandomPassword(length);
        }

        internal static string GenerateRandomPassword(int length)
        {
            char[] a = new char[length];
            for (int i = 0; i < length; i++)
            {
                a[i] = GetOneRandomPasswordChar();
            }

            string result = new System.String(a);
            return result;
        }


        public static string GenerateRandomAsciiString()
        {
            return GenerateRandomAsciiString(_rnd.Next(14));
        }

        public static string GenerateRandomName()
        {
            return
                GenerateRandomUpperString(1) +
                GenerateRandomLowerString(_rnd.Next(9) + 3);
        }

        public static string GenerateRandomName(int length)
        {
            return
                GenerateRandomUpperString(1) +
                GenerateRandomLowerString(length - 1);
        }

        public static string GenerateRandomAsciiString(int length)
        {
            return GenerateRandomAsciiStringImpl(length, 0);
        }

        public static string GenerateRandomUpperString()
        {
            return GenerateRandomAsciiStringImpl(_rnd.Next(10) + 3, 65);
        }

        public static string GenerateRandomUpperString(int length)
        {
            return GenerateRandomAsciiStringImpl(length, 65);
        }

        public static string GenerateRandomLowerString(int length)
        {
            return GenerateRandomAsciiStringImpl(length, 97);
        }

        public static string GenerateRandomLowerString()
        {
            return GenerateRandomAsciiStringImpl(_rnd.Next(9) + 4, 97);
        }

        private static string GenerateRandomAsciiStringImpl(int length, int delta)
        {
            bool WantRandomized = (delta == 0);

            string result = "";
            char[] a = new char[length];

            for (int i = 0; i < length; i++)
            {
                if (WantRandomized)
                    delta = (_rnd.Next(2) == 0) ? 65 : 97;
                a[i] = GetOneRandomAsciiChar(delta);
            }

            result = new System.String(a);
            return result;
        }



        private static char GetOneRandomAsciiChar(int delta)
        {
            // delta == 65 means uppercase
            // delta == 97 means lowercase
            return (char)(_rnd.Next(26) + delta);
        }




        internal static int GenerateFilesOneLevelDeep(TestContext tc,
                                                      string TestName,
                                                      string DirToZip,
                                                      Action<Int16, Int32> update,
                                                      out int subdirCount)
        {
            int[] settings = { 7, 6, 17, 23, 4000, 4000 }; // to randomly set dircount, filecount, and filesize
            return GenerateFilesOneLevelDeep(tc, TestName, DirToZip, settings, update, out subdirCount);
        }


        internal static int GenerateFilesOneLevelDeep(TestContext tc,
                                                      string TestName,
                                                      string DirToZip,
                                                      int[] settings,
                                                      Action<Int16, Int32> update,
                                                      out int subdirCount)
        {
            int entriesAdded = 0;
            String filename = null;

            subdirCount = _rnd.Next(settings[0]) + settings[1];
            if (update != null)
                update(0, subdirCount);
            tc.WriteLine("{0}: Creating {1} subdirs.", TestName, subdirCount);
            for (int i = 0; i < subdirCount; i++)
            {
                string SubDir = Path.Combine(DirToZip, String.Format("dir{0:D4}", i));
                Directory.CreateDirectory(SubDir);

                int filecount = _rnd.Next(settings[2]) + settings[3];
                if (update != null)
                    update(1, filecount);
                tc.WriteLine("{0}: Subdir {1}, Creating {2} files.", TestName, i, filecount);
                for (int j = 0; j < filecount; j++)
                {
                    filename = String.Format("file{0:D4}.x", j);
                    TestUtilities.CreateAndFillFile(Path.Combine(SubDir, filename),
                                                    _rnd.Next(settings[4]) + settings[5]);
                    entriesAdded++;
                    if (update != null)
                        update(3, j + 1);
                }
                if (update != null)
                    update(2, i + 1);
            }
            if (update != null)
                update(4, entriesAdded);
            return entriesAdded;
        }




        internal static string[] GenerateFilesFlat(string Subdir)
        {
            if (!Directory.Exists(Subdir))
                Directory.CreateDirectory(Subdir);

            int NumFilesToCreate = _rnd.Next(23) + 14;
            string[] FilesToZip = new string[NumFilesToCreate];
            for (int i = 0; i < NumFilesToCreate; i++)
            {
                FilesToZip[i] = Path.Combine(Subdir, String.Format("file{0:D3}.txt", i));
                TestUtilities.CreateAndFillFileText(FilesToZip[i], _rnd.Next(34000) + 5000);
            }
            return FilesToZip;
        }


        internal static string GetTestBinDir(string startingPoint)
        {
            var location = startingPoint;
            for (int i = 0; i < 3; i++)
                location = Path.GetDirectoryName(location);

            var testDir = "Zip Tests\\bin\\Debug";
            location = Path.Combine(location, testDir);
            return location;
        }


        internal static int Exec_NoContext(string program, string args, out string output)
        {
            return Exec_NoContext(program, args, true, out output);
        }



        

        internal static int Exec_NoContext(string program, string args, bool waitForExit, out string output)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process
                {
                    StartInfo =
                    {
                        FileName = program,
                        Arguments = args,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                        UseShellExecute = false,
                    }
                    
                };
            
            if (waitForExit)
            {
                StringBuilder sb = new StringBuilder();
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                // must read at least one of the stderr or stdout asynchronously,
                // to avoid deadlock
                Action<Object,System.Diagnostics.DataReceivedEventArgs> stdErrorRead = (o,e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                        sb.Append(e.Data);
                };
                
                p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(stdErrorRead);
                p.Start();
                p.BeginErrorReadLine();
                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                if (sb.Length > 0)
                    output += sb.ToString();
                return p.ExitCode;
            }
            else
            {
                p.Start();
            }
            output = "";
            return 0;
        }


        #endregion

        internal static string LoremIpsum =
            "Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Integer " +
            "vulputate, nibh non rhoncus euismod, erat odio pellentesque lacus, sit " +
            "amet convallis mi augue et odio. Phasellus cursus urna facilisis " +
            "quam. Suspendisse nec metus et sapien scelerisque euismod. Nullam " +
            "molestie sem quis nisl. Fusce pellentesque, ante sed semper egestas, sem " +
            "nulla vestibulum nulla, quis sollicitudin leo lorem elementum " +
            "wisi. Aliquam vestibulum nonummy orci. Sed in dolor sed enim ullamcorper " +
            "accumsan. Duis vel nibh. Class aptent taciti sociosqu ad litora torquent " +
            "per conubia nostra, per inceptos hymenaeos. Sed faucibus, enim sit amet " +
            "venenatis laoreet, nisl elit posuere est, ut sollicitudin tortor velit " +
            "ut ipsum. Aliquam erat volutpat. Phasellus tincidunt vehicula " +
            "eros. Curabitur vitae erat. " +
            "\n " +
            "Quisque pharetra lacus quis sapien. Duis id est non wisi sagittis " +
            "adipiscing. Nulla facilisi. Etiam quam erat, lobortis eu, facilisis nec, " +
            "blandit hendrerit, metus. Fusce hendrerit. Nunc magna libero, " +
            "sollicitudin non, vulputate non, ornare id, nulla.  Suspendisse " +
            "potenti. Nullam in mauris. Curabitur et nisl vel purus vehicula " +
            "sodales. Class aptent taciti sociosqu ad litora torquent per conubia " +
            "nostra, per inceptos hymenaeos. Cum sociis natoque penatibus et magnis " +
            "dis parturient montes, nascetur ridiculus mus. Donec semper, arcu nec " +
            "dignissim porta, eros odio tempus pede, et laoreet nibh arcu et " +
            "nisl. Morbi pellentesque eleifend ante. Morbi dictum lorem non " +
            "ante. Nullam et augue sit amet sapien varius mollis. " +
            "\n " +
            "Nulla erat lorem, fringilla eget, ultrices nec, dictum sed, " +
            "sapien. Aliquam libero ligula, porttitor scelerisque, lobortis nec, " +
            "dignissim eu, elit. Etiam feugiat, dui vitae laoreet faucibus, tellus " +
            "urna molestie purus, sit amet pretium lorem pede in erat.  Ut non libero " +
            "et sapien porttitor eleifend. Vestibulum ante ipsum primis in faucibus " +
            "orci luctus et ultrices posuere cubilia Curae; In at lorem et lacus " +
            "feugiat iaculis. Nunc tempus eros nec arcu tristique egestas. Quisque " +
            "metus arcu, pretium in, suscipit dictum, bibendum sit amet, " +
            "mauris. Aliquam non urna. Suspendisse eget diam. Aliquam erat " +
            "volutpat. In euismod aliquam lorem. Mauris dolor nisl, consectetuer sit " +
            "amet, suscipit sodales, rutrum in, lorem. Nunc nec nisl. Nulla ante " +
            "libero, aliquam porttitor, aliquet at, imperdiet sed, diam. Pellentesque " +
            "tincidunt nisl et ipsum. Suspendisse purus urna, semper quis, laoreet " +
            "in, vestibulum vel, arcu. Nunc elementum eros nec mauris. " +
            "\n " +
            "Vivamus congue pede at quam. Aliquam aliquam leo vel turpis. Ut " +
            "commodo. Integer tincidunt sem a risus. Cras aliquam libero quis " +
            "arcu. Integer posuere. Nulla malesuada, wisi ac elementum sollicitudin, " +
            "libero libero molestie velit, eu faucibus est ante eu libero. Sed " +
            "vestibulum, dolor ac ultricies consectetuer, tellus risus interdum diam, " +
            "a imperdiet nibh eros eget mauris. Donec faucibus volutpat " +
            "augue. Phasellus vitae arcu quis ipsum ultrices fermentum. Vivamus " +
            "ultricies porta ligula. Nullam malesuada. Ut feugiat urna non " +
            "turpis. Vivamus ipsum. Vivamus eleifend condimentum risus. Curabitur " +
            "pede. Maecenas suscipit pretium tortor. Integer pellentesque. " +
            "\n " +
            "Mauris est. Aenean accumsan purus vitae ligula. Lorem ipsum dolor sit " +
            "amet, consectetuer adipiscing elit. Nullam at mauris id turpis placerat " +
            "accumsan. Sed pharetra metus ut ante. Aenean vel urna sit amet ante " +
            "pretium dapibus. Sed nulla. Sed nonummy, lacus a suscipit semper, erat " +
            "wisi convallis mi, et accumsan magna elit laoreet sem. Nam leo est, " +
            "cursus ut, molestie ac, laoreet id, mauris. Suspendisse auctor nibh. " +
            "\n";

        static string[] LoremIpsumWords;


    }

    public interface IExec
    {
        TestContext TestContext
        {
            get;
            set;
        }
    }

    public static class Extensions
    {

        internal static string Exec(this IExec o, string program, string args)
        {
            return Exec(o, program, args, true);
        }
        
        internal static string Exec(this IExec o, string program, string args, bool waitForExit)
        {
            if (args == null)
                throw new ArgumentException("args");

            if (program == null)
                throw new ArgumentException("program");

            // Microsoft.VisualStudio.TestTools.UnitTesting
            o.TestContext.WriteLine("running command: {0} {1}", program, args);

            string output;
            int rc = TestUtilities.Exec_NoContext(program, args, waitForExit, out output);

            if (rc != 0)
                throw new Exception(String.Format("Exception running app {0}: {1}", program, output));

            o.TestContext.WriteLine("output: {0}", output);

            return output;
        }


        public static IEnumerable<string> SplitByWords(this string subject)
        {  
            List<string> tokens = new List<string>();
            Regex regex = new Regex(@"\s+");
            tokens.AddRange(regex.Split(subject));  
  
            return tokens;  
        }

        // Capitalize
        public static string Capitalize(this string subject)
        {
            if (subject.Length < 2) return subject.ToUpper();
            return subject.Substring(0,1).ToUpper() +
                subject.Substring(1);
        }

        // TrimPunctuation
        public static string TrimPunctuation(this string subject)
        {
            while (subject.EndsWith(".") ||
                   subject.EndsWith(",") ||
                   subject.EndsWith(";")||
                   subject.EndsWith("?") ||
                   subject.EndsWith("!"))
                subject = subject.Substring(0,subject.Length-1);
            return subject;
        }
    }


    
    public class RandomTextGenerator
    {
        static string[] uris =  new string[]
            {
                // "Through the Looking Glass", by Lewis Carroll (~181k)
                "http://www.gutenberg.org/files/12/12.txt",

                // Decl of Independence (~16k)
                "http://www.gutenberg.org/files/16780/16780.txt",

                // The Naval War of 1812, by Theodore Roosevelt (968k)
                "http://www.gutenberg.org/dirs/etext05/7trnv10.txt",
            };

        SimpleMarkovChain markov;

        public RandomTextGenerator()
        {
            System.Random rnd = new System.Random();
            string uri = uris[rnd.Next(uris.Length)];
            string seedText = GetPageMarkup(uri);
            markov = new SimpleMarkovChain(seedText);
        }

        
        public string Generate(int length)
        {
            return markov.GenerateText(length);
        }

        
        private static string GetPageMarkup(string uri)
        {
            string pageData = null;
            using (WebClient client = new WebClient())
            {
                pageData = client.DownloadString(uri);
            }
            return pageData;
        }
    }

    
    /// <summary>
    /// Implements a simple Markov chain for text.
    /// </summary>
    ///
    /// <remarks>
    /// Uses a Markov chain starting with some base texts to produce
    /// random natural-ish text. This implementation is based on Pike's
    /// perl implementation, see
    /// http://cm.bell-labs.com/cm/cs/tpop/markov.pl
    /// </remarks>
    public class SimpleMarkovChain
    {
        Dictionary<String, List<String>> table = new Dictionary<String, List<String>>();
        System.Random rnd = new System.Random();
            
        public SimpleMarkovChain(string seed)
        {
            string NEWLINE = "\n";
            string key= NEWLINE;
            var sr = new StringReader(seed);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                foreach (var word in line.SplitByWords())
                {
                    var w = (word=="") ? NEWLINE : word; // newline
                    if (word == "\r") w = NEWLINE;
                        
                    if (!table.ContainsKey(key)) table.Add(key,new List<string>());
                    table[key].Add(w);
                    key = w.ToLower().TrimPunctuation();
                }
            }
            if (!table.ContainsKey(key)) table.Add(key,new List<string>());
            table[key].Add(NEWLINE);
            key = NEWLINE;
        }

            
        internal void Diag()
        {
            Console.WriteLine("There are {0} keys in the table", table.Keys.Count);
            foreach (string s in table.Keys)
            {
                string x= s.Replace("\n", "¿");
                var y = table[s].ToArray();
                Console.WriteLine("  {0}: {1}", x, String.Join(", ", y));
            }
        }

        internal void ShowList(string word)
        {
            string x= word.Replace("\n", "¿");
            if (table.ContainsKey(word))
            {
                var y = table[word].ToArray();
                var z = Array.ConvertAll(y,  x1 => x1.Replace("\n", "¿"));
                Console.WriteLine("  {0}: {1}", x, String.Join(", ", z));
            }
            else
                Console.WriteLine("  {0}: -key not found-", x);
        }

        private List<string> _keywords;
        private List<string> keywords
        {
            get
            {
                if (_keywords== null)
                    _keywords = new List<String>(table.Keys);
                return _keywords;
            }
        }

        /// <summary>
        /// Generates random text with a minimum character length.
        /// </summary>
        ///
        /// <param name="minimumLength">
        /// The minimum length of text, in characters, to produce.
        /// </param>
        public string GenerateText(int minimumLength)
        {
            var chosenStartWord = keywords[rnd.Next(keywords.Count)];
            return _InternalGenerate(chosenStartWord, StopCriterion.NumberOfChars, minimumLength);
        }

        /// <summary>
        /// Generates random text with a minimum character length.
        /// </summary>
        ///
        /// <remarks>
        /// The first sentence will start with the given start word. 
        /// </remarks>
        ///
        /// <param name="minimumLength">
        /// The minimum length of text, in characters, to produce.
        /// </param>
        /// <param name="start">
        /// The word to start with. If this word does not exist in the
        /// seed text, the generation will fail.
        /// </param>
        /// <seealso cref="GenerateText(int)"/>
        /// <seealso cref="GenerateWords(int)"/>
        /// <seealso cref="GenerateWords(string, int)"/>
        public string GenerateText(string start, int minimumLength)
        {
            return _InternalGenerate(start, StopCriterion.NumberOfChars, minimumLength);
        }

        /// <summary>
        /// Generate random text with a minimum number of words.
        /// </summary>
        ///
        /// <remarks>
        /// The first sentence will start with the given start word. 
        /// </remarks>
        ///
        /// <param name="minimumWords">
        /// The minimum number of words of text to produce.
        /// </param>
        /// <param name="start">
        /// The word to start with. If this word does not exist in the
        /// seed text, the generation will fail.
        /// </param>
        /// <seealso cref="GenerateText(int)"/>
        /// <seealso cref="GenerateText(string, int)"/>
        /// <seealso cref="GenerateWords(int)"/>
        public string GenerateWords(string start, int minimumWords)
        {
            return _InternalGenerate(start, StopCriterion.NumberOfWords, minimumWords);
        }


        /// <summary>
        /// Generate random text with a minimum number of words.
        /// </summary>
        ///
        /// <param name="minimumWords">
        /// The minimum number of words of text to produce.
        /// </param>
        /// <seealso cref="GenerateText(int)"/>
        /// <seealso cref="GenerateWords(string, int)"/>
        public string GenerateWords(int minimumWords)
        {
            var chosenStartWord = keywords[rnd.Next(keywords.Count)];
            return _InternalGenerate(chosenStartWord, StopCriterion.NumberOfWords, minimumWords);
        }

        
        private string _InternalGenerate(string start, StopCriterion crit, int limit)
        {
            string w1= start.ToLower();
            StringBuilder sb = new StringBuilder();
            sb.Append(start.Capitalize());
                
            int consecutiveNewLines = 0;
            string word= null;
            string priorWord = null;

            // About the stop criteria:
            // we keep going til we reach the specified number of words or chars, with the added
            // proviso that we have to complete the in-flight sentence when the limit is reached.
            
            for (int i= 0;
                 (crit==StopCriterion.NumberOfWords && i < limit) ||
                     (crit==StopCriterion.NumberOfChars && sb.Length < limit) ||
                     consecutiveNewLines==0 ;
                 i++)
            {
                if (table.ContainsKey(w1))
                {
                    var list= table[w1];
                    int ix = rnd.Next(list.Count);
                    priorWord = word;
                    word = list[ix];
                    if (word != "\n")
                    {
                        // capitalize
                        if (consecutiveNewLines > 0)
                            sb.Append(word.Capitalize());
                        else
                            sb.Append(" ").Append(word);

                        // words that end sentences get a newline
                        if (word.EndsWith("."))
                        {
                            if (consecutiveNewLines==0 || consecutiveNewLines==1)
                                sb.Append("\n");
                            consecutiveNewLines++;
                        }
                        else consecutiveNewLines=0;
                    }
                    w1 = word.ToLower().TrimPunctuation();
                }
            }
            return sb.ToString();
        }


        
        private enum StopCriterion
        {
            NumberOfWords,
            NumberOfChars
        }
        
    }


    
    public class RandomTextInputStream : Stream
    {
        RandomTextGenerator _rtg;
        Int64 _desiredLength;
        Int64 _bytesRead;
        System.Text.Encoding _encoding;

        
        public RandomTextInputStream(Int64 length)
            : this(length, System.Text.Encoding.GetEncoding("ascii") )
        {
        }

        public RandomTextInputStream(Int64 length, System.Text.Encoding encoding)
            : base()
        {
            _desiredLength= length;
            _rtg = new RandomTextGenerator();
            _encoding = encoding;
        }

        public Int64 BytesRead
        {
            get { return _bytesRead; }
        }

        static readonly int maxChunkSize = 1024 * 32;
        public override int Read(byte[] buffer, int offset, int count)
        {
            int remainingBytesToRead = count;
            if (_desiredLength - _bytesRead < remainingBytesToRead)
                remainingBytesToRead = unchecked((int)(_desiredLength - _bytesRead));


            int totalBytesToRead= remainingBytesToRead;
            byte[] src = _encoding.GetBytes(_rtg.Generate(maxChunkSize));
            int nBlocks= 0;
            while (remainingBytesToRead > 0)
            {
                int chunksize = (remainingBytesToRead > maxChunkSize) ? maxChunkSize : remainingBytesToRead;
                Array.Copy(src, 0, buffer, offset, chunksize);
                remainingBytesToRead -= chunksize;
                offset += chunksize;
                nBlocks++;
                if (nBlocks % 32 == 0)
                    src = _encoding.GetBytes(_rtg.Generate(maxChunkSize));
            }
            _bytesRead += totalBytesToRead;

            return totalBytesToRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }


        public override bool CanRead
        {
            get { return true; }
        }
        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _desiredLength; }  
        }

        public override long Position
        {
            get { return _desiredLength - _bytesRead; }
            set
            {
                Console.WriteLine("setting position to {0}", value);
                throw new NotImplementedException();
            }
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            if (value < _bytesRead)
                throw new NotImplementedException();
            _desiredLength = value;
        }
        
        public override void Flush()
        {
        }
    }


    
}
