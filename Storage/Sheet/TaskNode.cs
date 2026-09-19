using Storage.Task;
using static Storage.Sheet.SheetTreeInfo.QuestionIssue;

namespace Storage.Sheet;

public class TaskNode : ISheetNode
{
    public required Question? Question { get; set; }

    public Question[] GetQuestions() 
        => Question is not null ? [Question] : [];

    public SheetTreeInfo GetSheetInfo()
        => Question switch
        {
            null => new SheetTreeInfo(0, ContainsNull: true, QuestionIssues: null),
            { } q => new SheetTreeInfo(1, QuestionIssues: q.Answers.Count switch
            {
                0 => [CreateQuestionIssue(q, ErrorNoAnswers)],
                1 => [CreateQuestionIssue(q, Error1Answer)],
                _ => q.GetCorrectAnswerCount() switch
                {
                    0 => [CreateQuestionIssue(q, ErrorNoSolution)],
                    > 1 => [CreateQuestionIssue(q, ErrorMultipleSolutions)],
                    _ => q.Answers.Count switch
                    {
                        < 5 => [CreateQuestionIssue(q, WarningLessThan5Answers)],
                        _ => null,
                    }
                }
            }, PossibleQuestions: [Question.Id]),
        };

    private static Tuple<Question, SheetTreeInfo.QuestionIssue> CreateQuestionIssue(Question q,
        SheetTreeInfo.QuestionIssue issue)
    {
        return new Tuple<Question, SheetTreeInfo.QuestionIssue>(q, issue);
    }
}