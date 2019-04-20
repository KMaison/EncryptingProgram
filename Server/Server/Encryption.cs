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
        byte[] IV;
        byte[] Key;
        Aes aesAlg;
        ICryptoTransform encryptor;
        CipherMode aesType;

        public void Initialize(out byte[] genKey, out byte[] genIV, CipherMode aesType)
        {
            aesAlg = Aes.Create();

            Key = GetKey();
            aesAlg.Key = Key;
            aesAlg.Padding = PaddingMode.PKCS7;
            aesAlg.Mode = aesType;
            aesAlg.GenerateIV();
            IV = aesAlg.IV;

            genKey = Key;
            genIV = IV;

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
