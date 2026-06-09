using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZXing;
using ZXing.ImageSharp;
using ZXing.QrCode;

namespace FuelFlow.Features.Vouchers.Import;

public sealed class QrGenerator : IQrGenerator
{
    public string GenerateQrCode(string payload, int width = 300, int height = 300)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return string.Empty;

        var hints = new Dictionary<EncodeHintType, object>
        {
            { EncodeHintType.ERROR_CORRECTION, ZXing.QrCode.Internal.ErrorCorrectionLevel.H },
            { EncodeHintType.QR_VERSION, 1 },
            { EncodeHintType.CHARACTER_SET, "UTF-8" },
            { EncodeHintType.DISABLE_ECI, true }
        };

        var options = new QrCodeEncodingOptions
        {
            Width = width,
            Height = height,
            Margin = 0
        };
        foreach (var hint in hints)
            options.Hints[hint.Key] = hint.Value;

        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = options
        };

        var pixelData = writer.Write(payload);
        using var image = Image.LoadPixelData<Bgra32>(pixelData.Pixels, pixelData.Width, pixelData.Height);

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return Convert.ToBase64String(ms.ToArray());
    }
}

public interface IQrGenerator
{
    string GenerateQrCode(string payload, int width = 300, int height = 300);
}