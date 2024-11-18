using AndroidX.Camera.View.Transform;
using Microsoft.Maui.Graphics.Platform;
using Xamarin.Google.MLKit.Vision.Barcode.Common;

namespace BarcodeScanning;

public static partial class Extensions
{
    internal static BarcodeResult AsBarcodeResult(this Barcode barcode, CoordinateTransform? coordinateTransform = null)
    {
        RectF imageRect, previewRect;
        if (barcode.BoundingBox is null)
        {
            imageRect = RectF.Zero;
            previewRect = RectF.Zero;
        }
        else
        {
            using var barcodeBox = barcode.BoundingBox.AsRectF();
            imageRect = barcodeBox.AsRectangleF();

            if (coordinateTransform is null)
            {
                previewRect = RectF.Zero;
            }
            else
            {
                coordinateTransform.MapRect(barcodeBox);
                previewRect = barcodeBox.AsRectangleF();
            }
        }

        return new BarcodeResult()
        {
            BarcodeType = Methods.ConvertBarcodeResultTypes(barcode.ValueType),
            BarcodeFormat = (BarcodeFormats)barcode.Format,
            DisplayValue = barcode.DisplayValue ?? string.Empty,
            RawValue = barcode.RawValue ?? string.Empty,
            RawBytes = barcode.GetRawBytes() ?? [],
            PreviewBoundingBox = previewRect,
            ImageBoundingBox = imageRect
        };
    }
}