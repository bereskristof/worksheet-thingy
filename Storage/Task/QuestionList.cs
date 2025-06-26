using System.Collections.ObjectModel;

namespace Storage.Task;

public class QuestionList : ObservableCollection<Question>
{
    public void LoadAll()
    {
        var queryCommand = Manager.Connection.CreateCommand();
        queryCommand.CommandText = "SELECT Id, Question FROM Questions ORDER BY Id ASC;";
        var reader = queryCommand.ExecuteReader();
        while (reader.Read())
        {
            Add(Question.Load(reader.GetInt32(0), reader.GetString(1)));
        }
    }

    public Question Add()
    {
        var question = Question.New();
        base.Add(question);
        return question;
    }

    public new void Remove(Question question)
    {
        base.Remove(question);
        question.Delete();
    }
}