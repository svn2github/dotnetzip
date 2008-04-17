// ZipCrypto_Traditional.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2006, 2007, 2008 Microsoft Corporation.  All rights reserved.
//
// Part of an implementation of a zipfile class library. 
// See the file ZipFile.cs for the license and for further information.
//
// This module provides the implementation for "traditional" Zip encryption
//
// Created Tue Apr 15 17:39:56 2008
//
// last saved: 
// Time-stamp: <Wednesday, April 16, 2008  11:26:21  (by dinoch)>
//
// ------------------------------------------------------------------


using System;

namespace Ionic.Utils.Zip
{

    /// <summary>
    /// This implements the "traditional"" or "classic" PKZip encryption, 
    /// which today is considered to be weak. On the other hand it is ubiquitous. 
    /// </summary>
    public class ZipCrypto
    {
        /// <summary>
        /// The default constructor.
        /// </summary>
        public ZipCrypto() { }


        /// <summary> 
        /// From AppNote.txt:
        /// unsigned char decrypt_byte()
        ///     local unsigned short temp
        ///     temp :=- Key(2) | 2
        ///     decrypt_byte := (temp * (temp ^ 1)) bitshift-right 8
        /// end decrypt_byte
        /// </summary>		
        private byte MagicByte
        {
            get
            {
                UInt16 t = (UInt16)((UInt16)(_Keys[2] & 0xFFFF) | 2);
                return (byte)((t * (t ^ 1)) >> 8);
            }
        }



        /// <summary> 
        /// From AppNote.txt:
        /// loop for i from 0 to 11
        ///     C := buffer(i) ^ decrypt_byte()
        ///     update_keys(C)
        ///     buffer(i) := C
        /// end loop
        /// </summary>		
        public byte[] DecryptMessage(byte[] CipherText)
        {
            byte[] PlainText = new byte[CipherText.Length];
            for (int i = 0; i < CipherText.Length; i++)
            {
                byte C = (byte)(CipherText[i] ^ MagicByte);
                UpdateKeys(C);
                PlainText[i] = C;
            }
            return PlainText;
        }

        /// <summary>
        /// This is the converse of DecryptMessage.
        /// </summary>
        public byte[] EncryptMessage(byte[] PlainText)
        {
            byte[] CipherText = new byte[PlainText.Length];
            for (int i = 0; i < PlainText.Length; i++)
            {
                byte C = PlainText[i];
                CipherText[i] = (byte)(PlainText[i] ^ MagicByte);
                UpdateKeys(C);
            }
            return CipherText;
        }


        /// <summary>
        /// This initializes the cipher with the given password. 
        /// See AppNote.txt for details. 
        /// </summary>
        /// <remarks>
        /// Step 1 - Initializing the encryption keys
        /// -----------------------------------------
        /// Start with these keys:        
        /// Key(0) := 305419896 (0x12345678)
        /// Key(1) := 591751049 (0x23456789)
        /// Key(2) := 878082192 (0x34567890)
        /// 
        /// Then, initialize the keys with a password:
        /// 
        /// loop for i from 0 to length(password)-1
        ///     update_keys(password(i))
        /// end loop
        /// 
        /// Where update_keys() is defined as:
        /// 
        /// update_keys(char):
        ///   Key(0) := crc32(key(0),char)
        ///   Key(1) := Key(1) + (Key(0) bitwiseAND 000000ffH)
        ///   Key(1) := Key(1) * 134775813 + 1
        ///   Key(2) := crc32(key(2),key(1) rightshift 24)
        /// end update_keys
        /// 
        /// Where crc32(old_crc,char) is a routine that given a CRC value and a
        /// character, returns an updated CRC value after applying the CRC-32
        /// algorithm described elsewhere in this document.
        ///
        /// <para>
        /// After the keys are initialized, then you can use the cipher to encrypt
        /// the plaintext. 
        /// </para>
        /// <para>
        /// Essentially we encrypt the password with the keys, then discard the 
        /// ciphertext for the password. This initializes the keys for later use.
        /// </para>
        /// </remarks>
        public void InitCipher(string Passphrase)
        {
            byte[] p = Shared.AsciiStringToByteArray(Passphrase);
            for (int i = 0; i < Passphrase.Length; i++)
                UpdateKeys(p[i]);
        }


        private void UpdateKeys(byte b)
        {
            _Keys[0] = crc32.ComputeCrc32(_Keys[0], b);
            _Keys[1] = _Keys[1] + (byte)_Keys[0];
            _Keys[1] = _Keys[1] * 0x08088405 + 1;
            _Keys[2] = crc32.ComputeCrc32(_Keys[2], (byte)(_Keys[1] >> 24));
        }

        ///// <summary>
        ///// Generate random keys for this crypto effort. This is what you want 
        ///// to do when you encrypt. 
        ///// </summary>
        //public void GenerateRandomKeys()
        //{
        //    var rnd = new System.Random();
        //    _Keys[0] = (uint)rnd.Next();
        //    _Keys[1] = (uint)rnd.Next();
        //    _Keys[2] = (uint)rnd.Next();
        //}

        ///// <summary>
        ///// The byte array representing the seed keys used.
        ///// Get this after calling InitCipher.  The 12 bytes represents
        ///// what the zip spec calls the "EncryptionHeader".
        ///// </summary>
        //public byte[] KeyHeader
        //{
        //    get
        //    {
        //        byte[] result = new byte[12];
        //        result[0] = (byte)(_Keys[0] & 0xff);
        //        result[1] = (byte)((_Keys[0] >> 8) & 0xff);
        //        result[2] = (byte)((_Keys[0] >> 16) & 0xff);
        //        result[3] = (byte)((_Keys[0] >> 24) & 0xff);
        //        result[4] = (byte)(_Keys[1] & 0xff);
        //        result[5] = (byte)((_Keys[1] >> 8) & 0xff);
        //        result[6] = (byte)((_Keys[1] >> 16) & 0xff);
        //        result[7] = (byte)((_Keys[1] >> 24) & 0xff);
        //        result[8] = (byte)(_Keys[2] & 0xff);
        //        result[9] = (byte)((_Keys[2] >> 8) & 0xff);
        //        result[10] = (byte)((_Keys[2] >> 16) & 0xff);
        //        result[11] = (byte)((_Keys[2] >> 24) & 0xff);
        //        return result;
        //    }
        //}


        // private fields for the crypto stuff:
        private UInt32[] _Keys = { 0x12345678, 0x23456789, 0x34567890 };
        private CRC32 crc32 = new CRC32();

    }
}
