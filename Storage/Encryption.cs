using System.Security.Cryptography;

namespace Storage;

public static class Encryption
{
    private const int Iterations = 10_000;
    private static byte[] _key = [];
    
    // public static void Init()
    // {
    //     var keyCommand = Manager.Connection.CreateCommand();
    //     keyCommand.CommandText = "SELECT value FROM Security WHERE key = 'Key';";
    //     var reader = keyCommand.ExecuteReader();
    //     while (reader.Read())
    //     {
    //         Console.WriteLine("Key: {0}", Convert.FromBase64String(reader.GetString(0)));
    //         _key = Convert.FromBase64String(reader.GetString(0));
    //     }
    //     if (_key.Length == 0)
    //     {
    //         Log.Write("Security key not found, creating new one");
    //         var aes = Aes.Create();
    //         if (_key.Length == 0) CreateKey(aes);
    //         return;
    //     }
    //     Console.WriteLine(reader);
    // }

    public static void SavePassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(32);
        byte[] verify = RandomNumberGenerator.GetBytes(1024);
        byte[] hash = SHA256.HashData(verify);
        
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        _key = key;

        byte[] data = Encrypt(Convert.ToBase64String(verify));

        var storeCommand = Manager.Connection.CreateCommand();
        storeCommand.CommandText = "INSERT INTO Security (key, value) VALUES ('Verify', @Verify), ('Salt', @Salt);";

        byte[] concat = hash.Concat(data).ToArray();
        storeCommand.Parameters.AddWithValue("@Verify", Convert.ToBase64String(concat));
        storeCommand.Parameters.AddWithValue("@Salt", Convert.ToBase64String(salt));
        storeCommand.ExecuteNonQuery();
    }

    /// WARNING: Sets the encryption key as a side effect, not just checks for validity.
    public static bool TryPassword(string password)
    {
        byte[] salt = [];
        var saltCommand = Manager.Connection.CreateCommand();
        saltCommand.CommandText = "SELECT value FROM Security WHERE key = 'Salt';";
        var saltReader = saltCommand.ExecuteReader();
        while (saltReader.Read())
        {
            salt = Convert.FromBase64String(saltReader.GetString(0));
        }

        if (salt.Length == 0)
        {
            throw new CryptographicException("Salt missing from database");
        }

        byte[] verify = [];
        var verifyCommand = Manager.Connection.CreateCommand();
        verifyCommand.CommandText = "SELECT value FROM Security WHERE key = 'Verify';";
        var reader = verifyCommand.ExecuteReader();
        while (reader.Read())
        {
            verify = Convert.FromBase64String(reader.GetString(0));
        }

        if (verify.Length == 0)
        {
            throw new CryptographicException("Verify missing from database");
        }
        
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        _key = key;

        try
        {
            byte[] data = Convert.FromBase64String(Decrypt(verify[32..]));
            var challengeHash = SHA256.HashData(data);
            return challengeHash.SequenceEqual(verify[..32]);
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    // private static bool ValidateKey(byte[] )

    // private static void CreateKey(Aes aes)
    // {
    //     _key = aes.Key;
    //     var storeCommand = Manager.Connection.CreateCommand();
    //     storeCommand.CommandText = "INSERT INTO Security (key, value) VALUES ('Key', @keyValue);";
    //     storeCommand.Parameters.AddWithValue("@keyValue", Convert.ToBase64String(_key));
    //     storeCommand.ExecuteNonQuery();
    // }
    
    private static byte[] Encrypt(string plainText)
    {
        using Aes aesAlg = Aes.Create();
        aesAlg.Key = _key;

        ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        using MemoryStream msEncrypt = new MemoryStream();
        using CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        StreamWriter swEncrypt = new StreamWriter(csEncrypt);
        swEncrypt.Write(plainText);
        swEncrypt.Close();

        var encrypted = msEncrypt.ToArray();

        return aesAlg.IV.Concat(encrypted).ToArray();
    }
    
    public static byte[] EncryptBlob(byte[] blob)
    {
        using Aes aesAlg = Aes.Create();
        aesAlg.Key = _key;

        ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        using MemoryStream msEncrypt = new MemoryStream();
        using CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        BinaryWriter bwEncrypt = new BinaryWriter(csEncrypt);
        bwEncrypt.Write(blob);
        bwEncrypt.Close();

        var encrypted = msEncrypt.ToArray();

        return aesAlg.IV.Concat(encrypted).ToArray();
    }
    
    public static string EncryptBase64(string plainText) 
        => Convert.ToBase64String(Encrypt(plainText));

    private static string Decrypt(byte[] cipherText)
    {
        const int offset = 16;
        using Aes aesAlg = Aes.Create();
        aesAlg.Key = _key;
        aesAlg.IV = cipherText.Take(offset).ToArray();

        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        using MemoryStream msDecrypt = new MemoryStream(cipherText.Take(Range.StartAt(offset)).ToArray());
        using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using StreamReader srDecrypt = new StreamReader(csDecrypt);
        
        var plaintext = srDecrypt.ReadToEnd();
        return plaintext;
    }

    public static byte[] DecryptBlob(byte[] cipherBlob)
    {
        const int offset = 16;
        using Aes aesAlg = Aes.Create();
        aesAlg.Key = _key;
        aesAlg.IV = cipherBlob.Take(offset).ToArray();

        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        using MemoryStream msDecrypt = new MemoryStream(cipherBlob.Take(Range.StartAt(offset)).ToArray());
        using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using BinaryReader brDecrypt = new BinaryReader(csDecrypt);
        
        int remaining = (int)(msDecrypt.Length - msDecrypt.Position);
        byte[] decrypted = brDecrypt.ReadBytes(remaining);
        return decrypted;
    }
    
    public static string DecryptBase64(string cipherText)
        => Decrypt(Convert.FromBase64String(cipherText));
    
    public static bool CanDecryptBase64(string text, out string decryptedText)
    {
        bool isDecrypted = false;
        try
        {
            decryptedText = DecryptBase64(text);
            isDecrypted = true;
        }
        catch (Exception e) when (e is CryptographicException or FormatException)
        {
            decryptedText = "[DAMAGED] " + text;
        }
        return isDecrypted;
    }
}