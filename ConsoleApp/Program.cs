// See https://aka.ms/new-console-template for more information

using System.Security.Cryptography;
using Storage;
using Storage.Task;

namespace ConsoleApp;

public static class Program
{
    public static void Main()
    {
        Manager.CreateDatabase(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "test0.db"), "");
    }
}
