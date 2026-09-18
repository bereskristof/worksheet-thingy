using Storage.Task;

namespace Storage.Sheet;

public class TaskNode : ISheetNode
{
    public required Question? Question { get; set; }

    public Question[] GetQuestions() 
        => Question is not null ? [Question] : [];

    public SheetTreeInfo GetSheetInfo()
        => Question switch
        {
            null => new SheetTreeInfo(0, true),
            _ => new SheetTreeInfo(1),
        };
}