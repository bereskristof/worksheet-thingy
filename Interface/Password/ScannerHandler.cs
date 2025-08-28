using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using Docnet.Core.Readers;
using Scanner;
using Storage;

namespace Interface.Password;

public static class ScannerHandler
{
    private const int SuccessScoreCount = 24;
    
    private const double MinConfidence = 0.8; // Minimum difference between best and next best answer to consider it not double filled
    private const double MinFilledConfidence = 0.1; // Minimum % of pixels filled in the bubble to consider it filled
    
    private const int EmptyAnswer = -1;
    private const int WrongAnswer = -2;

    public static void ScanPdfPage(List<string> bySheetResultsToMutate, List<string> byTaskResultsToMutate, IDocReader reader, int i)
    {
        var pageImg = GetSinglePageAsBitmap(reader, i);
        var scanResult = ScanPageResults(pageImg, 15, 5); // TODO: Get question and answer count dynamically
        var sheetResultCsv = ProcessScanResults(scanResult, i, ResultOrdering.BySheet);
        var taskResultCsv = ProcessScanResults(scanResult, i, ResultOrdering.ByTask);
        bySheetResultsToMutate.Add(sheetResultCsv);
        byTaskResultsToMutate.Add(taskResultCsv);
    }
    
    private static Bitmap GetSinglePageAsBitmap(IDocReader reader, int pageIndex)
    {
        using var pageReader = reader.GetPageReader(pageIndex);
        var page = pageReader.GetImage();
            
        var width = pageReader.GetPageWidth();
        var height = pageReader.GetPageHeight();
            
        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            
        var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        System.Runtime.InteropServices.Marshal.Copy(page, 0, bmpData.Scan0, page.Length);
        bmp.UnlockBits(bmpData);
            
        var stream = new MemoryStream();
        bmp.Save(stream, ImageFormat.Png);
        return bmp;
    }
    
    private static ScanResult ScanPageResults(Bitmap pageImg, uint questionCount, uint answerCount)
    {
        var imageConverter = new ImageConverter();
        var imageData = (byte[])(imageConverter.ConvertTo(pageImg, typeof(byte[])) ?? Array.Empty<byte>());
        var qrScanner = new Scanner.CodeScanner(imageData);
        CodeScanner.QrScanResult result;
        try
        {
            result = qrScanner.FindCodes();
        }
        catch (ArgumentException e)
        {
            return new ScanResult
            {
                ExamCode = Guid.Empty,
                UserCode = "???",
                MatrixQuestionResults = [],
                HoughQuestionResults = []
            };
        }

        var matrixScanner = new Scanner.MatrixScanner(imageData);
        var matrixBubbles = matrixScanner.FindBubbles(questionCount, answerCount);
        
        var bubbleChecker = new Scanner.BubbleChecker(imageData);
        var matrixResults = bubbleChecker.CheckBubbles(matrixBubbles, questionCount, answerCount);
        
        var scanResult = new ScanResult
        {
            ExamCode = result.Uuid,
            UserCode = result.UserCode,
            MatrixQuestionResults = new ScanResult.QuestionResult[questionCount],
            HoughQuestionResults = new ScanResult.QuestionResult[questionCount],
        };

        for (int i = 0; i < matrixResults.Length; i++)
        {
            var (mResult, mBest, mNextBest) = matrixResults[i];
            scanResult.MatrixQuestionResults[i] = new ScanResult.QuestionResult
            {
                SelectedAnswer = mResult,
                Confidence = (mBest - mNextBest) / mBest,
                BestAnswerConfidence = mBest,
            };
        }
        return scanResult;
    }
    
    public enum ResultOrdering
    {
        BySheet,
        ByTask
    }

    private static string ProcessScanResults(ScanResult result, int pageIndex, ResultOrdering ordering) // TODO: Add confidence
    {
        var answers = new int[result.MatrixQuestionResults.Length];
        for (int i = 0; i < result.MatrixQuestionResults.Length; i++)
        {
            var mResult = result.MatrixQuestionResults[i];
            if (mResult.BestAnswerConfidence < MinFilledConfidence)
            {
                answers[i] = EmptyAnswer;
            }
            else if (mResult.Confidence > MinConfidence)
            {
                answers[i] = mResult.SelectedAnswer;
            }
            else
            {
                answers[i] = WrongAnswer;
            }
        }

        var results = ExamResultObtainer.ObtainResults(answers, result.ExamCode, out var questionIds);
        var totalScore = results.Sum();
        var isSuccess = totalScore >= SuccessScoreCount ? "Sikeres" : "Sikertelen"; // TODO: Localize this
        string resultCsv;
        if (ordering == ResultOrdering.BySheet)
        {
            resultCsv = $"{pageIndex}, {result.UserCode}, {totalScore}, {isSuccess}, {string.Join(", ", results)}";
            resultCsv += $"\n, , , , {string.Join(", ", result.MatrixQuestionResults.Select(v => v.Confidence.ToString(CultureInfo.InvariantCulture)))}"; // Append question IDs for reference
        }
        else
        {
            int?[] taskOrderedResults = new int?[questionIds.Max() + 1];
            taskOrderedResults = taskOrderedResults
                .Select((_, i) => questionIds.Contains(i) ? (int?)results[questionIds.ToList().IndexOf(i)] : null).ToArray();
            string[] readableResults = taskOrderedResults
                .Select(r => (r == null) ? "" : r.ToString())
                .ToArray()!;
            resultCsv = $"{result.UserCode}, {pageIndex}, {totalScore}, {isSuccess}, {string.Join(", ", readableResults)}";
        }
            
        return resultCsv;
    }
}