using AVFoundation;
using CoreGraphics;
using Microsoft.Maui.Graphics.Platform;
using System.Text;
using Vision;

namespace BarcodeScanning;

public static partial class Extensions
{
    public static BarcodeResult AsBarcodeResult(this VNBarcodeObservation barcode, AVCaptureVideoPreviewLayer? previewLayer = null)
    {
        var box = barcode.BoundingBox;

        #pragma warning disable CA1422
        // TODO look for a fix for this issue
        // Rotate bounding box because video nad UI orientation is Portrait on macOS > 26
        if (OperatingSystem.IsMacCatalyst() && previewLayer?.Connection?.VideoOrientation == AVCaptureVideoOrientation.Portrait)
            box = new CGRect(1 - box.Y - box.Height, box.X, box.Height, box.Width);
        #pragma warning restore CA1422

        // Invert Y axis
        box = new CGRect(box.X, 1 - box.Y - box.Height, box.Width, box.Height);

        return new BarcodeResult()
        {
            BarcodeType = BarcodeTypes.Unknown,
            BarcodeFormat = Methods.ConvertFromIOSFormats(barcode.Symbology),
            DisplayValue = barcode.PayloadStringValue ?? string.Empty,
            RawValue = barcode.PayloadStringValue ?? string.Empty,
            RawBytes = OperatingSystem.IsIOSVersionAtLeast(17) ? barcode.PayloadData?.ToArray() ?? [] : Encoding.ASCII.GetBytes(barcode.PayloadStringValue ?? string.Empty),
            PreviewBoundingBox =  previewLayer?.MapToLayerCoordinates(box).AsRectangleF() ?? RectF.Zero,
            ImageBoundingBox = barcode.BoundingBox.AsRectangleF()
        }; 
    }
}