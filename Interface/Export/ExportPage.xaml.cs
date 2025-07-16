using System.ComponentModel;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Docnet.Core.Models;
using Storage;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Interface.Export;

public partial class ExportPage
{ 
    struct PageData
    {
        public byte[] Data;
        public int Width;
        public int Height;
        public double Y;
    }
    
    private readonly ScaleTransform _scaleTransform = new();
    
    private int _dimX = 1080;
    private int _dimY = 1920;
    
    public ExportPage()
    {
        InitializeComponent();
        PreviewScroll.RenderTransform = _scaleTransform;
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e) 
        => CreatePdfPreview();

    private void ZoomOut_OnClick(object sender, RoutedEventArgs e)
        => RescalePreview(0.8);

    private void ZoomIn_OnClick(object sender, RoutedEventArgs e)
        => RescalePreview(1.25);

    private void ScaleSmall_OnSelect(object sender, RoutedEventArgs e)
    {
        _dimX = 540;
        _dimY = 960;
    }

    private void ScaleMedium_OnSelect(object sender, RoutedEventArgs e)
    {
        _dimX = 1080;
        _dimY = 1920;
    }

    private void ScaleLarge_OnSelect(object sender, RoutedEventArgs e)
    {
        _dimX = 2160;
        _dimY = 3840;
    }

    private void CreatePdfPreview()
    {
        CreateButton.IsEnabled = false;
        PreviewProgressBar.Visibility = Visibility.Visible;
        PreviewScroll.Children.Clear();
        _scaleTransform.ScaleX = 1.0;
        _scaleTransform.ScaleY = 1.0;
        BackgroundWorker asyncImageLoader = new();
        asyncImageLoader.DoWork += BackgroundLoader_DoWork;
        asyncImageLoader.RunWorkerCompleted += BackgroundLoader_RunWorkerCompleted;
        asyncImageLoader.RunWorkerAsync();
    }
    
    private void BackgroundLoader_DoWork(object? sender, DoWorkEventArgs e)
    {
        // Create a PDF to preview
        var examBuilder = new ExamBuilder(Bindings.Instance.SheetRoot, 5); // TODO: Get from UI
        try
        {
            examBuilder.ExportPdf();
        }
        catch (TimeoutException)
        {
            e.Result = new Tuple<bool, PageData[], double, double, ExamBuilder>(true, [], 0, 0, examBuilder);
            examBuilder.CleanUpError();
            return;
        }

        string pdfPath = examBuilder.GetExportPath();
        
        // Preview the PDF file
        using var doclib = Docnet.Core.DocLib.Instance;
        using var reader = doclib.GetDocReader(pdfPath, new PageDimensions(_dimX, _dimY));

        var pageCount = reader.GetPageCount();

        var canvasYOffset = 20;
        var canvasXOffset = 0;
        
        PageData[] pages = new PageData[pageCount];
        
        for (var i = 0; i < pageCount; i++)
        {
            using var pageReader = reader.GetPageReader(i);
            var page = pageReader.GetImage();
            
            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();
            
            using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            
            var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            System.Runtime.InteropServices.Marshal.Copy(page, 0, bmpData.Scan0, page.Length);
            bmp.UnlockBits(bmpData);
            
            var stream = new MemoryStream();
            bmp.Save(stream, ImageFormat.Png);
            
            var pageData = new PageData
            {
                Data = stream.ToArray(),
                Width = width,
                Height = height,
                Y = canvasYOffset,
            };
            pages[i] = pageData;
            
            canvasYOffset += height + 22; // 20px margin + 2px border
            canvasXOffset = int.Max(width + 2, canvasXOffset);
        }

        e.Result = new Tuple<bool, PageData[], double, double, ExamBuilder>(false, pages, canvasYOffset, canvasXOffset, examBuilder);
    }

    private void BackgroundLoader_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        var (failed, pages, canvasYOffset, canvasXOffset, examBuilder) = (Tuple<bool, PageData[], double, double, ExamBuilder>)e.Result!; // <--- !!!
        if (failed)
        {
            MessageBox.Show("Failed to load PDF preview. Please check the log for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); // TODO: Localize
            PreviewProgressBar.Visibility = Visibility.Collapsed;
            CreateButton.IsEnabled = true;
            return;
        }

        foreach (var page in pages)
        {
            var previewImage = new LatexPagePreview();
            var stream = new MemoryStream(); // TODO: Probably useless, added for debugging some other issue
            stream.Write(page.Data, 0, page.Data.Length);
            previewImage.MainImage.Source = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            previewImage.MainImage.Width = page.Width;
            previewImage.MainImage.Height = page.Height;
            Canvas.SetTop(previewImage, page.Y);
            Canvas.SetLeft(previewImage, 20);
            PreviewScroll.Children.Add(previewImage);
        }

        PreviewScroll.Width = canvasXOffset + 40; // 20 px margin on the right
        PreviewScroll.Height = canvasYOffset;
        PreviewProgressBar.Visibility = Visibility.Collapsed;
        CreateButton.IsEnabled = true;
        examBuilder.CleanUp();
    }

    private void RescalePreview(double mult)
    {
        _scaleTransform.ScaleX *= mult;
        _scaleTransform.ScaleY *= mult;
        PreviewScroll.Width *= mult;
        PreviewScroll.Height *= mult;
    }
}