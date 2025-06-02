using EAGLE.Library;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EAGLE.Library;

/**
 *  \brief The Encryption class is used to encrypt / decrypt data.
 */
public class AesEncryption
{

    private static IConfigurationRoot _config = new ConfigurationBuilder()
        .AddUserSecrets<DbConnections>() // Load secrets
        .Build();

    private static string Key = _config["AppSettings:DevServer"]; // 256-bit key
    private static string Iv = _config["AppSettings:DevServer"]; // 128-bit IV

    //b14ca5898a4e4133bbce2ea2315a1916  // Example code.
    private AesEncryption(){}

    public static AesEncryption Instance = new();

    /**
     *  \brief Used to encrypt data to the Configuration Table class (and others if needed).
     *
     *  This is primarily used for encrypting some of the values found in Configuration.  However, this can also be used in the event we end up storing
     *  data that needs to be protected.
     *
     *  @param plaintext    The string you want to encrypt.
     *
     *  Example Code:
     *  ~~~~~~~~~~~~~~~{.c#}
     *       string value = Encrypt("Some data that needs protecting.");
     *  ~~~~~~~~~~~~~~~
     */
    [PublicAPI]
    public static string Encrypt(string plaintext)
    {
        using Aes aesAlg = GetAes();
        ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        using MemoryStream msEncrypt = new();
        using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
            csEncrypt.Write(plainBytes, 0, plainBytes.Length);
        }
        byte[] encryptedBytes = msEncrypt.ToArray();
        return Convert.ToBase64String(encryptedBytes);
    }

    /**
     *  \brief Used to decrypt results from the Configuration Table class (and others if needed).
     *
     *  This is primarily used for decrypting some of the values found in Configuration.  However, this can also be used in the event we end up storing
     *  data that needs to be protected.
     *
     *  @param cipherText   The encrypted data you want to un-encrypt.
     *
     *  Example Code:
     *  ~~~~~~~~~~~~~~~{.c#}
     *       string value = Decrypt(someEncryptedValue);
     *  ~~~~~~~~~~~~~~~
     */
    public static string Decrypt(string cipherText)
    {
        using Aes aesAlg = GetAes();

        // ReSharper disable once IdentifierTypo
        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
        byte[] decryptedBytes;
        using (MemoryStream msDecrypt = new(Convert.FromBase64String(cipherText)))
        {
            using (CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read))
            {
                using (MemoryStream msPlain = new())
                {
                    csDecrypt.CopyTo(msPlain);
                    decryptedBytes = msPlain.ToArray();
                }
            }
        }
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private static Aes GetAes()
    {
        Aes aesAlg = Aes.Create();
        aesAlg.Key = Encoding.ASCII.GetBytes(Key);
        aesAlg.IV = Encoding.ASCII.GetBytes(Iv);
        aesAlg.Mode = CipherMode.CBC;
        aesAlg.Padding = PaddingMode.PKCS7;
        //aesAlg.BlockSize = 128;

        return aesAlg;
    }
}