using System;
using System.Text;

namespace DialogSystem
{
    /// <summary>
    /// Utility class for obfuscating dialogue data to prevent datamining of story text.
    /// Uses a symmetric XOR cipher.
    /// </summary>
    public static class DialogueObfuscation
    {
        // Simple key for XOR encryption/decryption
        private static readonly byte[] key = Encoding.UTF8.GetBytes("AntigravityHorrorDialogueObfuscationKey2026");

        /// <summary>
        /// Obfuscates or de-obfuscates raw bytes.
        /// </summary>
        public static byte[] EncryptDecrypt(byte[] data)
        {
            if (data == null) return null;

            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ key[i % key.Length]);
            }
            return result;
        }

        /// <summary>
        /// Decrypts obfuscated bytes back to a UTF-8 string.
        /// </summary>
        public static string DecryptToString(byte[] encryptedData)
        {
            if (encryptedData == null) return null;
            byte[] decrypted = EncryptDecrypt(encryptedData);
            return Encoding.UTF8.GetString(decrypted);
        }

        /// <summary>
        /// Encrypts a UTF-8 string into obfuscated bytes.
        /// </summary>
        public static byte[] EncryptStringToBytes(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return null;
            byte[] raw = Encoding.UTF8.GetBytes(plainText);
            return EncryptDecrypt(raw);
        }
    }
}
