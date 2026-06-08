namespace FuelFlow.Features.Vouchers;

public class FuelVoucher
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = null!;
    public FuelType FuelType { get; set; }
    public decimal Liters { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public string VoucherNumber { get; set; } = null!;
    public string QrPayload { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}
