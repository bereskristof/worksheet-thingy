namespace Interface.Password;

public struct ScanResult
{
    public struct QuestionResult
    {
        public int SelectedAnswer;
        public double Confidence;
        public double BestAnswerConfidence;
    }
    
    public Guid ExamCode;
    public string UserCode;
    
    public QuestionResult[] MatrixQuestionResults;
    public QuestionResult[] HoughQuestionResults;
}