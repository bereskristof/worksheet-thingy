using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using Storage.Sheet;
using Storage.Task;
using static Storage.Sheet.SheetTreeInfo;

namespace Interface.Export;

internal static partial class Exporting
{
    internal enum TreeSafetyResult
    {
        Allowed,
        Blocked,
    }

    /// Returns true if the tree can safely be turned into an exam.
    internal static TreeSafetyResult IsTreeSafe(SelectorNode root)
    {
        var info = root.GetSheetInfo();
        var diag = info.GetDiagnostics();
        var questionIssues = info.GetQuestionIssues(Bindings.Instance.Questions);

        if (CreateDiagnosticsMessage(diag) != TreeSafetyResult.Allowed)
            return TreeSafetyResult.Blocked;
        return CreateQuestionIssuesMessage(questionIssues);
    }

    private static TreeSafetyResult CreateDiagnosticsMessage(SheetTreeDiagnostics diag) {
        if (diag == SheetTreeDiagnostics.NoIssues)
            return TreeSafetyResult.Allowed;

        var messageBoxErrorMessage = diag switch
        {
            SheetTreeDiagnostics.ErrorPossibleBothZeroAndTooManyQuestions
                => Resources.Lang.ExportError_MayBeZeroOrTooMany,
            SheetTreeDiagnostics.ErrorPossibleZeroQuestions
                => Resources.Lang.ExportError_MayBeZero,
            SheetTreeDiagnostics.ErrorPossibleTooManyQuestions
                => Resources.Lang.ExportError_MayBeTooMany,
            SheetTreeDiagnostics.WarningEmptyQuestions
                => Resources.Lang.ExportError_HasNull,
            SheetTreeDiagnostics.WarningNonConstantQuestionCount
                => Resources.Lang.ExportError_NonConst,
            _ => throw new UnreachableException("IsTreeSafe"),
        };
        messageBoxErrorMessage = FormatString(messageBoxErrorMessage);

        var messageBoxImage = diag switch
        {
            SheetTreeDiagnostics.ErrorPossibleBothZeroAndTooManyQuestions
                or SheetTreeDiagnostics.ErrorPossibleZeroQuestions
                or SheetTreeDiagnostics.ErrorPossibleTooManyQuestions =>
                MessageBoxImage.Error,
            SheetTreeDiagnostics.WarningEmptyQuestions
                or SheetTreeDiagnostics.WarningNonConstantQuestionCount =>
                MessageBoxImage.Warning,
            _ => throw new UnreachableException("IsTreeSafe"),
        };

        var buttons = (messageBoxImage == MessageBoxImage.Error) ? MessageBoxButton.OK : MessageBoxButton.YesNo;
        
        var buttonPressed = MessageBox.Show(messageBoxErrorMessage, Resources.Lang.Common_Error, buttons, messageBoxImage);
        if (messageBoxImage == MessageBoxImage.Warning && buttonPressed == MessageBoxResult.Yes)
            return TreeSafetyResult.Allowed;
        return TreeSafetyResult.Blocked;
    }

    private static TreeSafetyResult CreateQuestionIssuesMessage(Dictionary<QuestionIssue, List<Question>> issues)
    {
        var issueMessages =
            new ReadOnlyDictionary<QuestionIssue, Tuple<string, MessageBoxImage>>(new Dictionary<QuestionIssue, Tuple<string, MessageBoxImage>>
            {
                { QuestionIssue.WarningLessThan5Answers, Tuple.Create(Resources.Lang.ExportError_NotEnoughAnswers, MessageBoxImage.Warning) },
                { QuestionIssue.ErrorNoAnswers, Tuple.Create(Resources.Lang.ExportError_ZeroAnswer, MessageBoxImage.Error) },
                { QuestionIssue.Error1Answer, Tuple.Create(Resources.Lang.ExportError_OneAnswer, MessageBoxImage.Error) },
                { QuestionIssue.ErrorNoSolution, Tuple.Create(Resources.Lang.ExportError_ZeroSolution, MessageBoxImage.Error) },
                { QuestionIssue.ErrorMultipleSolutions, Tuple.Create(Resources.Lang.ExportError_ManySolutions, MessageBoxImage.Error) },
                { QuestionIssue.WarningQuestionRepeated, Tuple.Create(Resources.Lang.ExportError_RepeatedQuestion, MessageBoxImage.Warning) },
            });

        foreach (var (issue, offender) in issues.OrderBy(kv => (int)kv.Key))
        {
            var (message, image) = issueMessages[issue];
            var subject = (offender.Count == 1) ? Subject.Singular : Subject.Plural;
            message = FormatString(message, subject, GetOffendersAsString(offender));
            var buttons = (image == MessageBoxImage.Error) ? MessageBoxButton.OK : MessageBoxButton.YesNo;
            var buttonPressed = MessageBox.Show(message, Resources.Lang.Common_Error, buttons, image);
            if (image == MessageBoxImage.Error || buttonPressed == MessageBoxResult.No)
                return TreeSafetyResult.Blocked;
        }
        return TreeSafetyResult.Allowed;
    }

    enum Subject
    {
        Singular,
        Plural,
    }
    
    [GeneratedRegex(@"\((.*?)(?:\|(.*?))?\)", RegexOptions.Compiled)]
    private static partial Regex SubjectRegex();

    /// Does string formatting for message boxes.
    /// Replaces combined forms: 'ha(ve|s)' with proper forms: 'has or have'.
    /// Also replaces specific strings like '#ERR#' and '#MAX#'
    private static string FormatString(string src, Subject subject = Subject.Singular, string err = "")
    {
        src = src.Replace("#MAX#", MaximumNumberOfQuestions.ToString()).Replace("#ERR#", err);
        var regex = SubjectRegex();
        foreach (Match match in regex.Matches(src))
        {
            var plural = match.Groups[1].Value;
            var singular = (match.Groups.Count > 1) ? match.Groups[2].Value : "";
            src = src.Replace(match.Value, (subject == Subject.Plural) ? plural : singular);
        }
        return src;
    }

    /// Returns a well formatted list of offending questions.
    private static string GetOffendersAsString(List<Question> offenders)
        => string.Join("\n", offenders.Select(q => $"{q.Id}: {q.Text.Split('\n').First()}"));
}