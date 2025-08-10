using OpenCvSharp;
using OpenCvSharp.Aruco;

namespace Scanner;

public class HoughScanner
{
    private const float CenterNearnessDotThreshold = 1.0f - 1e-3f;
    
    private readonly Mat _originalImage;
    private readonly Mat _grayImage = new();

    public HoughScanner(string imagePath)
    {
        _originalImage = Cv2.ImRead(imagePath);
        Cv2.CvtColor(_originalImage, _grayImage, ColorConversionCodes.BGR2GRAY);
    }

    public CircleSegment[] FindBubbles(uint questionCount = 15, uint answerCount = 5)
    {
        var allCircles = Cv2.HoughCircles(_grayImage, HoughModes.Gradient, 1, 20, 100, 30, 10, 40);
        
        var uv = GetUv();
        var circles = RecoverMatrix(allCircles, uv, questionCount, answerCount);

        return circles;
    }

    private Point2f[] GetUv()
    {
        var u = new Point2f(0, 0);
        var v = new Point2f(0, 0);
        
        var dict = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.Dict4X4_50);
        var parameters = new DetectorParameters();
        CvAruco.DetectMarkers(_grayImage, dict, out var markers, out _, parameters, out _);

        foreach (var corners in markers)
        {
            var topLeft = corners[0];
            var topRight = corners[1];
            var bottomLeft = corners[3];

            u +=  topRight - topLeft;
            v += bottomLeft - topLeft;
        }
        
        return [Normalize(u), Normalize(v)];
    }

    private static CircleSegment[] RecoverMatrix(CircleSegment[] circles, Point2f[] uv, uint questionCount, uint answerCount)
    {
        var medianRadius = circles.OrderBy(x => x.Radius).ElementAt(circles.Length / 2).Radius;
        
        var rows = new List<List<CircleSegment>>(); // Aligned to U, only a "row" if the image is upright
        var cols = new List<List<CircleSegment>>(); // Aligned to V, same as above
        
        var u = uv[0];
        var v = uv[1];

        // Organize circles into rows and columns based on if their offsets from the center align with U or V
        foreach (var circle in circles)
        {
            bool foundRow = false;
            bool foundCol = false;

            foreach (var row in rows)
            {
                bool inNear = double.Abs(u.DotProduct(Normalize(row[0].Center - circle.Center))) > CenterNearnessDotThreshold;
                if (!inNear) continue;
                row.Add(circle);
                foundRow = true;
            }
            
            foreach (var col in cols)
            {
                bool inNear = double.Abs(v.DotProduct(Normalize(col[0].Center - circle.Center))) > CenterNearnessDotThreshold;
                if (!inNear) continue;
                col.Add(circle);
                foundCol = true;
            }
            
            if (!foundRow)
            {
                var newRow = new List<CircleSegment> { circle };
                rows.Add(newRow);
            }
            
            if (!foundCol)
            {
                var newCol = new List<CircleSegment> { circle };
                cols.Add(newCol);
            }
        }

        rows = rows.OrderByDescending(r => r.Count).Take((int)questionCount).ToList();
        cols = cols.OrderByDescending(c => c.Count).Take((int)answerCount).ToList();
        
        var mendSegments = new List<Tuple<int, CircleSegment>>();

        // Ensure each row has the correct number of circles, filling in missing ones
        foreach (var row in rows)
        {
            if (row.Count >= answerCount) continue;
            // Fill missing circles in the row
            var availableCols = Enumerable.Range(0, (int)answerCount).ToList();
            foreach (var colId in row.Select(circle => cols.FindIndex(c => c.Contains(circle))).Where(colId => colId >= 0))
            {
                availableCols.Remove(colId);
            }
            var missingCols = availableCols.ToArray();
            foreach (var colIdx in missingCols)
            {
                var crossPoint = GetCrossPoint(row, cols[colIdx]);
                var newCircle = new CircleSegment
                {
                    Center = crossPoint,
                    Radius = medianRadius,
                };
                var tuple = new Tuple<int, CircleSegment>(rows.IndexOf(row), newCircle);
                mendSegments.Add(tuple);
            }
        }

        foreach (var (rowId, circle) in mendSegments)
        {
            rows[rowId].Add(circle);
        }
        
        // Sorting rows and columns to go from top-left to bottom-right
        foreach (var row in rows)
        {
            row.Sort(
                (a, b) => double.Sign(u.DotProduct(a.Center - b.Center))
            );
        }
        rows.Sort(
            (a, b) => double.Sign(v.DotProduct(a[0].Center - b[0].Center))
        );
        
        var finalCircles = (from row in rows from circle in row select circle).ToArray();
        return finalCircles;
    }
    
    private static Point2f Normalize(Point2f point)
    {
        float length = 1.0f / Length(point);
        return new Point2f(point.X * length, point.Y * length);
    }
    
    private static float Length(Point2f point)
    {
        return float.Sqrt(point.X * point.X + point.Y * point.Y);
    }

    private static Point2f GetCrossPoint(List<CircleSegment> pointsA, List<CircleSegment> pointsB)
    {
        var (mA, bA) = LeastSquaresGetLine(pointsA);
        var (mB, bB) = LeastSquaresGetLine(pointsB);
        float x = (bB - bA) / (mA - mB);
        float y = mA * x + bA;
        return new Point2f(x, y);
    }

    private static Tuple<float, float> LeastSquaresGetLine(List<CircleSegment> points)
    {
        var xSum = points.Select(c => c.Center.X).Sum();
        var ySum = points.Select(c => c.Center.Y).Sum();
        var xSquaredSum = points.Select(c => c.Center.X * c.Center.X).Sum();
        var xySum = points.Select(c => c.Center.X * c.Center.Y).Sum();
        var n = points.Count;
        
        var m = (n * xySum - xSum * ySum) / (n * xSquaredSum - xSum * xSum);
        var b = (ySum - m * xSum) / n;
        
        return new Tuple<float, float>(m, b);
    }
}