using System.Text;
using AVFoundation;
using CoreGraphics;
using Microsoft.Maui.Graphics.Platform;
using Vision;

namespace BarcodeScanning;

public static partial class Extensions
{
    public static BarcodeResult AsBarcodeResult(this VNBarcodeObservation barcode, AVCaptureVideoPreviewLayer? previewLayer = null)
    {
        return new BarcodeResult()
        {
            BarcodeType = BarcodeTypes.Unknown,
            BarcodeFormat = Methods.ConvertFromIOSFormats(barcode.Symbology),
            DisplayValue = barcode.PayloadStringValue ?? string.Empty,
            RawValue = barcode.PayloadStringValue ?? string.Empty,
            RawBytes = OperatingSystem.IsIOSVersionAtLeast(17) ? [.. barcode.PayloadData ?? []] : Encoding.ASCII.GetBytes(barcode.PayloadStringValue ?? string.Empty),
            PreviewBoundingBox =  previewLayer?.MapToLayerCoordinates(InvertY(barcode.BoundingBox)).AsRectangleF() ?? RectF.Zero,
            ImageBoundingBox = barcode.BoundingBox.AsRectangleF()
        }; 
    }

    private static CGRect InvertY(CGRect rect)
    {
        return new CGRect(rect.X, 1 - rect.Y - rect.Height, rect.Width, rect.Height);
    }
}