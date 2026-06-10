using System;
using System.Security.Cryptography;
using System.Text;

namespace $safeprojectname$.Services
{
    /// <summary>
    /// internal クラスにすることで難読化の対象とする
    /// </summary>
    internal static class EncryptionService
    {
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";
            try
            {
                // エントロピーをメソッド内に隠す
                byte[] entropy = { 0x12, 0x34, 0x56, 0x78, 0x9a };
                byte[] data = Encoding.UTF8.GetBytes(plainText);
                byte[] encrypted = ProtectedData.Protect(data, entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encrypted);
            }
            catch { return ""; }
        }

        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return "";
            try
            {
                byte[] entropy = { 0x12, 0x34, 0x56, 0x78, 0x9a };
                byte[] data = Convert.FromBase64String(encryptedText);
                byte[] decrypted = ProtectedData.Unprotect(data, entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch { return ""; }
        }
    }
}