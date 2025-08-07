using OpenCvSharp;

namespace Scanner;

public class CodeScanner
{
    private const char NameCodeSeparator = ';';
    
    public struct QrScanResult
    {
        public string Name;
        public string UserCode;
        public Guid Uuid;
    }
    
    private readonly Mat _originalImage;
    private readonly Mat _grayImage = new();
    private readonly Mat _threshImage = new();

    public CodeScanner(string imagePath)
    {
        _originalImage = Cv2.ImRead(imagePath);
        Cv2.CvtColor(_originalImage, _grayImage, ColorConversionCodes.BGR2GRAY);
        var blurredImage = new Mat();
        var invertedThreshImage = new Mat();
        Cv2.GaussianBlur(_grayImage, blurredImage, new Size(9, 9), 0);
        Cv2.Threshold(blurredImage, invertedThreshImage, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.Binary);
        Cv2.BitwiseNot(invertedThreshImage, _threshImage);
    }
    
    public QrScanResult FindCodes()
    {
        QrScanResult result = new();
        QRCodeDetector detector = new();
        detector.DetectMulti(_grayImage, out var points);
        detector.DecodeMulti(_grayImage, points, out var  decodedTexts);
        foreach (var text in decodedTexts)
        {
            if (string.IsNullOrEmpty(text)) continue;
            if (IsUuid(text))
            {
                var uuid = Guid.ParseExact(text, "N");
                result.Uuid = uuid;
            }
            else
            {
                var parts = text.Split(NameCodeSeparator);
                if (parts.Length < 2) continue;
                result.Name = parts[0].Trim();
                result.UserCode = parts[1].Trim();
            }
        }

        foreach (var point in points)
        {
            Cv2.DrawMarker(_originalImage, point.ToPoint(), Scalar.OrangeRed);
        }
        
        Cv2.Resize(_originalImage, _originalImage, new Size(0, 0), 0.5, 0.5);
        Cv2.ImShow("Detected Markers", _originalImage);
        Cv2.WaitKey();
        return result;
    }

    private bool IsUuid(string text)
        => Guid.TryParseExact(text, "N", out _);
}