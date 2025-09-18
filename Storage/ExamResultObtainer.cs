using System.Diagnostics;
using Scanner;

namespace Storage;

/// Input: Array of obtained question results
/// Output: Array of integer values indicating how many points were scored for each question
public static class ExamResultObtainer
{
    public static void ObtainResults(ref ScanResult result)
    {
        var verifyCommand = Manager.Connection.CreateCommand();
        verifyCommand.CommandText = "SELECT AnswerNumber, QuestionId, QuestionNumber FROM Solutions WHERE Uuid == @Uuid ORDER BY QuestionNumber ASC;";
        verifyCommand.Parameters.AddWithValue("@Uuid", (result.ExamCode ?? throw new UnreachableException("ObtainResults: ExamCode is null despite already checking it!")).ToString());
        using var reader = verifyCommand.ExecuteReader();
        while (reader.Read())
        {
            var answerNumber = reader.GetInt32(0);
            var questionId = reader.GetInt32(1);
            var i = reader.GetInt32(2);
            
            var select = result.Results[i];
            select.TaskIndex = questionId;
            
            if (select.Points != null)
            {
                result.Results[i] = select;
                continue; // Already processed (empty or double filled)
            }

            select.Points =
                select.BestFilledAnswer == answerNumber 
                ? ScanResult.QuestionResult.CorrectAnswerPoints
                : ScanResult.QuestionResult.WrongAnswerPoints;
            
            result.Results[i] = select;
        }
        
        // TODO: Make this nicer
        if (result.CurrentState == ScanResult.State.Unknown)
        {
            result.CurrentState =
                result.Results.Any(x => x.Points == null || x.TaskIndex == null)
                    ? ScanResult.State.MissingTaskFromDatabase
                    : ScanResult.State.Completed;
        }
        if (result.CurrentState == ScanResult.State.ManuallyCorrected)
        {
            result.CurrentState =
                result.Results.Any(x => x.Points == null || x.TaskIndex == null)
                    ? ScanResult.State.MissingTaskFromDatabase
                    : ScanResult.State.CompletedWithManualCorrection;
        }
    }
}