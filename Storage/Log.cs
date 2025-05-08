namespace Storage;

internal static class Log
{
    public enum Severity
    {
        Information,
        Warning,
        Error
    }
    
    public static void Write(string message, Severity severity = Severity.Information)
    {
        var now = DateTime.Now;
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WorksheetToolkit", "log.txt");
        var fileInfo = new FileInfo(filePath);
        fileInfo.Directory?.Create();
        var file = File.AppendText(filePath);
        file.WriteLine($"[{now}] {severity.ToString()}: " + message);
        file.Close();
    }
}