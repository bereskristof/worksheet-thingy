namespace Storage;

/// Input: Array of obtained question results
/// Output: Array of integer values indicating how many points were scored for each question
public static class ExamResultObtainer
{
    private const int CorrectAnswerPoints = 4;
    private const int EmptyAnswerPoints = 0;
    private const int WrongAnswerPoints = -1;
    
    private const int EmptyAnswer = -1; // Mirrored from ScannerHandler.EmptyAnswer
    
    public static int[] ObtainResults(int[] answerIndices, Guid uuid)
    {
        var points = new int[answerIndices.Length];
        for (int i = 0; i < answerIndices.Length; i++)
        {
            var verifyCommand = Manager.Connection.CreateCommand();
            verifyCommand.CommandText = "SELECT AnswerNumber FROM Solutions WHERE Uuid == @Uuid AND QuestionNumber == @QuestionNumber;";
            verifyCommand.Parameters.AddWithValue("@Uuid", uuid.ToString());
            verifyCommand.Parameters.AddWithValue("@QuestionNumber", i);
            using var reader = verifyCommand.ExecuteReader();
            if (!reader.Read())
            {
                Log.Write($"ObtainResults: QuestionNumber {i} not in database", Log.Severity.Error);
                throw new Exception($"QuestionNumber {i} not in database"); // TODO: Handle this more gracefully, ask for manual checking
            }
            var correctAnswer = reader.GetInt32(0);
            if (answerIndices[i] == EmptyAnswer)
            {
                points[i] = EmptyAnswerPoints;
            }
            else if (answerIndices[i] == correctAnswer)
            {
                points[i] = CorrectAnswerPoints;
            }
            else
            {
                points[i] = WrongAnswerPoints;
            }
        }

        return points;
    }
}