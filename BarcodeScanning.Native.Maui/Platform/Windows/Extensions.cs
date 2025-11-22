using ZXingCpp;

namespace BarcodeScanning;

public static partial class Extensions
{
    public static BarcodeResult AsBarcodeResult(this Barcode barcode, Size? imageSize = null, Size? previewSize = null)
    {
        RectF imageRect = CalculateBoundingBox(barcode.Position);
        RectF previewRect = RectF.Zero;

        if (imageSize.HasValue && previewSize.HasValue && imageRect != RectF.Zero)
        {
            previewRect = TransformToPreviewCoordinates(imageRect, imageSize.Value, previewSize.Value);
        }

        return new BarcodeResult
        {
            BarcodeFormat = Methods.ConvertFromZxingFormats(barcode.Format),
            DisplayValue = barcode.Text,
            RawValue = barcode.Text,
            RawBytes = barcode.Bytes ?? [],
            BarcodeType = BarcodeTypes.Unknown,
            ImageBoundingBox = imageRect,
            PreviewBoundingBox = previewRect
        };
    }

    private static RectF CalculateBoundingBox(Position position)
    {
        //Ensures rotated barcodes are also fully contained in the bounding box
        var leftX = Math.Min(Math.Min(position.TopLeft.X, position.BottomLeft.X), Math.Min(position.TopRight.X, position.BottomRight.X));
        var topY = Math.Min(Math.Min(position.TopLeft.Y, position.TopRight.Y), Math.Min(position.BottomLeft.Y, position.BottomRight.Y));

        var rightX = Math.Max(Math.Max(position.TopRight.X, position.BottomRight.X), Math.Max(position.TopLeft.X, position.BottomLeft.X));
        var bottomY = Math.Max(Math.Max(position.BottomLeft.Y, position.BottomRight.Y), Math.Max(position.TopLeft.Y, position.TopRight.Y));

        return new RectF(leftX, topY, rightX - leftX, bottomY - topY);
    }

    private static RectF TransformToPreviewCoordinates(RectF imageRect, Size imageSize, Size previewSize)
    {
        // Different scaling between preview and image
        var scaleX = (float)(previewSize.Width / imageSize.Width);
        var scaleY = (float)(previewSize.Height / imageSize.Height);

        // Ensure the the bounding box has the right size
        var scale = Math.Max(scaleX, scaleY);

        var offsetX = (float)((previewSize.Width - (imageSize.Width * scale)) / 2);
        var offsetY = (float)((previewSize.Height - (imageSize.Height * scale)) / 2);

        return new RectF(
            (imageRect.X * scale) + offsetX,
            (imageRect.Y * scale) + offsetY,
            imageRect.Width * scale,
            imageRect.Height * scale
        );
    }
}
