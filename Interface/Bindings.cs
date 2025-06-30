using Storage.Task;

namespace Interface;

/// Binding singleton
public sealed class Bindings
{
    private static Bindings? _instance;
    
    public static Bindings Instance => _instance ??= new Bindings();
    
    // Instance
    
    public QuestionList Questions { get; private set; } = [];
    
    private Bindings()
    {
        Questions.LoadAll();
    }
}