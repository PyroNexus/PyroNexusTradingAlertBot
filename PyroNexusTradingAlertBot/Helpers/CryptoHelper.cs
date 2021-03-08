using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace PyroNexusTradingAlertBot.Helpers
{
    public class CryptoHelper
    {
        public class KeyIV
        {
            public byte[] Key;
            public byte[] IV;
        }

        private readonly byte[] Key;
        private readonly byte[] IV;

        private static string FromEnvVar(string envVarName) => Environment.GetEnvironmentVariable(envVarName);

        public CryptoHelper()
            : this(FromEnvVar("PyroNexusTradingAlertBotConfigKey"), FromEnvVar("PyronexusTradingAlertBotConfigIV"))
        { }

        public CryptoHelper(string key, string iv)
            : this(new KeyIV() { Key = Convert.FromBase64String(key), IV = Convert.FromBase64String(iv) })
        { }

        public CryptoHelper(KeyIV keyIV)
        {
            Key = keyIV.Key;
            IV = keyIV.IV;
        }

        public static KeyIV NewKeyIV()
        {
            using Aes aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();
            return new KeyIV()
            {
                Key = aes.Key,
                IV = aes.IV
            };
        }

        public string EncryptString(string plainText)
        {
            byte[] array;

            using Aes aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;

            using MemoryStream memoryStream = new MemoryStream();
            using CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
            {
                streamWriter.Write(plainText);
            }
            array = memoryStream.ToArray();

            return Convert.ToBase64String(array);
        }

        public string DecryptString(string cipherText)
        {
            byte[] buffer = Convert.FromBase64String(cipherText);

            using Aes aes = Aes.Create();

            aes.Key = Key;
            aes.IV = IV;

            using MemoryStream memoryStream = new MemoryStream(buffer);
            using CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader streamReader = new StreamReader(cryptoStream);
            return streamReader.ReadToEnd();
        }
    }
}
