using System.ComponentModel;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Docnet.Core.Models;
using Storage;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

using WorkArgs = System.Tuple<string, string, string>;
using PreviewTuple = System.Tuple<string?, Interface.Export.ExportPage.PageData[], double, double, string>;

namespace Interface.Export;

public partial class ExportPage : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    
    public struct PageData
    {
        public byte[] Data;
        public int Width;
        public int Height;
        public double Y;
    }
    
    private readonly ScaleTransform _scaleTransform = new();
    
    private int _dimX = 1080;
    private int _dimY = 1920;
    
    private uint _pageCount = 1;
    public uint PageCount
    {
        get => _pageCount;
        set
        {
            if (_pageCount == value) return;
            _pageCount = value;
            OnPropertyChanged(nameof(PageCount));
        }
    }
    
    private byte _answerCount = 5;
    public byte AnswerCount
    {
        get => _answerCount;
        set
        {
            if (_answerCount == value) return;
            _answerCount = value;
            OnPropertyChanged(nameof(AnswerCount));
        }
    }
    
    public ExportPage()
    {
        InitializeComponent();
        var pageCountBinding = new Binding("PageCount")
        {
            Source = this,
            Path = new PropertyPath("PageCount"), 
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        var answerCountBinding = new Binding("AnswerCount")
        {
            Source = this,
            Path = new PropertyPath("AnswerCount"),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        BindingOperations.SetBinding(AmountBox, TextBox.TextProperty, pageCountBinding);
        BindingOperations.SetBinding(AnswerCountBox, TextBox.TextProperty, answerCountBinding);
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
        BackgroundWorker previewWorker = new();
        previewWorker.DoWork += BackgroundLoader_DoWork;
        previewWorker.RunWorkerCompleted += BackgroundLoader_RunWorkerCompleted;
        var title = TitleBox.Text.Trim();
        var author = AuthorBox.Text.Trim();
        var date = DateBox.Text.Trim();
        var args = new WorkArgs(title, author, date);
        previewWorker.RunWorkerAsync(argument: args);
    }
    
    private void BackgroundLoader_DoWork(object? sender, DoWorkEventArgs e)
    {
        var (title, author, date) = (WorkArgs)e.Argument!;
        
        // Create a PDF to preview
        var builder = MultiExamBuilder.BuildNExams(1, Bindings.Instance.SheetRoot, AnswerCount, title, author, date);
        string pdfPath;
        pdfPath = MultiExamBuilder.TryExportPdf(builder, out var success, out var errorMessage);
        if (!success)
        {
            e.Result = new PreviewTuple(errorMessage, [], 0, 0, pdfPath);
            return;
        }
        
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

        e.Result = new PreviewTuple(null, pages, canvasYOffset, canvasXOffset, pdfPath);
    }

    private void BackgroundLoader_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        var (error, pages, canvasYOffset, canvasXOffset, pdfPath) = (PreviewTuple)e.Result!;
        if (error != null)
        {
            MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error); // TODO: Localize
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
        MultiExamBuilder.CleanUp(pdfPath);
    }

    private void RescalePreview(double mult)
    {
        _scaleTransform.ScaleX *= mult;
        _scaleTransform.ScaleY *= mult;
        PreviewScroll.Width *= mult;
        PreviewScroll.Height *= mult;
    }

    private void ExportButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new ExportingWindow
        {
            Owner = Window.GetWindow(this),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Title = "Exporting PDF", // TODO: Localize
        };
        var title = TitleBox.Text.Trim();
        var author = AuthorBox.Text.Trim();
        var date = DateBox.Text.Trim();
        window.ExportNPages(Bindings.Instance.SheetRoot, PageCount, 5, title, author, date);
        window.ShowDialog();
    }
}