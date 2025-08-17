using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using Storage;

namespace Interface.Password;

public static class ScannerHandler
{
    private const int DimX = 1200;
    private const int DimY = 1700;

    private const int SuccessScoreCount = 24;
    
    private const double MinConfidence = 0.8; // Minimum difference between best and next best answer to consider it not double filled
    private const double MinFilledConfidence = 0.15; // Minimum % of pixels filled in the bubble to consider it filled
    
    private const int EmptyAnswer = -1;
    private const int WrongAnswer = -2;
    
    public static string ScanMultiPagePdf(string path)
    {
        var results = new List<string>();
        using var doclib = Docnet.Core.DocLib.Instance;
        using var reader = doclib.GetDocReader(path, new PageDimensions(DimX, DimY));

        var pageCount = reader.GetPageCount();
        
        for (var i = 0; i < pageCount; i++)
        {
            var pageImg = GetSinglePageAsBitmap(reader, i);
            var scanResult = ScanPageResults(pageImg, 15, 5); // TODO: Get question and answer count dynamically
            var resultCsv = ProcessScanResults(scanResult);
            results.Add(resultCsv);
        }
        var finalResult = string.Join("\n", results);
        return finalResult;
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
        var result = qrScanner.FindCodes();
        
        var matrixScanner = new Scanner.MatrixScanner(imageData);
        var matrixBubbles = matrixScanner.FindBubbles(questionCount, answerCount);
        
        var houghScanner = new Scanner.HoughScanner(imageData);
        var houghBubbles = houghScanner.FindBubbles(questionCount, answerCount);
        
        var bubbleChecker = new Scanner.BubbleChecker(imageData);
        var matrixResults = bubbleChecker.CheckBubbles(matrixBubbles, questionCount, answerCount);
        var houghResults = bubbleChecker.CheckBubbles(houghBubbles, questionCount, answerCount);
        
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
            Console.WriteLine(mResult);
            scanResult.MatrixQuestionResults[i] = new ScanResult.QuestionResult
            {
                SelectedAnswer = mResult,
                Confidence = (mBest - mNextBest) / mBest,
                BestAnswerConfidence = mBest,
            };
            var (hResult, hBest, hNextBest) = houghResults[i];
            scanResult.HoughQuestionResults[i] = new ScanResult.QuestionResult
            {
                SelectedAnswer = hResult,
                Confidence = (hBest - hNextBest) / hBest,
                BestAnswerConfidence = hBest,
            };
        }
        return scanResult;
    }

    private static string ProcessScanResults(ScanResult result)
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
                Console.WriteLine($"{i} is wrong :=> {mResult.BestAnswerConfidence}");
                answers[i] = WrongAnswer;
            }
        }

        var results = ExamResultObtainer.ObtainResults(answers, result.ExamCode);
        var totalScore = results.Sum();
        var isSuccess = totalScore > SuccessScoreCount ? "Sikeres" : "Sikertelen";
        var resultCsv = $"{result.UserCode}, {totalScore}, {isSuccess}, {string.Join(", ", results)}";
        Console.WriteLine(resultCsv); // TODO: Delete this line in production
        return resultCsv;
    } 
}