using AVFoundation;
using CoreGraphics;
using CoreImage;
using Foundation;
using Microsoft.Maui.Graphics.Platform;
using System.Text;
using UIKit;
using Vision;

namespace BarcodeScanning;

public static partial class Methods
{
    public static async Task<HashSet<BarcodeResult>> ScanFromImageAsync(byte[] imageArray)
        => await ProcessBitmapAsync(UIImage.LoadFromData(NSData.FromArray(imageArray)));
    public static async Task<HashSet<BarcodeResult>> ScanFromImageAsync(FileResult file)
        => await ProcessBitmapAsync(UIImage.LoadFromData(NSData.FromStream(await file.OpenReadAsync())));
    public static async Task<HashSet<BarcodeResult>> ScanFromImageAsync(string url)
        => await ProcessBitmapAsync(UIImage.LoadFromData(NSData.FromUrl(new NSUrl(url))));
    public static async Task<HashSet<BarcodeResult>> ScanFromImageAsync(Stream stream)
        => await ProcessBitmapAsync(UIImage.LoadFromData(NSData.FromStream(stream)));
    private static async Task<HashSet<BarcodeResult>> ProcessBitmapAsync(UIImage image)
    {
        if (image is null)
            return null;
        
        VNBarcodeObservation[] observations = null;
        using var barcodeRequest = new VNDetectBarcodesRequest((request, error) => {
            if (error is null)
                observations = request.GetResults<VNBarcodeObservation>();
        });
        using var handler = new VNImageRequestHandler(image.CGImage, new NSDictionary());
        await Task.Run(() => handler.Perform([barcodeRequest], out _));
        var barcodeResults = new HashSet<BarcodeResult>();
        ProcessBarcodeResult(observations, barcodeResults);
        return barcodeResults;
    }

    internal static void ProcessBarcodeResult(VNBarcodeObservation[] inputResults, HashSet<BarcodeResult> outputResults, AVCaptureVideoPreviewLayer previewLayer = null)
    {
        if (inputResults is null || inputResults.Length == 0)
            return;
        
        lock (outputResults)
        {
            foreach (var barcode in inputResults)
            {
                outputResults.Add(new BarcodeResult()
                {
                    BarcodeType = BarcodeTypes.Unknown,
                    BarcodeFormat = ConvertFromIOSFormats(barcode.Symbology),
                    DisplayValue = barcode.PayloadStringValue,
                    RawValue = barcode.PayloadStringValue,
                    RawBytes = GetRawBytes(barcode) ?? Encoding.ASCII.GetBytes(barcode.PayloadStringValue),
                    PreviewBoundingBox =  previewLayer?.MapToLayerCoordinates(InvertY(barcode.BoundingBox)).AsRectangleF() ?? RectF.Zero,
                    ImageBoundingBox = barcode.BoundingBox.AsRectangleF()
                });
            };
        }
    }

    internal static NSString GetBestSupportedPreset(AVCaptureSession captureSession, CaptureQuality quality)
    {
        ArgumentNullException.ThrowIfNull(captureSession);
                
        while (!captureSession.CanSetSessionPreset(SessionPresetTranslator(quality)) && quality != CaptureQuality.Low)
        {
            quality -= 1;
        }

        return SessionPresetTranslator(quality);
    }

    internal static VNBarcodeSymbology[] SelectedSymbologies(BarcodeFormats barcodeFormats)
    {
        if (barcodeFormats.HasFlag(BarcodeFormats.All))
            return [];

        var symbologiesList = new List<VNBarcodeSymbology>();

        if (barcodeFormats.HasFlag(BarcodeFormats.Code128))
            symbologiesList.Add(VNBarcodeSymbology.Code128);
        if (barcodeFormats.HasFlag(BarcodeFormats.Code39))
        {
            symbologiesList.Add(VNBarcodeSymbology.Code39);
            symbologiesList.Add(VNBarcodeSymbology.Code39Checksum);
            symbologiesList.Add(VNBarcodeSymbology.Code39FullAscii);
            symbologiesList.Add(VNBarcodeSymbology.Code39FullAsciiChecksum);
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.Code93))
        {
            symbologiesList.Add(VNBarcodeSymbology.Code93);
            symbologiesList.Add(VNBarcodeSymbology.Code93i);
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.CodaBar))
            symbologiesList.Add(VNBarcodeSymbology.Codabar);
        if (barcodeFormats.HasFlag(BarcodeFormats.DataMatrix))
            symbologiesList.Add(VNBarcodeSymbology.DataMatrix);
        if (barcodeFormats.HasFlag(BarcodeFormats.Ean13))
            symbologiesList.Add(VNBarcodeSymbology.Ean13);
        if (barcodeFormats.HasFlag(BarcodeFormats.Ean8))
            symbologiesList.Add(VNBarcodeSymbology.Ean8);
        if (barcodeFormats.HasFlag(BarcodeFormats.Itf))
            symbologiesList.Add(VNBarcodeSymbology.Itf14);
        if (barcodeFormats.HasFlag(BarcodeFormats.QRCode))
            symbologiesList.Add(VNBarcodeSymbology.QR);
        if (barcodeFormats.HasFlag(BarcodeFormats.Upca))
            symbologiesList.Add(VNBarcodeSymbology.Ean13);
        if (barcodeFormats.HasFlag(BarcodeFormats.Upce))
            symbologiesList.Add(VNBarcodeSymbology.Upce);
        if (barcodeFormats.HasFlag(BarcodeFormats.Pdf417))
            symbologiesList.Add(VNBarcodeSymbology.Pdf417);
        if (barcodeFormats.HasFlag(BarcodeFormats.Aztec))
            symbologiesList.Add(VNBarcodeSymbology.Aztec);
        if (barcodeFormats.HasFlag(BarcodeFormats.MicroQR))
            symbologiesList.Add(VNBarcodeSymbology.MicroQR);
        if (barcodeFormats.HasFlag(BarcodeFormats.MicroPdf417))
            symbologiesList.Add(VNBarcodeSymbology.MicroPdf417);
        if (barcodeFormats.HasFlag(BarcodeFormats.I2OF5))
        {
            symbologiesList.Add(VNBarcodeSymbology.I2OF5);
            symbologiesList.Add(VNBarcodeSymbology.I2OF5Checksum);
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.GS1DataBar))
        {
            symbologiesList.Add(VNBarcodeSymbology.GS1DataBar);
            symbologiesList.Add(VNBarcodeSymbology.GS1DataBarLimited);
            symbologiesList.Add(VNBarcodeSymbology.GS1DataBarExpanded);
        }

        return [.. symbologiesList];
    }

    private static BarcodeFormats ConvertFromIOSFormats(VNBarcodeSymbology symbology)
    {
        return symbology switch
        {
            VNBarcodeSymbology.Aztec => BarcodeFormats.Aztec,
            VNBarcodeSymbology.Code39 => BarcodeFormats.Code39,
            VNBarcodeSymbology.Code39Checksum => BarcodeFormats.Code39,
            VNBarcodeSymbology.Code39FullAscii => BarcodeFormats.Code39,
            VNBarcodeSymbology.Code39FullAsciiChecksum => BarcodeFormats.Code39,
            VNBarcodeSymbology.Code93 => BarcodeFormats.Code93,
            VNBarcodeSymbology.Code93i => BarcodeFormats.Code93,
            VNBarcodeSymbology.Code128 => BarcodeFormats.Code128,
            VNBarcodeSymbology.DataMatrix => BarcodeFormats.DataMatrix,
            VNBarcodeSymbology.Ean8 => BarcodeFormats.Ean8,
            VNBarcodeSymbology.Ean13 => BarcodeFormats.Ean13,
            VNBarcodeSymbology.I2OF5 => BarcodeFormats.I2OF5,
            VNBarcodeSymbology.I2OF5Checksum => BarcodeFormats.I2OF5,
            VNBarcodeSymbology.Itf14 => BarcodeFormats.Itf,
            VNBarcodeSymbology.Pdf417 => BarcodeFormats.Pdf417,
            VNBarcodeSymbology.QR => BarcodeFormats.QRCode,
            VNBarcodeSymbology.Upce => BarcodeFormats.Upce,
            VNBarcodeSymbology.Codabar => BarcodeFormats.CodaBar,
            VNBarcodeSymbology.GS1DataBar => BarcodeFormats.GS1DataBar,
            VNBarcodeSymbology.GS1DataBarExpanded => BarcodeFormats.GS1DataBar,
            VNBarcodeSymbology.GS1DataBarLimited => BarcodeFormats.GS1DataBar,
            VNBarcodeSymbology.MicroPdf417 => BarcodeFormats.MicroPdf417,
            VNBarcodeSymbology.MicroQR => BarcodeFormats.MicroQR,
            _ => BarcodeFormats.None
        };
    }

    private static CGRect InvertY(CGRect rect)
    {
        return new CGRect(rect.X, 1 - rect.Y - rect.Height, rect.Width, rect.Height);
    }

    private static byte[] GetRawBytes(VNBarcodeObservation barcodeObservation)
    {
        return barcodeObservation.Symbology switch
        {
            VNBarcodeSymbology.QR => ((CIQRCodeDescriptor)barcodeObservation.BarcodeDescriptor)?.ErrorCorrectedPayload?.ToArray(),
            VNBarcodeSymbology.Aztec => ((CIAztecCodeDescriptor)barcodeObservation.BarcodeDescriptor)?.ErrorCorrectedPayload?.ToArray(),
            VNBarcodeSymbology.Pdf417 => ((CIPdf417CodeDescriptor)barcodeObservation.BarcodeDescriptor)?.ErrorCorrectedPayload?.ToArray(),
            VNBarcodeSymbology.DataMatrix => ((CIDataMatrixCodeDescriptor)barcodeObservation.BarcodeDescriptor)?.ErrorCorrectedPayload?.ToArray(),
            _ => null
        };
    }

    private static NSString SessionPresetTranslator(CaptureQuality quality)
    {
        return quality switch
        {
            CaptureQuality.Low => AVCaptureSession.Preset640x480,
            CaptureQuality.Medium => AVCaptureSession.Preset1280x720,
            CaptureQuality.High => AVCaptureSession.Preset1920x1080,
            CaptureQuality.Highest => AVCaptureSession.Preset3840x2160,
            _ => AVCaptureSession.Preset1280x720
        };
    }
}
