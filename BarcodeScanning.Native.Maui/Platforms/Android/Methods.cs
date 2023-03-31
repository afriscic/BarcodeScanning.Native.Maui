using Android.Gms.Extensions;
using Android.Graphics;
using Android.Runtime;
using AndroidX.Camera.View;
using BarcodeScanning.Native.Maui.Platforms.Android;
using Java.Util;
using Microsoft.Maui.Graphics.Platform;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;
using Image = Android.Media.Image;

namespace BarcodeScanning.Platforms.Android;

internal class Methods
{
    internal static async Task<HashSet<BarcodeResult>> ScanFromImage(byte[] imageArray)
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

    internal static HashSet<BarcodeResult> ProcessBarcodeResult(Java.Lang.Object result, CoordinateScale scale = null)
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

            if (scale is not null)
            {
                rectF.Left *= scale.Scale;
                rectF.Top *= scale.Scale;
                rectF.Right *= scale.Scale;
                rectF.Bottom *= scale.Scale;
                rectF.Offset(-scale.TranslX, -scale.TranslY);
            }

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

    /// <summary>
    /// https://github.com/giuseppesorce/cameraxscan/blob/master/app/src/main/java/com/gs/scancamerax/BarcodeOverlay.kt
    /// </summary>
    internal static CoordinateScale GetScale(Image image, PreviewView view)
    {
        float pw;
        float ph;
        float vw = view.Width;
        float vh = view.Height;

        if (DeviceDisplay.Current.MainDisplayInfo.Orientation == DisplayOrientation.Portrait)
        {
            pw = image.Height;
            ph = image.Width;
        }
        else
        {
            pw = image.Width;
            ph = image.Height;
        }

        if (pw / ph > vw / vh)
        {
            return new CoordinateScale()
            {
                Scale = vh / ph,
                TranslX = ((pw * vh / ph) - vw) / 2,
                TranslY = 0
            };
        }
        else
        {
            return new CoordinateScale()
            {
                Scale = vw / pw,
                TranslX = 0,
                TranslY = ((ph * vw / pw) - vh) / 2
            };
        }
    }
}
