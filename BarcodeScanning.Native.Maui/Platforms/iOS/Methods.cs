using AVFoundation;
using CoreGraphics;
using CoreImage;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;
using Vision;

namespace BarcodeScanning;

public static partial class Methods
{
    public static async Task<HashSet<BarcodeResult>> ScanFromImage(byte[] imageArray)
        => await ProcessImage(UIImage.LoadFromData(NSData.FromArray(imageArray)));
    public static async Task<HashSet<BarcodeResult>> ScanFromImage(FileResult file)
        => await ProcessImage(UIImage.LoadFromData(NSData.FromStream(await file.OpenReadAsync())));
    public static async Task<HashSet<BarcodeResult>> ScanFromImage(string url)
        => await ProcessImage(UIImage.LoadFromData(NSData.FromUrl(new NSUrl(url))));
    public static async Task<HashSet<BarcodeResult>> ScanFromImage(Stream stream)
        => await ProcessImage(UIImage.LoadFromData(NSData.FromStream(stream)));
    private static async Task<HashSet<BarcodeResult>> ProcessImage(UIImage image)
    {
        if (image is null)
            return null;
        
        VNBarcodeObservation[] observations = null;
        var barcodeRequest = new VNDetectBarcodesRequest((request, error) => {
            if (error is null)
                observations = request.GetResults<VNBarcodeObservation>();
        });
        var handler = new VNImageRequestHandler(image.CGImage, new NSDictionary());
        await Task.Run(() => handler.Perform([barcodeRequest], out _));
        return ProcessBarcodeResult(observations);
    }


    internal static HashSet<BarcodeResult> ProcessBarcodeResult(VNBarcodeObservation[] result, AVCaptureVideoPreviewLayer previewLayer = null)
    {
        var resultList = new HashSet<BarcodeResult>();

        if (result is null || result.Length == 0)
            return resultList;
        
        foreach (var barcode in result)
        {
            
            resultList.Add(new BarcodeResult()
            {
                BarcodeType = BarcodeTypes.Unknown,
                BarcodeFormat = ConvertFromIOSFormats(barcode.Symbology),
                DisplayValue = barcode.PayloadStringValue,
                RawValue = barcode.PayloadStringValue,
                RawBytes = GetRawBytes(barcode) ?? System.Text.Encoding.ASCII.GetBytes(barcode.PayloadStringValue),
                BoundingBox =  previewLayer?.MapToLayerCoordinates(InvertY(barcode.BoundingBox)).ToRectangle() ?? barcode.BoundingBox.ToRectangle()
            });
        };

        return resultList;
    }

    internal static VNBarcodeSymbology[] SelectedSymbologies(BarcodeFormats barcodeFormats)
    {
        if (barcodeFormats.HasFlag(BarcodeFormats.All))
            return null;

        var symbologiesList = new List<VNBarcodeSymbology>();

        if (barcodeFormats.HasFlag(BarcodeFormats.Aztec))
            symbologiesList.Add(VNBarcodeSymbology.Aztec);
        if (barcodeFormats.HasFlag(BarcodeFormats.CodaBar))
            symbologiesList.Add(VNBarcodeSymbology.Codabar);
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
        if (barcodeFormats.HasFlag(BarcodeFormats.Code128))
            symbologiesList.Add(VNBarcodeSymbology.Code128);
        if (barcodeFormats.HasFlag(BarcodeFormats.DataMatrix))
            symbologiesList.Add(VNBarcodeSymbology.DataMatrix);
        if (barcodeFormats.HasFlag(BarcodeFormats.Ean8))
            symbologiesList.Add(VNBarcodeSymbology.Ean8);
        if (barcodeFormats.HasFlag(BarcodeFormats.Ean13))
            symbologiesList.Add(VNBarcodeSymbology.Ean13);
        if (barcodeFormats.HasFlag(BarcodeFormats.GS1DataBar))
        {
            symbologiesList.Add(VNBarcodeSymbology.GS1DataBar);
            symbologiesList.Add(VNBarcodeSymbology.GS1DataBarLimited);
            symbologiesList.Add(VNBarcodeSymbology.GS1DataBarExpanded);
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.I2OF5))
        {
            symbologiesList.Add(VNBarcodeSymbology.I2OF5);
            symbologiesList.Add(VNBarcodeSymbology.I2OF5Checksum);
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.Itf))
            symbologiesList.Add(VNBarcodeSymbology.Itf14);
        if (barcodeFormats.HasFlag(BarcodeFormats.MicroQR))
            symbologiesList.Add(VNBarcodeSymbology.MicroQR);
        if (barcodeFormats.HasFlag(BarcodeFormats.MicroPdf417))
            symbologiesList.Add(VNBarcodeSymbology.MicroPdf417);
        if (barcodeFormats.HasFlag(BarcodeFormats.QRCode))
            symbologiesList.Add(VNBarcodeSymbology.QR);
        if (barcodeFormats.HasFlag(BarcodeFormats.Upce))
            symbologiesList.Add(VNBarcodeSymbology.Upce);

        return symbologiesList.ToArray();
    }

    private static BarcodeFormats ConvertFromIOSFormats(VNBarcodeSymbology symbology)
    {
        return symbology switch
        {
            VNBarcodeSymbology.Aztec => BarcodeFormats.Aztec,
            VNBarcodeSymbology.Codabar => BarcodeFormats.CodaBar,
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
            VNBarcodeSymbology.GS1DataBar => BarcodeFormats.GS1DataBar,
            VNBarcodeSymbology.GS1DataBarExpanded => BarcodeFormats.GS1DataBar,
            VNBarcodeSymbology.GS1DataBarLimited => BarcodeFormats.GS1DataBar,
            VNBarcodeSymbology.I2OF5 => BarcodeFormats.I2OF5,
            VNBarcodeSymbology.I2OF5Checksum => BarcodeFormats.I2OF5,
            VNBarcodeSymbology.Itf14 => BarcodeFormats.Itf,
            VNBarcodeSymbology.MicroPdf417 => BarcodeFormats.MicroPdf417,
            VNBarcodeSymbology.MicroQR => BarcodeFormats.MicroQR,
            VNBarcodeSymbology.Pdf417 => BarcodeFormats.Pdf417,
            VNBarcodeSymbology.QR => BarcodeFormats.QRCode,
            VNBarcodeSymbology.Upce => BarcodeFormats.Upce,
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
}
