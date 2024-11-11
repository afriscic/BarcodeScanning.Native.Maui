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

        if (!string.IsNullOrEmpty(this.RawValue))
        {
            if (this.RawValue == other.RawValue && this.ImageBoundingBox.IntersectsWith(other.ImageBoundingBox))
                return true;
            else
                return false;
        }
        else
        {
            if (this.DisplayValue == other.DisplayValue && this.ImageBoundingBox.IntersectsWith(other.ImageBoundingBox))
                return true;
            else
                return false;
        }

    }
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        else if (obj is not BarcodeResult barcode)
            return false;
        else
            return base.Equals(barcode);
    }
    public override int GetHashCode()
    {
        if (!string.IsNullOrEmpty(this.RawValue))
            return this.RawValue.GetHashCode();
        else
            return this.DisplayValue.GetHashCode();
    }
}