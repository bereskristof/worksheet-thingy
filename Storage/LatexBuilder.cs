namespace Storage;

/// State machine LaTeX builder.
public class LatexBuilder(string title, string author, string date)
{
    private const int AsciiA = 65;
    
    private string _text = string.Empty;
    
    public void AutoHeader()
    {
        Macro("documentclass", "exam", ["answers", "a4paper", "twoside"]);
        Package("inputenc", ["utf8"]);
        Package("fontenc", ["T1"]);
        Package("geometry", ["inner=3cm", "outer=3cm"]);
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
        Text(@"\pagestyle{plain}");
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
        Text("");
        Begin("oneparchoices");
        foreach (string answer in answers)
        {
            Macro("choice", lineBreak: false);
            Text(answer);
        }
        End("oneparchoices");
        Macro("vspace", "4mm");
    }

    public string Finish()
    {
        string result = _text;
        _text = string.Empty;
        return result;
    }

    public void AddTitle(string miniCode)
    {
        if (title != string.Empty)
            Text(@$"\centering\textsc{{\textbf{{\Large{{{title}}}}}}}");
        if (title != string.Empty && author != string.Empty && date != string.Empty)
            Text("");
        if (author != string.Empty && date != string.Empty)
            Text(@$"\centering{{{author} --- {date}}}");
        if (title != string.Empty || author != string.Empty || date != string.Empty)
            Text("\n\\hfill\n");
        Text(@"\centering\textsc{{\textbf{{\Large Feladatlap}}}} \\");
        Text(@"\hfill\texttt{" + miniCode + @"} \\");
    }

    public void AddAnswerPage(int questionCount, byte answerCount, string miniCode, string? qrPath)
    {
        // Top menu
        Text(@"\pagestyle{empty}");
        Text(@"\newgeometry{margin=12mm}");
        Text(@"\def\arraystretch{0.0}");
        Text(@"\setlength\tabcolsep{0.0pt}");
        Macro("noindent");
        Text(@"\begin{tabularx}{\textwidth}{|>{\centering\arraybackslash}m{113mm}|>{\centering\arraybackslash\ttfamily}X|>{\centering\arraybackslash}m{45mm}|}");
        Text(@"\hline");
        Text(@"\tikz{");
        Text(@"\path (0,0) rectangle (113mm, 45mm);");
        Text(@"\node[draw, dashed, minimum width=105mm, minimum height=37mm, color=black!66] at (56.5mm, 22.5mm) {Ide ragassza a QR kódot tartalmazó címkét!};");
        Text("} &");
        Text($"{miniCode} &");
        Text($@"\includegraphics[width=37mm,height=37mm]{{{qrPath}}} \\");
        Text(@"\hline");
        Text(@"\end{tabularx}");
        
        // Title
        Begin("center");
        Text(@"\textsc{{\textbf{{\Large Válaszlap}}}} \\");
        // if (title != "")
        //     Text($@"\textsc{{\textbf{{{title}}}}} \\");
        // if (author != "" && date != "")
        //     Text($@"\textsc{{{author} --- {date}}} \\");
        End("center");
        
        // Help
        Text(@"\textsc{\textbf{\Large Figyelmesen olvassa el!}}");
        Begin("itemize");
        Text(@"\item A teszt \textbf{15 feleletválasztós feladatot} tartalmaz, a megírására \textbf{45 perc} áll rendelkezésre. A feladatok szövege után öt lehetséges válasz található, amelyek közül \textbf{pontosan egy a helyes}. Ha egy feladatnál több válasz is jelölt, azt rossz válasznak tekintjük és 1 pont levonással jár. Minden jó válasz 4 pontot ér, a hibás válasz 1 pont levonásával jár, a nem megválaszolt feladatra nem jár pont. Az elérhető maximális pontszám 60 pont, a dolgozat sikeres, ha legalább 24 pontos.");
        Text(@"\item A kódlapon a feladatok sorszáma melletti öt négyzet közül a helyes válasz betűjelének megfelelő négyzetbe \(\times\)-et kell sötétkék vagy fekete tollal, \textbf{jól láthatóan} beírni, a többi négy négyzetet pedig üresen kell hagyni. Amennyiben a többi négy négyzet nem teljesen üres (valamelyikben tollal vagy ceruzával írt betű, szám vagy bármilyen jelkezdemény szerepel) a feladatra adott válasz rossz válasznak számít. Radír, javító festék vagy hibajavító toll használata esetén a feladatra adott válasz szintén rossz válasznak számít. Ha valaki egy feladatra nem ad választ, az nem számít rossz megoldásnak. Ebben az esetben a kódlapon a feladat sorszáma melletti négyzeteket üresen kell hagyni. A kódlapot jól láthatóan, sötétkék vagy fekete tollal kell kitölteni, mert más színeket, halványan, vékonyan és kis jelekkel kitöltött kódlap jeleit a leolvasó rendszer nem érzékeli.");
        End("itemize");
        
        // Answer grid
        Text(@"\def\arraystretch{0.0}");
        Text(@"\setlength\tabcolsep{6.0pt}");
        Begin("center");
        Text(@"\begin{tabular}{|>{\centering\arraybackslash}m{0.8cm}|", lineBreak: false);
        for (int i = 0; i < answerCount; i++)
        {
            Text(@">{\centering\arraybackslash}m{0.8cm}", lineBreak: false);
        }
        Text(@"|>{\centering\arraybackslash}m{0.8cm}|}");
        Text(@"\hline");
        Text(@"\parbox[c][8mm][c]{\linewidth}{\centerline{\includegraphics[height=6mm]{aruco_2.png}}} & ", lineBreak: false); // Markers are ordered weirdly to make the inner corner id equal its id
        for (int i = 0; i < answerCount; i++)
        {
            Text($@"\parbox[c][8mm][c]{{\linewidth}}{{\centerline{{{(char)(i + AsciiA)}}}}} & ", lineBreak: false);
        }
        Text(@"\parbox[c][8mm][c]{\linewidth}{\centerline{\includegraphics[height=6mm]{aruco_3.png}}} \\");
        Text(@"\hline");
        for (int i = 0; i < questionCount; i++)
        {
            Text($@"\parbox[c][8mm][c]{{\linewidth}}{{\centering {i + 1}}} & ", lineBreak: false);
            for (int j = 0; j < answerCount; j++)
            {
                Text(@"\parbox[c][8mm][c]{\linewidth}{\centerline{\tikz{\draw (0,0) circle (2.5mm);}}} & ", lineBreak: false);
            }
            Text($@"\parbox[c][8mm][c]{{\linewidth}}{{\centering {i + 1}}} \\");
        }
        Text(@"\hline");
        Text(@"\parbox[c][8mm][c]{\linewidth}{\centerline{\includegraphics[height=6mm]{aruco_1.png}}} & ", lineBreak: false);
        for (int i = 0; i < answerCount; i++)
        {
            Text($@"\parbox[c][8mm][c]{{\linewidth}}{{\centerline{{{(char)(i + AsciiA)}}}}} & ", lineBreak: false);
        }
        Text(@"\parbox[c][8mm][c]{\linewidth}{\centerline{\includegraphics[height=6mm]{aruco_0.png}}} \\");
        Text(@"\hline");
        End("tabular");
        End("center");
        Text(@"\restoregeometry");
    }
}