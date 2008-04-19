using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ionic.Utils.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Library.Tests
{
    class TestUtilities
    {

        static System.Random _rnd;

        static TestUtilities()
        {
            _rnd = new System.Random();
        }

        #region Test Init and Cleanup

        internal static void Initialize(ref string CurrentDir, ref string TopLevelDir)
        {
            CurrentDir = System.IO.Directory.GetCurrentDirectory();
            TopLevelDir = TestUtilities.GenerateUniqueFilename("tmp");
            System.IO.Directory.CreateDirectory(TopLevelDir);

            System.IO.Directory.SetCurrentDirectory(System.IO.Path.GetDirectoryName(TopLevelDir));
        }


        internal static void Cleanup(string CurrentDir, List<String> FilesToRemove)
        {
            System.IO.Directory.SetCurrentDirectory(CurrentDir);
            System.IO.IOException GotException = null;
            int Tries = 0;
            do
            {
                try
                {
                    GotException = null;
                    foreach (string filename in FilesToRemove)
                    {
                        if (System.IO.Directory.Exists(filename))
                            System.IO.Directory.Delete(filename, true);

                        if (System.IO.File.Exists(filename))
                            System.IO.File.Delete(filename);
                    }
                    Tries++;
                }
                catch (System.IO.IOException ioexc)
                {
                    GotException = ioexc;
                    // use an backoff interval before retry
                    System.Threading.Thread.Sleep(200 * Tries);
                }
            } while ((GotException != null) && (Tries < 4));
            if (GotException != null) throw GotException;
        }

        #endregion


        #region Helper methods

        internal static void CreateAndFillFile(string Filename, int size)
        {
            Assert.IsTrue(size > 0, "File size should be greater than zero.");
            int bytesRemaining = size;
            byte[] Buffer = new byte[2000];
            using (System.IO.Stream fileStream = new System.IO.FileStream(Filename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                while (bytesRemaining > 0)
                {
                    int sizeOfChunkToWrite = (bytesRemaining > Buffer.Length) ? Buffer.Length : bytesRemaining;
                    _rnd.NextBytes(Buffer);
                    fileStream.Write(Buffer, 0, sizeOfChunkToWrite);
                    bytesRemaining -= sizeOfChunkToWrite;
                }
            }
        }

        internal static string CreateUniqueFile(string extension, string ContainingDirectory)
        {
            string fileToCreate = GenerateUniqueFilename(extension, ContainingDirectory);
            System.IO.File.Create(fileToCreate);
            return fileToCreate;
        }
        internal static string CreateUniqueFile(string extension)
        {
            return CreateUniqueFile(extension, null);
        }

        internal static string CreateUniqueFile(string extension, int size)
        {
            return CreateUniqueFile(extension, null, size);
        }

        internal static string CreateUniqueFile(string extension, string ContainingDirectory, int size)
        {
            string fileToCreate = GenerateUniqueFilename(extension, ContainingDirectory);
            CreateAndFillFile(fileToCreate, size);
            return fileToCreate;
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

        internal static string GenerateUniqueFilename(string extension)
        {
            return GenerateUniqueFilename(extension, null);
        }
        internal static string GenerateUniqueFilename(string extension, string ContainingDirectory)
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
                candidate = System.IO.Path.Combine(parentDir, Name);
            } while (System.IO.File.Exists(candidate));

            // this file/path does not exist.  It can now be created, as file or directory. 
            return candidate;
        }

        internal static bool CheckZip(string zipfile, int fileCount)
        {
            int entries = 0;
            using (ZipFile zip = ZipFile.Read(zipfile))
            {
                foreach (ZipEntry e in zip)
                    if (!e.IsDirectory) entries++;
            }
            return (entries == fileCount);
        }

        #endregion


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

            using (System.IO.FileStream fs = System.IO.File.Open(filename, System.IO.FileMode.Open))
            {
                hash = _md5.ComputeHash(fs);
            }
            return hash;
        }
    }
}
