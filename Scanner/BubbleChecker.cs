using OpenCvSharp;
using OpenCvSharp.Aruco;

namespace Scanner;

public class BubbleChecker
{
    private const int RadiusDecrease = 6; // Radius decrease for bubble detection
    
    private readonly Mat _originalImage;
    private readonly Mat _grayImage = new();
    private readonly Mat _threshImage = new();

    public BubbleChecker(byte[] imageData)
    {
        _originalImage = Cv2.ImDecode(imageData, ImreadModes.Color);
        Cv2.CvtColor(_originalImage, _grayImage, ColorConversionCodes.BGR2GRAY);
        var blurredImage = new Mat();
        var invertedThreshImage = new Mat();
        Cv2.GaussianBlur(_grayImage, blurredImage, new Size(3, 3), 0);
        Cv2.Threshold(blurredImage, invertedThreshImage, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.Binary);
        Cv2.BitwiseNot(invertedThreshImage, _threshImage);
    }
    
    public Tuple<int, double, double>[] CheckBubbles(CircleSegment[] bubbles, uint questionCount = 15, uint answerCount = 5)
    {
        var results = new List<Tuple<int, double, double>>();
        for (int q = 0; q < questionCount; q++)
        {
            double[] filledness = [-1, -1, -1, -1, -1]; // It IS a word: https://en.wiktionary.org/wiki/filledness
            for (int b = 0; b < answerCount; b++)
            {
                var bubble = bubbles[q * (int)answerCount + b];
                var mask = new Mat(_threshImage.Size(), MatType.CV_8UC1, Scalar.All(0));
                Cv2.Circle(mask, bubble.Center.ToPoint(), (int)bubble.Radius - RadiusDecrease, new Scalar(255), -1);
                var masked = new Mat();
                Cv2.BitwiseAnd(_threshImage, _threshImage, masked, mask);
                int pixelCount = Cv2.CountNonZero(masked);
                filledness[b] = pixelCount / (Math.PI * Math.Pow(bubble.Radius - RadiusDecrease, 2));
            }

            // var debugImg = _threshImage.Clone();
            // for (int b = 0; b < answerCount; b++)
            // {
            //     var bubble = bubbles[q * (int)answerCount + b];
            //     Cv2.Circle(debugImg, bubble.Center.ToPoint(), (int)bubble.Radius, Scalar.Red, 2);
            // }
            // Cv2.ImShow("aa", debugImg);
            // Cv2.WaitKey();
            
            var ordered = filledness
                .Select((value, index) => new { value, index })
                .OrderByDescending(x => x.value)
                .ToList();
            var maxIndex = ordered.First().index;
            var secondIndex = ordered.Skip(1).First().index;
            results.Add(
                new Tuple<int, double, double>(maxIndex, filledness[maxIndex], filledness[secondIndex])
            );
        }
        return results.ToArray();
    }
}