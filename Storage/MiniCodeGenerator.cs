using System.Security.Cryptography;

namespace Storage;

public static class MiniCodeGenerator
{
    private static readonly HashSet<string> AlreadyGeneratedCodes = [];
    
    public static void Reset()
    {
        AlreadyGeneratedCodes.Clear();
    }
    
    public static string GenerateCode()
    {
        while (true)
        {
            string code = GetCode();
            if (!AlreadyGeneratedCodes.Add(code)) continue;
            return code;
        }
    }

    private static string GetCode()
    {
        const string alphanumeric = "ACDEFGHJKLMNPQRTUVWXYZ0123456789"; // To keep the characters equally distributed, B I O S are removed.
        char[] code = new char[7];
        byte[] randomBytes = new byte[7];
        RandomNumberGenerator.Fill(randomBytes);
        for (int i = 0; i < 7; i++)
        {
            code[i] = alphanumeric[randomBytes[i] % 32];
        }
        return new string(code[..2]) + "-" + new string(code[2..]);
    }
}