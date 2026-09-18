using System.Diagnostics;
using System.Windows;
using Storage.Sheet;
using static Storage.Sheet.SheetTreeInfo;

namespace Interface.Export;

internal static class Exporting
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
        messageBoxErrorMessage = messageBoxErrorMessage.Replace("#MAX#", MaximumNumberOfQuestions.ToString());

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
}