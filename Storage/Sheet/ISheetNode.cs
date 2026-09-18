using Storage.Task;

namespace Storage.Sheet;

public interface ISheetNode
{
    public Question[] GetQuestions();
    
    /// Get the number of questions this (sub)tree can have.
    public SheetTreeInfo GetSheetInfo();
}
