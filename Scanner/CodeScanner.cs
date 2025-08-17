using System.Drawing;
using OpenCvSharp;
using ZXing;
using ZXing.Common;
using ZXing.Multi;
using ZXing.Windows.Compatibility;
using Image = System.Drawing.Image;

namespace Scanner;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class CodeScanner
{
    public struct QrScanResult
    {
        public string UserCode;
        public Guid Uuid;
    }
    
    private readonly Mat _originalImage;
    private readonly Mat _grayImage = new();

    [Obsolete]
    public CodeScanner(string imagePath)
    {
        _originalImage = Cv2.ImRead(imagePath);
        Cv2.CvtColor(_originalImage, _grayImage, ColorConversionCodes.BGR2GRAY);
    }
    
    public CodeScanner(byte[] imageData)
    {
        _originalImage = Cv2.ImDecode(imageData, ImreadModes.Color);
        Cv2.CvtColor(_originalImage, _grayImage, ColorConversionCodes.BGR2GRAY);
    }
    
    /// <exception cref="ArgumentException">Failed to find QR codes</exception>
    public QrScanResult FindCodes()
    {
        var bm = (Bitmap)Image.FromStream(_originalImage.ToMemoryStream());
        var findings = FindQrCodesUsingZxing(bm);
        var zxingResult = ParseDecodedText(findings);
        if (IsValidScanResult(zxingResult))
        {
            return zxingResult;
        }
        
        var opencvFindings = FindQrCodesUsingOpenCv(_grayImage);
        var opencvResult = ParseDecodedText(opencvFindings);
        if (IsValidScanResult(opencvResult))
        {
            return opencvResult;
        }

        if (string.IsNullOrEmpty(zxingResult.UserCode) && string.IsNullOrEmpty(opencvResult.UserCode))
        {
            throw new ArgumentException("Failed to find QR codes in the image.");
        }
        if (zxingResult.Uuid == Guid.Empty && opencvResult.Uuid == Guid.Empty)
        {
            throw new ArgumentException("Failed to find UUID in the QR codes.");
        }

        return new QrScanResult
        {
            UserCode = !string.IsNullOrEmpty(zxingResult.UserCode) ? zxingResult.UserCode : opencvResult.UserCode,
            Uuid = zxingResult.Uuid != Guid.Empty ? zxingResult.Uuid : opencvResult.Uuid,
        };
    }
    
    private static string[] FindQrCodesUsingZxing(Bitmap image)
    {
        var binary = new BinaryBitmap(new HybridBinarizer(new BitmapLuminanceSource(image)));
        
        var baseReader = new MultiFormatReader()
        {
            Hints = new Dictionary<DecodeHintType, object>
            {
                { DecodeHintType.POSSIBLE_FORMATS, new List<BarcodeFormat> { BarcodeFormat.QR_CODE } },
                { DecodeHintType.TRY_HARDER, true },
            }
        };

        var reader = new GenericMultipleBarcodeReader(baseReader);
        var result = reader.decodeMultiple(binary);
        
        return result.Select(r => r?.Text ?? string.Empty).ToArray();
    }

    // No shot this would do anything if ZXing fails, but who knows
    private static string[] FindQrCodesUsingOpenCv(Mat image)
    {
        QRCodeDetector detector = new();
        Point2f[] points;
        
        detector.DetectMulti(image, out points);
        if (points.Length == 0)
        {
            return [];
        }
        
        detector.DecodeMulti(image, points, out var decodedTexts);
        return decodedTexts.Select(r => r ?? string.Empty).ToArray();
    }
    
    private static QrScanResult ParseDecodedText(string[] texts)
    {
        var result = new QrScanResult();

        foreach (var text in texts)
        {
            if (string.IsNullOrEmpty(text)) continue;
            if (IsUuid(text))
            {
                var uuid = Guid.ParseExact(text, "N");
                result.Uuid = uuid;
            }
            else if (IsNeptunCode(text))
            {
                result.UserCode = text;
            }
        }

        return result;
    }
    
    private static bool IsValidScanResult(QrScanResult result)
        => result.Uuid != Guid.Empty && !string.IsNullOrEmpty(result.UserCode);

    private static bool IsUuid(string text)
        => Guid.TryParseExact(text, "N", out _);

    private static bool IsNeptunCode(string text)
        => text.Length == 6 && text.All(char.IsLetterOrDigit);
}