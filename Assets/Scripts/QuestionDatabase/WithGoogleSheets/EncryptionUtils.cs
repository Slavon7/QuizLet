using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class EncryptionUtils
{
    // Ключ має бути 16, 24 або 32 символи (для AES-128, 192, 256)
    // ВЕЛИКЕ ПОПЕРЕДЖЕННЯ: Не залишайте цей ключ у відкритому вигляді в релізному коді.
    private static readonly string PrivateKey = "A67B98C2D1E44F5A8B9C0D1E2F3A4B5C"; 
    private static readonly string IV = "1234567890123456"; // 16 символів

    public static string Encrypt(string plainText)
    {
        byte[] key = Encoding.UTF8.GetBytes(PrivateKey);
        byte[] iv = Encoding.UTF8.GetBytes(IV);

        using (Aes aes = Aes.Create())
        {
            ICryptoTransform encryptor = aes.CreateEncryptor(key, iv);
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
    }

    public static string Decrypt(string cipherText)
    {
        byte[] key = Encoding.UTF8.GetBytes(PrivateKey);
        byte[] iv = Encoding.UTF8.GetBytes(IV);
        byte[] buffer = Convert.FromBase64String(cipherText);

        using (Aes aes = Aes.Create())
        {
            ICryptoTransform decryptor = aes.CreateDecryptor(key, iv);
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }
    }
}