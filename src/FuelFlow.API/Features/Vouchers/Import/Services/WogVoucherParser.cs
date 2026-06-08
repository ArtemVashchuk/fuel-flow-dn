using System.Globalization;
using System.Text.RegularExpressions;
using FuelFlow.Features.Vouchers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace FuelFlow.Features.Vouchers.Import;

public sealed class WogVoucherParser : IVoucherProviderParser
{
    private static readonly Regex LitersRegex = new(@"(\d+(?:[.,]\d+)?)\s*(?:л|l)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DateRegex = new(@"\b(\d{2})[./-](\d{2})[./-](\d{4})\b", RegexOptions.Compiled);
    private static readonly Regex VoucherNumberRegex = new(@"\b\d{16,20}\b", RegexOptions.Compiled);

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

        foreach (var region in context.VoucherRegions)
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

            var fuelType = ParseFuelType(rawText);
            var liters = ParseLiters(rawText);
            var expirationDate = ParseExpirationDate(rawText);
            var voucherNumber = ParseVoucherNumber(rawText);

            string? qrPayload = null;
            try
            {
                using var croppedImage = context.PageRender.Image.Clone(x => x.Crop(region.Bounds));
                qrPayload = context.QrDecoder.Decode(croppedImage);
            }
            catch (Exception)
            {
            }

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

    private static FuelType ParseFuelType(string text)
    {
        var normalized = text.ToUpperInvariant()
            .Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("-", "");

        if (normalized.Contains("ДП") || normalized.Contains("DIESEL"))
            return FuelType.Diesel;
        if (normalized.Contains("А92") || normalized.Contains("A92") || normalized.Contains("А95") || normalized.Contains("A95"))
            return FuelType.Gasoline95;
        if (normalized.Contains("А98") || normalized.Contains("A98"))
            return FuelType.Gasoline98;
        if (normalized.Contains("ГАЗ") || normalized.Contains("GAZ") || normalized.Contains("LPG"))
            return FuelType.LPG;

        return FuelType.Unknown;
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
}
