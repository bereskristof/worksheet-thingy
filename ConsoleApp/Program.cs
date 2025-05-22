// See https://aka.ms/new-console-template for more information

using Storage;

namespace ConsoleApp;

public static class Program
{
    public static void Main()
    {
        Manager.CreateDatabase("demo.wstkdb");
        for (int i = 0; i < 500; i++)
        {
            var x = Storage.Task.Question.New();
            x.Text = "Very secret text! Do not steal!";
            x.Store();
        }
        // var x = Encryption.EncryptBase64("This is a string to encrypt");
        // var y = Encryption.DecryptBase64("/hbh/c/q/xO1cf7d2qfYFjb2vj8APwRrl5mNquXWDlKuBQIr3O9Xm9O4E/OLGqec");
        // Console.WriteLine(y);
        Console.WriteLine("All done!");
        Manager.CloseDatabase();
    }
}
