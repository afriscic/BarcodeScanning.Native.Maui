using Android.Gms.Extensions;
using Android.Graphics;
using Android.Runtime;
using AndroidX.Camera.View.Transform;
using Java.Util;
using Microsoft.Maui.Graphics.Platform;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;
using Image = Android.Media.Image;
using Size = Android.Util.Size;

namespace BarcodeScanning;

public static partial class Methods
{
    public static async Task<HashSet<BarcodeResult>> ScanFromImage(byte[] imageArray)
    {
        using Bitmap bitmap = await BitmapFactory.DecodeByteArrayAsync(imageArray, 0, imageArray.Length);
        if (bitmap is null)
            return null;
        using var image = InputImage.FromBitmap(bitmap, 0);
        var scanner = Xamarin.Google.MLKit.Vision.BarCode.BarcodeScanning.GetClient(new BarcodeScannerOptions.Builder()
            .SetBarcodeFormats(Barcode.FormatAllFormats)
            .Build());
        return ProcessBarcodeResult(await scanner.Process(image));
    }

    internal static void InvertLuminance(Image image)
    {
        var yBuffer = image.GetPlanes()[0].Buffer;
        using (var bits = BitSet.ValueOf(yBuffer))
        {
            bits.Flip(0, bits.Length());
            yBuffer.Rewind();
            yBuffer.Put(bits.ToByteArray());
        }
    }

    internal static BarcodeTypes ConvertBarcodeResultTypes(int barcodeValueType)
    {
        return barcodeValueType switch
        {
            Barcode.TypeCalendarEvent => BarcodeTypes.CalendarEvent,
            Barcode.TypeContactInfo => BarcodeTypes.ContactInfo,
            Barcode.TypeDriverLicense => BarcodeTypes.DriversLicense,
            Barcode.TypeEmail => BarcodeTypes.Email,
            Barcode.TypeGeo => BarcodeTypes.GeographicCoordinates,
            Barcode.TypeIsbn => BarcodeTypes.Isbn,
            Barcode.TypePhone => BarcodeTypes.Phone,
            Barcode.TypeProduct => BarcodeTypes.Product,
            Barcode.TypeSms => BarcodeTypes.Sms,
            Barcode.TypeText => BarcodeTypes.Text,
            Barcode.TypeUrl => BarcodeTypes.Url,
            Barcode.TypeWifi => BarcodeTypes.WiFi,
            _ => BarcodeTypes.Unknown
        };
    }

    internal static int ConvertBarcodeFormats(BarcodeFormats barcodeFormats)
    {
        var formats = Barcode.FormatAllFormats;

        if (barcodeFormats.HasFlag(BarcodeFormats.CodaBar))
            formats |= Barcode.FormatCodabar;
        if (barcodeFormats.HasFlag(BarcodeFormats.Code128))
            formats |= Barcode.FormatCode128;
        if (barcodeFormats.HasFlag(BarcodeFormats.Code93))
            formats |= Barcode.FormatCode93;
        if (barcodeFormats.HasFlag(BarcodeFormats.Code39))
            formats |= Barcode.FormatCode39;
        if (barcodeFormats.HasFlag(BarcodeFormats.CodaBar))
            formats |= Barcode.FormatCodabar;
        if (barcodeFormats.HasFlag(BarcodeFormats.DataMatrix))
            formats |= Barcode.FormatDataMatrix;
        if (barcodeFormats.HasFlag(BarcodeFormats.Ean13))
            formats |= Barcode.FormatEan13;
        if (barcodeFormats.HasFlag(BarcodeFormats.Ean8))
            formats |= Barcode.FormatEan8;
        if (barcodeFormats.HasFlag(BarcodeFormats.Itf))
            formats |= Barcode.FormatItf;
        if (barcodeFormats.HasFlag(BarcodeFormats.Pdf417))
            formats |= Barcode.FormatPdf417;
        if (barcodeFormats.HasFlag(BarcodeFormats.QRCode))
            formats |= Barcode.FormatQrCode;
        if (barcodeFormats.HasFlag(BarcodeFormats.Upca))
            formats |= Barcode.FormatUpcA;
        if (barcodeFormats.HasFlag(BarcodeFormats.Upce))
            formats |= Barcode.FormatUpcE;
        if (barcodeFormats.HasFlag(BarcodeFormats.Aztec))
            formats |= Barcode.FormatAztec;
        if (barcodeFormats.HasFlag(BarcodeFormats.All))
            formats = Barcode.FormatAllFormats;
        return formats;
    }

    internal static HashSet<BarcodeResult> ProcessBarcodeResult(Java.Lang.Object result, CoordinateTransform transform = null)
    {
        var resultList = new HashSet<BarcodeResult>();

        if (result is null)
            return resultList;
        var javaList = result.JavaCast<ArrayList>();
        if (javaList.IsEmpty)
            return resultList;

        foreach (var barcode in javaList.ToArray())
        {
            var mapped = barcode.JavaCast<Barcode>();
            var rectF = mapped.BoundingBox.AsRectF();
            
            transform?.MapRect(rectF);

            resultList.Add(new BarcodeResult()
            {
                BarcodeType = ConvertBarcodeResultTypes(mapped.ValueType),
                BarcodeFormat = (BarcodeFormats)mapped.Format,
                DisplayValue = mapped.DisplayValue,
                RawValue = mapped.RawValue,
                RawBytes = mapped.GetRawBytes(),
                BoundingBox = rectF.AsRectangleF()
            });
        }
        return resultList;
    }

    internal static Size TargetResolution(CaptureQuality? captureQuality)
    {
        if (DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
        {
            return captureQuality switch
            {
                CaptureQuality.Low => new Size(480, 640),
                CaptureQuality.Medium => new Size(720, 1280),
                CaptureQuality.High => new Size(1080, 1920),
                CaptureQuality.Highest => new Size(2160, 3840),
                _ => new Size(720, 1280)
            };
        }
        else
        {
            return captureQuality switch
            {
                CaptureQuality.Low => new Size(640, 480),
                CaptureQuality.Medium => new Size(1280, 720),
                CaptureQuality.High => new Size(1920, 1080),
                CaptureQuality.Highest => new Size(3840, 2160),
                _ => new Size(1280, 720)
            };
        }
    }
}
