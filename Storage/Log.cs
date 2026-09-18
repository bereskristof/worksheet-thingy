using System.Diagnostics;

namespace Storage;

public static class Log
{
    public enum Severity
    {
        Information,
        Warning,
        Error
    }
    
    [Conditional("DEBUG")]
    public static void Write(string message, Severity severity = Severity.Information)
    {
        var now = DateTime.Now;
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Manager.PathTitle, "log.txt");
        var fileInfo = new FileInfo(filePath);
        fileInfo.Directory?.Create();
        var file = File.AppendText(filePath);
        file.WriteLine($"[{now}] {severity.ToString()}: " + message);
        file.Close();
    }
}