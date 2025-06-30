namespace Interface.Sheet;

public interface ICanRequestDeletion
{
    public event EventHandler? RequestsDeletion;

    public void Delete(bool root = false);
}