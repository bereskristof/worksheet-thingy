using System.Collections.Immutable;
using System.Diagnostics;

namespace Storage;

public static class TexDoctor
{
    private const string RequiredClass = "exam";
    private static readonly ImmutableArray<string> RequiredPackages =
    [
        "inputenc",
        "fontenc",
        "geometry",
        "graphicx",
        "amsmath",
        "amsfonts",
        "amssymb",
        "tikz",
        "array",
        "tabularx",
    ];
    
    private static string[]? _missingPackageCache;
    
    public enum TexStatus
    {
        Operational,
        MissingPackages,
        NotFound,
    }
    
    // public static string? GetTexPath()
    // {
    //     if (_texPath == null)
    //     {
    //         FindPdfLatex();
    //     }
    //     return _texPath;
    // }

    public static TexStatus VerifyTexInstallation(out string[] missingPackages)
    {
        missingPackages = [];
        var path = FindValidPdfLatexPath();
        if (path == null) return TexStatus.NotFound;
        missingPackages = GetMissingPackages();
        if (missingPackages.Length == 0) return TexStatus.Operational;
        _missingPackageCache = missingPackages;
        return TexStatus.MissingPackages;
    }
    
    private static string? FindValidPdfLatexPath()
    {
        var isPdfLatexInPath = CallPdfLatex("pdflatex");
        if (isPdfLatexInPath) return "pdflatex";
        var manualPath = GetPdfLatexFromFile();
        if (manualPath == "") return null;
        var isPdfLatexInCustomPath = CallPdfLatex(manualPath);
        return !isPdfLatexInCustomPath ? null : manualPath;
    }

    private static string[] GetMissingPackages()
    {
        if (_missingPackageCache != null)
            return _missingPackageCache;
        List<string> missingPackages = [];
        missingPackages
            .AddRange(RequiredPackages
                .Select(package => $"{package}.sty")
                .Where(packageName => !CallKpsewhich(packageName))
            );
        if (!CallKpsewhich($"{RequiredClass}.cls"))
            missingPackages.Add($"{RequiredClass}.cls");
        return missingPackages.ToArray();
    }
    
    private static bool CallPdfLatex(string latexPath)
    {
        const string flags = "-version";
        var process = new Process();
        process.StartInfo = new ProcessStartInfo(latexPath, flags)
            { CreateNoWindow = true };
        try
        {
            process.Start();
        }
        catch (Exception)
        {
            return false; // If the process fails to start, pdflatex is not available
        }
        var finished = process.WaitForExit(30_000); // Wait for 30 seconds for the process to complete
        if (finished && process.ExitCode == 0) return true;
        process.Kill(true);
        return false;
    }
    
    private static bool CallKpsewhich(string packageName)
    {
        var process = new Process();
        process.StartInfo = new ProcessStartInfo("kpsewhich", packageName)
            { CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        var finished = process.WaitForExit(30_000); // Wait for 30 seconds for the process to complete
        return finished && process.ExitCode == 0 && !string.IsNullOrEmpty(output) && string.IsNullOrEmpty(error);
    }

    private static string GetPdfLatexFromFile()
    {
        string exePath = AppDomain.CurrentDomain.BaseDirectory;
        string filePath = Path.Combine(exePath, "pdflatex-path.txt");
        string content;
        try
        {
            content = File.ReadAllText(filePath);
        }
        catch (Exception)
        {
            content = string.Empty;
        }
        return content.Trim();
    }
}