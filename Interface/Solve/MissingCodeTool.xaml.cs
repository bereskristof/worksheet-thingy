using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Scanner;

namespace Interface.Solve;

public partial class MissingCodeTool : Window
{
    public ScanResult FixedResult = new();
    
    public MissingCodeTool()
    {
        InitializeComponent();
    }

    public void Setup(Bitmap image, ScanResult scanResult)
    {
        PreviewImage.Source = ConvertBitmapToImageSource(image);
        ExamTextBox.Text = (scanResult.ExamCode.ToString() ?? "").Replace("-", "");
        CodeTextBox.Text = scanResult.UserCode ?? "";
    }
    
    private static ImageSource ConvertBitmapToImageSource(Bitmap bitmap)
    {
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
        memoryStream.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memoryStream;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        return bitmapImage;
    }

    private void EnterButton_OnClick(object sender, RoutedEventArgs e)
    {
        // TODO: Validate code
        string examCode = ExamTextBox.Text.Trim();
        if (CodeScanner.IsUuid(examCode)) 
        {
            FixedResult.ExamCode = Guid.Parse(examCode);
        }
        else
        {
            MessageBox.Show("Entered exam code is not valid!", "Error", MessageBoxButton.OK, MessageBoxImage.Error); // TODO: Localize
            return;
        }
        
        string userCode = CodeTextBox.Text.Trim();
        if (CodeScanner.IsNeptunCode(userCode))
        {
            FixedResult.UserCode = userCode;
        }
        else
        {
            MessageBox.Show("Entered Neptun code is not valid!", "Error", MessageBoxButton.OK, MessageBoxImage.Error); // TODO: Localize
            return;
        }

        if (!CodeScanner.IsValidScanResult(FixedResult))
        {
            MessageBox.Show("Scan result is not valid!", "Error", MessageBoxButton.OK, MessageBoxImage.Error); // TODO: Localize
            return;
        }
        
        FixedResult.CurrentState = ScanResult.State.ManuallyCorrected;
        Close();
    }

    private void SkipButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}