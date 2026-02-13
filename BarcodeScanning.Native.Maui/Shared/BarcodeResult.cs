namespace BarcodeScanning;

public class BarcodeResult : IEquatable<BarcodeResult>
{
    public required BarcodeTypes BarcodeType { get; init; }
    public required BarcodeFormats BarcodeFormat { get; init; }
    public required string DisplayValue { get; init; }
    public required string RawValue { get; init; }
    public required byte[] RawBytes { get; init; }
    public required RectF PreviewBoundingBox { get; init; }
    public required RectF ImageBoundingBox { get; init; }

    public bool Equals(BarcodeResult? other)
    {
        if (other is null)
            return false;

        var value = !string.IsNullOrEmpty(RawValue) ? RawValue : DisplayValue;
        var otherValue = !string.IsNullOrEmpty(other.RawValue) ? other.RawValue : other.DisplayValue;

        return value == otherValue && ImageBoundingBox.IntersectsWith(other.ImageBoundingBox);
    }
    public override bool Equals(object? obj)
    {
        if (obj is BarcodeResult barcode)
            return Equals(barcode);
        else
            return false;
    }
    public override int GetHashCode()
    {
        var value = !string.IsNullOrEmpty(RawValue) ? RawValue : DisplayValue;
        return value.GetHashCode();
    }
}