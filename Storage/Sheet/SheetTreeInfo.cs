namespace Storage.Sheet;

public record SheetTreeInfo(uint? Min, uint? Max, bool ContainsNull = false)
{
    /// The maximum amount of questions a page can have before it causes a broken answer sheet.
    public const uint MaximumNumberOfQuestions = 17;
    
    public enum SheetTreeDiagnostics {
        NoIssues,
        /// The root tree has a non-constant amount of questions.
        /// While this is not fatal, it is most likely unintentional.
        WarningNonConstantQuestionCount,
        /// Some questions in the tree are left in a '...' state.
        /// While this is not fatal, it is most likely unintentional.
        WarningEmptyQuestions,
        /// The root tree has a chance to contain zero questions.
        ErrorPossibleZeroQuestions,
        /// The root tree has a chance to contain too many questions to fit on a single page.
        ErrorPossibleTooManyQuestions,
        /// The root tree both has a chance to contain zero questions and too many questions.
        ErrorPossibleBothZeroAndTooManyQuestions,
    }
    
    public SheetTreeInfo(uint? minMax, bool ContainsNull = false) : this(minMax, minMax, ContainsNull) {}
    
    /// The minimum number of questions a sheet derived from the tree can have.
    public uint? Min = Min;
    /// The minimum number of questions a sheet derived from the tree can have.
    public uint? Max = Max;

    /// Is `true` if at least 1 question in the tree is not set to a value. 
    public bool ContainsNull = ContainsNull;

    /// Elementwise summation for `min` and `max`.
    public static SheetTreeInfo Add(SheetTreeInfo left, SheetTreeInfo right)
        => new SheetTreeInfo(NullOp(left.Min, right.Min, (l, r) => l + r),
            NullOp(left.Max, right.Max, (l, r) => l + r),
            left.ContainsNull || right.ContainsNull);
    
    /// Combined minimum and maximum selection.
    public static SheetTreeInfo MinMax(SheetTreeInfo left, SheetTreeInfo right)
        => new(NullOp(left.Min, right.Min, Math.Min), NullOp(left.Max, right.Max, Math.Max),
            left.ContainsNull || right.ContainsNull);

    /// Performs operation `op` on `a` and `b` if and only if both of them are uint.
    /// If only one of them are a uint, then that value is returned instead.
    /// If both values are null, null is returned.
    /// This function is used to safely ignore if no values are added to a group.
    private static uint? NullOp(uint? a, uint? b, Func<uint, uint, uint> op)
    {
        return (a, b) switch
        {
            (null, null) => null,
            (null, not null) => b,
            (not null, null) => a,
            ({ } left, { } right) => op.Invoke(left, right),
        };
    }

    /// Generate a diagnostics enum from this object.
    public SheetTreeDiagnostics GetDiagnostics()
    {
        bool canZero = Min is 0 or null;
        bool canMany = Max > MaximumNumberOfQuestions;
        switch (canZero, canMany)
        {
            case (true, true): return SheetTreeDiagnostics.ErrorPossibleBothZeroAndTooManyQuestions;
            case (true, false): return SheetTreeDiagnostics.ErrorPossibleZeroQuestions;
            case (false, true): return SheetTreeDiagnostics.ErrorPossibleTooManyQuestions;
        }

        if (ContainsNull)
            return SheetTreeDiagnostics.WarningEmptyQuestions;
        if (Min != Max)
            return SheetTreeDiagnostics.WarningNonConstantQuestionCount;
        
        return SheetTreeDiagnostics.NoIssues;
    }
}