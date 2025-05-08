using System.Security.Cryptography;

namespace Storage;

// TODO: Make iv size hard coded
public static class Encryption
{
    private static byte[] _key = [];
    
    public static void GetKeyAndIv()
    {
        var keyCommand = Manager.Connection.CreateCommand();
        keyCommand.CommandText = "SELECT key, value FROM Security WHERE key = 'Key' OR key = 'IV';";
        var reader = keyCommand.ExecuteReader();
        while (reader.Read())
        {
            switch (reader.GetString(0))
            {
                case "Key":
                    Console.WriteLine("Key: {0}", Convert.FromBase64String(reader.GetString(1)));
                    _key = Convert.FromBase64String(reader.GetString(1));
                    break;
            }
        }
        if (_key.Length == 0)
        {
            Log.Write("Security key not found, creating new one"); // TODO: THIS IS OBVIOUSLY FUCKING RETARDED, REPLACE IT WITH SOMETHING THAT DOESN'T JUST SLOW THINGS DOWN!
            var aes = Aes.Create();
            if (_key.Length == 0) CreateKey(aes);
            return;
        }
        Console.WriteLine(reader);
    }

    private static void CreateKey(Aes aes)
    {
        _key = aes.Key;
        var storeCommand = Manager.Connection.CreateCommand();
        storeCommand.CommandText = "INSERT INTO Security (key, value) VALUES ('Key', @keyValue);";
        storeCommand.Parameters.AddWithValue("@keyValue", Convert.ToBase64String(_key));
        storeCommand.ExecuteNonQuery();
    }
    
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
    
    public static string DecryptBase64(string cipherText)
        => Decrypt(Convert.FromBase64String(cipherText));
}