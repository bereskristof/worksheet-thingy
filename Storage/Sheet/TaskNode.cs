using Storage.Task;

namespace Storage.Sheet;

public class TaskNode : ISheetNode
{
    public required Question? Question { get; set; }

    public Question[] GetQuestions() 
        => Question is not null ? [Question] : [];
}