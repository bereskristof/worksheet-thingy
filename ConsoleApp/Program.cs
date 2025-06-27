// See https://aka.ms/new-console-template for more information

using System.Security.Cryptography;
using Storage;
using Storage.Task;

namespace ConsoleApp;

public static class Program
{
    public static void Main()
    {
        Manager.OpenDatabase("demo.wstkdb");
        // Encryption.SavePassword("DefaultPassword");
        Encryption.TryPassword("DefaultPassword");
        
        // for (int i = 0; i < 500; i++)
        // {
        //     if (i % 5 == 0) Console.Write(".");
        //     var x = new Storage.Task.QuestionList();
        //     var q = x.Add();
        //     q.Text = i.ToString();
        //     q.Store();
        //     var a = new AnswerList();
        //     // a.LoadAll(q);
        //     for (int j = 0; j < 5; j++)
        //     {
        //         a.Add(q);
        //     }
        // }
        
        var questionList = new QuestionList();
        questionList.LoadAll();
        
        foreach (var question in questionList)
        {
            Console.WriteLine($"Question: {question.Text}");
            byte[] image = question.FetchImageStream()!.ToArray();
            if (image.Length > 0)
            {
                Console.WriteLine($"Image size: {image.Length} bytes");
            }
            else
            {
                Console.WriteLine("No image available.");
            }
        }
        
        Manager.CloseDatabase();
    }
}
