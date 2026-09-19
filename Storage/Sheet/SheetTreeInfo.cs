using Storage.Task;
using QuestionIssueList = System.Collections.Generic.List<System.Tuple<Storage.Task.Question, Storage.Sheet.SheetTreeInfo.QuestionIssue>>;

namespace Storage.Sheet;

public record SheetTreeInfo(uint? Min, uint? Max, bool ContainsNull = false, QuestionIssueList? QuestionIssues = null, List<long>? PossibleQuestions = null)
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

    public enum QuestionIssue
    {
        NoIssues, // Unused default
        /// The provided question has no answers added.
        ErrorNoAnswers,
        /// The provided question has exactly one answers added.
        Error1Answer,
        /// The provided question has no correct solution set.
        ErrorNoSolution,
        /// The provided question has multiple correct solutions set.
        ErrorMultipleSolutions,
        /// The provided question has less than 5 possible answers.
        WarningLessThan5Answers,
        /// The provided question was listed multiple times in the tree.
        WarningQuestionRepeated,
    }
    
    public SheetTreeInfo(uint? minMax, bool ContainsNull = false, QuestionIssueList? QuestionIssues = null, List<long>? PossibleQuestions = null) : this(minMax, minMax, ContainsNull, QuestionIssues, PossibleQuestions) {}
    
    /// The minimum number of questions a sheet derived from the tree can have.
    public uint? Min = Min;
    /// The minimum number of questions a sheet derived from the tree can have.
    public uint? Max = Max;

    /// Is `true` if at least 1 question in the tree is not set to a value. 
    public bool ContainsNull = ContainsNull;

    /// A list of questions which have warnings or errors.
    /// An empty list means no issues.
    public QuestionIssueList QuestionIssues = QuestionIssues ?? [];

    /// A list of every possible question used for duplicate detection.
    /// Since a low maximum is set for questions, this is a simple array. 
    public List<long> PossibleQuestions = PossibleQuestions ?? [];

    /// Combined left and right for when both trees are selected.
    public static SheetTreeInfo CombineAnd(SheetTreeInfo left, SheetTreeInfo right)
        => new(
            NullOp(left.Min, right.Min, (l, r) => l + r),
            NullOp(left.Max, right.Max, (l, r) => l + r),
            ContainsNull: left.ContainsNull || right.ContainsNull,
            QuestionIssues: [.. left.QuestionIssues, .. right.QuestionIssues],
            PossibleQuestions: [.. left.PossibleQuestions, .. right.PossibleQuestions]);
    
    /// Combined left and right for when only 1 tree is selected from them.
    public static SheetTreeInfo CombineOr(SheetTreeInfo left, SheetTreeInfo right)
        => new(
            NullOp(left.Min, right.Min, Math.Min), 
            NullOp(left.Max, right.Max, Math.Max),
            ContainsNull: left.ContainsNull || right.ContainsNull,
            QuestionIssues: [.. left.QuestionIssues, .. right.QuestionIssues],
            PossibleQuestions: CombineOrPossibleQuestions(left.PossibleQuestions, right.PossibleQuestions));

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

    /// Returns the bigger group of possible tasks.
    private static List<long> CombineOrPossibleQuestions(List<long> left, List<long> right)
    {
        var leftGroup = left.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
        var rightGroup = right.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
        var uniqueKeys = left.Union(right).Distinct().ToArray();
        List<long> combined = [];
        foreach (var key in uniqueKeys)
        {
            var leftCount = leftGroup.GetValueOrDefault(key, 0);
            var rightCount = rightGroup.GetValueOrDefault(key, 0);
            var repeat = (leftCount > rightCount) ? leftCount : rightCount;
            for (var i = 0; i < repeat; i++)
                combined.Add(key);
        }
        return combined;
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

    /// Returns a dictionary of every issue as a key, with a list of every offending question as its value.
    public Dictionary<QuestionIssue, List<Question>> GetQuestionIssues(QuestionList questions)
    {
        var repeatedQuestions = PossibleQuestions
            .GroupBy(x => x)
            .ToDictionary(x => x.Key, x => x.Count())
            .Where(kv => kv.Value > 1)
            .Select(kv => kv.Key)
            .ToArray();

        var issuesDir = QuestionIssues
            .GroupBy(t => t.Item2)
            .ToDictionary(e => e.Key, e => e.Select(t => t.Item1).ToList());

        if (repeatedQuestions.Length == 0)
            return issuesDir;

        List<Question> repeatQuestionsList = [];
        foreach (var id in repeatedQuestions)
        {
            if (Math.Clamp(id, 0, int.MaxValue) != id)
                throw new ArgumentOutOfRangeException($"Question index {id} is out of range.");
            repeatQuestionsList.Add(questions[(int)id]);
        }
        issuesDir.Add(QuestionIssue.WarningQuestionRepeated, repeatQuestionsList);
        return issuesDir;
    }
}