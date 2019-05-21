using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Client
{
    class Encryption
    {
        public byte[] IV;
        public byte[] Key;
        Aes aesAlg;
        ICryptoTransform encryptor;

        public void Initialize(out byte[] genKey, out byte[] genIV)
        {
            aesAlg = Aes.Create();

            Key = GetKey();
            aesAlg.Key = Key;
            aesAlg.Padding = PaddingMode.Zeros;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.GenerateIV();
            IV = aesAlg.IV;

            genKey = Key;
            genIV = IV;

            encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        }

        public void Initialize(byte[] ParKey, byte[] ParIV)
        {
            aesAlg = Aes.Create();

            aesAlg.Key = ParKey;
            aesAlg.Padding = PaddingMode.Zeros;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.IV = ParIV;

            encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        }

        private byte[] GetKey()
        {
            var key = new byte[32];
            using (var random = new RNGCryptoServiceProvider())
            {
                random.GetNonZeroBytes(key);
            }
            return key;
        }

        public byte[] Encrypt(byte[] input)
        {
            using (var msEncrypt = new MemoryStream())
            {
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    csEncrypt.Write(input, 0, input.Length);
                }
                return msEncrypt.ToArray();
            }
            
        }
    }
}
