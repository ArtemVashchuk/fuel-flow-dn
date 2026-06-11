using System.Globalization;
using System.Text.RegularExpressions;
using FuelFlow.Features.Vouchers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZXing;
using ZXing.ImageSharp;

namespace FuelFlow.Features.Vouchers.Import;

public sealed class WogVoucherParser : IVoucherProviderParser
{
    private static readonly Regex LitersRegex = new(@"(\d+(?:[.,]\d+)?)\s*(?:л|l)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DateRegex = new(@"\b(\d{2})[./-](\d{2})[./-](\d{4})\b", RegexOptions.Compiled);
    private static readonly Regex VoucherNumberRegex = new(@"\b\d{16,20}\b", RegexOptions.Compiled);
    private static readonly Regex FuelTypeRegex = new(
        @"\b(?<fuel>[АA]\s*[-–—]?\s*\d{2,3}(?:\s*\+)?|Д\s*[ПP]|ГАЗ)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool CanParse(ProviderDetectionContext context)
    {
        return context.Words.Any(w =>
            w.Text.Contains("WOG", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyCollection<ParsedVoucher>> ParseAsync(
        ProviderParseContext context,
        CancellationToken cancellationToken)
    {
        var parsedVouchers = new List<ParsedVoucher>();
        var regions = context.VoucherRegions.ToList();

        var qrResults = DecodeAllQrCodes(context.PageRender.Image);

        foreach (var region in regions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var voucherWords = context.PageRender.Words
                .Where(w => region.Contains(w.BoundingBox))
                .ToList();

            var lines = voucherWords
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 1))
                .OrderByDescending(g => g.Key)
                .Select(g => string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));

            var rawText = string.Join("\n", lines);

            var liters = ParseLiters(rawText);
            var expirationDate = ParseExpirationDate(rawText);
            var voucherNumber = ParseVoucherNumber(rawText);
            var fuelType = ParseFuelType(rawText);

            var qrPayload = MatchQrToRegion(region, qrResults);

            decimal confidence = 0;
            if (fuelType != FuelType.Unknown) confidence += 20;
            if (liters > 0) confidence += 20;
            if (expirationDate != default) confidence += 20;
            if (!string.IsNullOrEmpty(voucherNumber)) confidence += 20;
            if (!string.IsNullOrEmpty(qrPayload)) confidence += 20;

            parsedVouchers.Add(new ParsedVoucher
            {
                Provider = "WOG",
                FuelType = fuelType,
                Liters = liters,
                ExpirationDate = expirationDate,
                VoucherNumber = voucherNumber,
                QrPayload = qrPayload ?? string.Empty,
                Confidence = confidence,
                RawText = rawText
            });
        }

        return await Task.FromResult(parsedVouchers);
    }

        private static IReadOnlyList<Result> DecodeAllQrCodes(Image pageImage)
    {
        using var imageRgba32 = pageImage.CloneAs<Rgba32>();
        var reader = new ZXing.ImageSharp.BarcodeReader<Rgba32>
        {
            AutoRotate = true,
            Options = new ZXing.Common.DecodingOptions
            {
                TryHarder = true,
                TryInverted = true,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                ReturnCodabarStartEnd = true
            }
        };
        var results = reader.DecodeMultiple(imageRgba32) ?? Array.Empty<Result>();
        LogQrMetadata(results);
        return results;
    }

    private static void LogQrMetadata(IReadOnlyList<Result> results)
    {
        foreach (var r in results)
        {
            var version = r.ResultMetadata?.TryGetValue(ResultMetadataType.ISSUE_NUMBER, out var v) == true ? v : "?";
            var ecc = r.ResultMetadata?.TryGetValue(ResultMetadataType.ERROR_CORRECTION_LEVEL, out var e) == true ? e : "?";
            var mask = r.ResultMetadata?.TryGetValue(ResultMetadataType.ORIENTATION, out var m) == true ? m : "?";
            var mode = r.ResultMetadata?.TryGetValue(ResultMetadataType.PDF417_EXTRA_METADATA, out var mo) == true ? mo : "?";
            Console.WriteLine($"[QR META] Payload: {r.Text}, Version: {version}, ECC: {ecc}, Mask: {mask}, Mode: {mode}");
        }
    }

    private static string? MatchQrToRegion(VoucherRegion region, IReadOnlyList<Result> qrResults)
    {
        if (qrResults.Count == 0) return null;

        var regionCenterX = region.Bounds.X + region.Bounds.Width / 2;
        var regionCenterY = region.Bounds.Y + region.Bounds.Height / 2;

        Result? bestMatch = null;
        double bestDistance = double.MaxValue;

        foreach (var result in qrResults)
        {
            if (result.ResultPoints == null || result.ResultPoints.Length == 0) continue;

            var qrCenterX = result.ResultPoints.Average(p => p.X);
            var qrCenterY = result.ResultPoints.Average(p => p.Y);

            var dx = qrCenterX - regionCenterX;
            var dy = qrCenterY - regionCenterY;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestMatch = result;
            }
        }

        return bestMatch?.Text;
    }

    private static decimal ParseLiters(string text)
    {
        var match = LitersRegex.Match(text);
        if (match.Success)
        {
            var value = match.Groups[1].Value.Replace(',', '.');
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var liters))
                return liters;
        }
        return 0;
    }

    private static DateOnly ParseExpirationDate(string text)
    {
        var match = DateRegex.Match(text);
        if (match.Success)
        {
            var cleaned = $"{match.Groups[1].Value}.{match.Groups[2].Value}.{match.Groups[3].Value}";
            if (DateOnly.TryParseExact(cleaned, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;
        }
        return default;
    }

    private static string ParseVoucherNumber(string text)
    {
        var match = VoucherNumberRegex.Match(text);
        return match.Success ? match.Value : string.Empty;
    }

    private static FuelType ParseFuelType(string text)
    {
        var match = FuelTypeRegex.Match(text);
        if (!match.Success) return FuelType.Unknown;

        var raw = match.Groups["fuel"].Value
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("–", "")
            .Replace("—", "")
            .Replace('\u0410', 'A');

        return raw.ToUpperInvariant() switch
        {
            "A95" or "A95+" or "95" => FuelType.Gasoline95,
            "A98" or "98" => FuelType.Gasoline98,
            "ДП" or "Д" => FuelType.Diesel,
            "ГАЗ" => FuelType.LPG,
            _ => FuelType.Unknown
        };
    }
}
