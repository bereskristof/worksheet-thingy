using System.Drawing;
using System.Drawing.Imaging;
using QRCoder;

namespace Storage;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class QrBuilder
{
    private const int Width = 333;
    private const int Height = 333;
    
    private readonly Image _image = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
    
    public QrBuilder(Guid uuid) // Required reading: https://stackoverflow.com/questions/9195551/
    {
        using Graphics graphics = Graphics.FromImage(_image);
        graphics.Clear(Color.White); // TODO: Transparent background?
        
        using var codeGenerator = new QRCodeGenerator();
        using var qrCodeData = codeGenerator.CreateQrCode(uuid.ToByteArray(), QRCodeGenerator.ECCLevel.H);
        using var pngCode = new PngByteQRCode(qrCodeData);
        
        byte[] qrCodePng = pngCode.GetGraphic(9, Color.Black, Color.Transparent);
        Image qrCodeImage = Image.FromStream(new MemoryStream(qrCodePng));
        graphics.DrawImageUnscaled(qrCodeImage, 0, 0);
    }
    
    public void Save(string filePath)
    {
        _image.Save(filePath, ImageFormat.Png);
    }
}