using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Docnet.Core.Readers;
using Scanner;
using Storage;

namespace Interface.Solve;

public static class ScannerHandler
{
    public static ScanResult ScanPdfPage(IDocReader reader, int i)
    {
        var pageImg = GetSinglePageAsBitmap(reader, i);
        var scanResult = ScanPageResults(pageImg, 15, 5); // TODO: Get question and answer count dynamically
        if (scanResult.CurrentState == ScanResult.State.MissingExamCode)
        {
            return scanResult;
        }
        ProcessScanResults(ref scanResult, i);
        return scanResult;
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
        var result = new ScanResult();
        result.Results = [];
        var imageConverter = new ImageConverter();
        var imageData = (byte[])(imageConverter.ConvertTo(pageImg, typeof(byte[])) ?? Array.Empty<byte>());
        var qrScanner = new CodeScanner(imageData);
        qrScanner.FindCodes(ref result);
        if (result.ExamCode == null)
        {
            result.CurrentState = ScanResult.State.MissingExamCode;
            return result;
        }
        if (result.UserCode == null)
        {
            result.CurrentState = ScanResult.State.MissingUserCode;
        }

        var matrixScanner = new MatrixScanner(imageData);
        var matrixBubbles = matrixScanner.FindBubbles(questionCount, answerCount);
        
        var bubbleChecker = new BubbleChecker(imageData);
        var matrixResults = bubbleChecker.CheckBubbles(matrixBubbles, questionCount, answerCount);

        result.Results = new ScanResult.QuestionResult[questionCount];

        for (int i = 0; i < matrixResults.Length; i++)
        {
            var (mResult, mBest, mNextBest) = matrixResults[i];
            result.Results[i] = new ScanResult.QuestionResult
            {
                TaskIndex = null,
                BestFilledAnswer = mResult,
                DeltaConfidence = double.IsNaN((mBest - mNextBest) / mBest) ? 1.0 : ((mBest - mNextBest) / mBest), // [0, 1], since mNextBest <= mBest
                FillConfidence = mBest, // [0, 1], since it's a ratio
                Points = null,
            };
        }

        return result;
    }

    private static void ProcessScanResults(ref ScanResult result, int pageIndex)
    {
        for (int i = 0; i < result.Results.Length; i++)
        {
            var res = result.Results[i];
            if (res.FillConfidence < ScanResult.QuestionResult.MinFillConfidence)
            {
                result.Results[i].Points = ScanResult.QuestionResult.EmptyAnswerPoints;
            }
            else if (res.DeltaConfidence <= ScanResult.QuestionResult.MinDeltaConfidence)
            {
                result.Results[i].Points = ScanResult.QuestionResult.WrongAnswerPoints;
            }
            // Else: Answer is filled correctly. Points will be assigned later, and as such, is left as null for now.
        }

        ExamResultObtainer.ObtainResults(ref result);
        result.FinalPoints = result.Results.Select(r => r.Points).Sum();
    }
    
    public enum CodeType
    {
        ExamCode,
        UserCode
    }

    // TODO: !!! Move to a better location, since this is used from SolvePage.xaml.cs
    public static bool TryUpdateResult(ref ScanResult result, string newCode, CodeType newType)
    {
        switch (newType)
        {
            case CodeType.ExamCode:
                if (!CodeScanner.IsUuid(newCode)) return false;
                var uuid = Guid.ParseExact(newCode, "N");
                result.ExamCode = uuid;
                if (CodeScanner.IsValidScanResult(result))
                {
                    result.CurrentState = ScanResult.State.ManuallyCorrected;
                }
                return true;
            case CodeType.UserCode:
                if (!CodeScanner.IsNeptunCode(newCode)) return false;
                result.UserCode = newCode;
                if (CodeScanner.IsValidScanResult(result))
                {
                    result.CurrentState = ScanResult.State.ManuallyCorrected;
                }
                return true;
            default:
                return false;
        }
    }
}