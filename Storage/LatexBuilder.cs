namespace Storage;

/// State machine LaTeX builder.
public class LatexBuilder
{
    private string _text = string.Empty;
    
    public void AutoHeader(string title, string author, string date, string? backgroundRelativePath)
    {
        Macro("documentclass", "exam", ["addpoints", "answers", "a4paper"]);
        Package("inputenc", ["utf8"]);
        Package("fontenc", ["T1"]);
        Package("geometry");
        Package("amsmath");
        Package("amsfonts");
        Package("amssymb");
        Package("background");
        Macro("title", title);
        Macro("author", author);
        Macro("date", date);
        if (backgroundRelativePath != null)
            Macro("backgroundsetup", @"scale=1,angle=0,opacity=1,contents={\includegraphics[width=\paperwidth,height=\paperheight,keepaspectratio]{" + backgroundRelativePath + "}}");
        Begin("document");
        Begin("questions");
    }
    
    public void AutoFooter()
    {
        End("questions");
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

    public void Question(string question, int points, string[] answers, byte[]? image = null)
    {
        Macro("question", options: [points.ToString()], lineBreak: false);
        Text(question);
        if (image != null)
        {
            Text("IMAGE DATA GOES HERE"); // TODO: Handle image data
        }
        Begin("checkboxes");
        foreach (string answer in answers)
        {
            Macro("choice", lineBreak: false);
            Text(answer);
        }
        End("checkboxes");
    }

    public string Finish()
    {
        string result = _text;
        _text = string.Empty;
        return result;
    }
}