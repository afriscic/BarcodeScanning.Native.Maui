namespace BarcodeScanning;

public class BarcodeResult : IEquatable<BarcodeResult>
{
    public BarcodeTypes BarcodeType { get; set; }
    public BarcodeFormats BarcodeFormat { get; set; }
    public string DisplayValue { get; set; }
    public string RawValue { get; set; }
    public byte[] RawBytes { get; set; }
    public RectF PreviewBoundingBox { get; set; }
    public RectF ImageBoundingBox { get; set; }

    public bool Equals(BarcodeResult other)
    {
        if (other is null)
            return false;

        if (this.DisplayValue == other.DisplayValue && this.ImageBoundingBox.IntersectsWith(other.ImageBoundingBox))
            return true;
        else
            return false;
    }
    public override bool Equals(object obj)
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
        return this.DisplayValue.GetHashCode();
    }
}