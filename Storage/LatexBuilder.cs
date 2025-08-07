namespace Storage;

/// State machine LaTeX builder.
public class LatexBuilder(string title, string author, string date)
{
    private const int AsciiA = 65;
    
    private string _text = string.Empty;
    
    public void AutoHeader()
    {
        Macro("documentclass", "exam", ["answers", "a4paper"]);
        Package("inputenc", ["utf8"]);
        Package("fontenc", ["T1"]);
        Package("geometry");
        Package("graphicx");
        Package("amsmath");
        Package("amsfonts");
        Package("amssymb");
        Package("tikz");
        Package("array");
        Package("tabularx");
        if (title != string.Empty)
            Macro("title", title);
        if (author != string.Empty)
            Macro("author", author);
        if (date != string.Empty)
            Macro("date", date);
        Begin("document");
    }
    
    public void AutoFooter()
    {
        End("document");
    }
    
    public void Text(string text, bool lineBreak = true)
    {
        _text += text + (lineBreak ? "\n" : "");
    }
    
    public void Macro(string macro, string content = "", string[]? options = null, bool lineBreak = true)
    {
        string optionString = string.Join(", ", options ?? []);
        if (!string.IsNullOrEmpty(optionString))
            optionString = "[" + optionString + "]";
        string contentString = string.IsNullOrEmpty(content) ? "" : "{" + content + "}";
        string lineBreakString = lineBreak ? "\n" : " ";
        _text += "\\" + macro + optionString + contentString + lineBreakString;
    }
    
    public void Package(string packageName, string[]? options = null)
    {
        Macro("usepackage", packageName, options);
    }
    
    public void Begin(string environment, bool lineBreak = true)
    {
        _text += "\\begin{" + environment + "}" + (lineBreak ? "\n" : " ");
    }
    
    public void End(string environment, bool lineBreak = true)
    {
        _text += "\\end{" + environment + "}" + (lineBreak ? "\n" : " ");
    }

    public void Question(string question, int points, string[] answers, string? imagePath = null)
    {
        Macro("question", lineBreak: false);
        Text(question);
        if (imagePath != null)
        {
            var safePath = imagePath.Replace('\\', '/'); // Ensure forward slashes for LaTeX
            Text(""); // Add a line break before the image
            Begin("center");
            Macro("includegraphics", safePath, [@"width=0.8\textwidth"]);
            End("center");
            Text("");
        }
        Begin("choices");
        foreach (string answer in answers)
        {
            Macro("choice", lineBreak: false);
            Text(answer);
        }
        End("choices");
    }

    public string Finish()
    {
        string result = _text;
        _text = string.Empty;
        return result;
    }

    public void AddTitle()
    {
        Text(@"\textsc{{\textbf{{\Large Feladatlap}}}} \\");
        if (title != string.Empty)
            Macro("textbf", title);
        if (title != string.Empty && author != string.Empty && date != string.Empty)
            Text("");
        if (author != string.Empty && date != string.Empty)
            Text(author + " --- " + date);
    }

    public void AddAnswerPage(int questionCount, byte answerCount, string? qrPath)
    {
        // Top menu
        Text(@"\newgeometry{a4paper, margin=8mm}");
        Text(@"\def\arraystretch{0.0}");
        Text(@"\setlength\tabcolsep{0.0pt}");
        Macro("noindent");
        Text(@"\begin{tabularx}{\textwidth}{|>{\centering\arraybackslash}m{113mm}|>{\centering\arraybackslash\ttfamily}X|>{\centering\arraybackslash}m{45mm}|}");
        Text(@"\hline");
        Text(@"\tikz{");
        Text(@"\path (0,0) rectangle (113mm, 45mm);");
        Text(@"\node[draw, dashed, minimum width=105mm, minimum height=37mm, color=black!66] at (56.5mm, 22.5mm) {Ide ragassza a QR kódot tartalmazó címkét!};");
        Text("} &");
        Text("DS-CODE &"); // TODO: Replace with actual display code
        Text($@"\includegraphics[width=37mm,height=37mm]{{{qrPath}}} \\");
        Text(@"\hline");
        Text(@"\end{tabularx}");
        
        // Title
        Begin("center");
        Text(@"\textsc{{\textbf{{\Large Válaszlap}}}} \\");
        if (title != "")
            Text($@"\textsc{{\textbf{{{title}}}}} \\");
        if (author != "" && date != "")
            Text($@"\textsc{{{author} --- {date}}} \\");
        End("center");
        
        // Help
        Text(@"\textsc{\textbf{\Large Figyelmesen olvassa el!}}");
        Begin("itemize");
        Text(@"\item Ezen a lapon csak akkor jelöljön meg választ, ha abban biztos, mivel azt már nem javíthatja!");
        Text(@"\item Ha több válasz is meg van jelölve, az hibának számít!");
        Text(@"\item Pontozás: Helyes válasz: 4 pont, kihagyott feladat: 0 pont, helytelen válasz: -1 pont");
        End("itemize");
        
        // Answer grid
        Text(@"\def\arraystretch{2.0}");
        Text(@"\setlength\tabcolsep{6.0pt}");
        Begin("center");
        Text(@"\begin{tabular}{|>{\centering\arraybackslash}m{0.8cm}|", lineBreak: false);
        for (int i = 0; i < answerCount; i++)
        {
            Text(@">{\centering\arraybackslash}m{0.8cm}", lineBreak: false);
        }
        Text(@"|>{\centering\arraybackslash}m{0.8cm}|}");
        Text(@"\hline");
        // Text(@"\null & ", lineBreak: false);
        Text(@"\parbox[c][6mm][c]{\linewidth}{\centerline{\includegraphics[height=6mm]{aruco_0.png}}} & ", lineBreak: false);
        for (int i = 0; i < answerCount; i++)
        {
            Text($@"\centerline{{{(char)(i + AsciiA)}}} & ", lineBreak: false);
        }
        // Text(@"\null \\");
        Text(@"\parbox[c][6mm][c]{\linewidth}{\centerline{\includegraphics[height=6mm]{aruco_2.png}}} \\");
        Text(@"\hline");
        for (int i = 0; i < questionCount; i++)
        {
            Text($"{i + 1} & ", lineBreak: false);
            for (int j = 0; j < answerCount; j++)
            {
                Text(@"\parbox[c][6mm][c]{\linewidth}{\centerline{\tikz{\draw (0,0) circle (2.5mm);}}} & ", lineBreak: false);
            }
            Text($@"{i + 1} \\");
        }
        Text(@"\hline");
        // Text(@"\null & ", lineBreak: false);
        Text(@"\parbox[c][6mm][c]{\linewidth}{\centerline{\includegraphics[height=6mm]{aruco_2.png}}} & ", lineBreak: false);
        for (int i = 0; i < answerCount; i++)
        {
            Text($@"\centerline{{{(char)(i + AsciiA)}}} & ", lineBreak: false);
        }
        // Text(@"\null \\");
        Text(@"\parbox[c][6mm][c]{\linewidth}{\centerline{\includegraphics[height=6mm]{aruco_2.png}}} \\");
        Text(@"\hline");
        End("tabular");
        End("center");
        // Text(@"\centering \includegraphics[height=8mm]{ruler-bottom.png}"); // TODO: Export image
        Text(@"\restoregeometry");
    }
}