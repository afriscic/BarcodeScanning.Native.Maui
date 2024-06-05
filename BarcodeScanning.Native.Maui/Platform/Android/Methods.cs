using Android.Gms.Extensions;
using Android.Graphics;
using Android.Runtime;
using AndroidX.Camera.View.Transform;
using Java.Net;
using Java.Util;
using Microsoft.Maui.Graphics.Platform;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;

using Image = Android.Media.Image;
using Paint = Android.Graphics.Paint;
using RectF = Microsoft.Maui.Graphics.RectF;
using Size = Android.Util.Size;

namespace BarcodeScanning;

public static partial class Methods
{
    private static readonly ParallelOptions parallelOptions = new()
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount * 2
    };

    public static async Task<HashSet<BarcodeResult>> ScanFromImageAsync(byte[] imageArray)
        => await ProcessBitmapAsync(await BitmapFactory.DecodeByteArrayAsync(imageArray, 0, imageArray.Length));
    public static async Task<HashSet<BarcodeResult>> ScanFromImageAsync(FileResult file)
        => await ProcessBitmapAsync(await BitmapFactory.DecodeStreamAsync(await file.OpenReadAsync()));
    public static async Task<HashSet<BarcodeResult>> ScanFromImageAsync(string url)
        => await ProcessBitmapAsync(await BitmapFactory.DecodeStreamAsync(new URL(url).OpenStream()));
    public static async Task<HashSet<BarcodeResult>> ScanFromImageAsync(Stream stream)
        => await ProcessBitmapAsync(await BitmapFactory.DecodeStreamAsync(stream));
    private static async Task<HashSet<BarcodeResult>> ProcessBitmapAsync(Bitmap bitmap)
    {
        if (bitmap is null)
            return null;
        
        var barcodeResults = new HashSet<BarcodeResult>();
        using var scanner = Xamarin.Google.MLKit.Vision.BarCode.BarcodeScanning.GetClient(new BarcodeScannerOptions.Builder()
            .SetBarcodeFormats(Barcode.FormatAllFormats)
            .Build());

        using var image = InputImage.FromBitmap(bitmap, 0);
        ProcessBarcodeResult(await scanner.Process(image), barcodeResults);

        using var invertedBitmap = Bitmap.CreateBitmap(bitmap.Height, bitmap.Width, Bitmap.Config.Argb8888);
        using var canvas = new Canvas(invertedBitmap);
        using var paint = new Paint();
        using var matrixInvert = new ColorMatrix();

        matrixInvert.Set(
        [
            -1.0f,  0.0f,  0.0f, 0.0f, 255.0f,
			 0.0f, -1.0f,  0.0f, 0.0f, 255.0f,
			 0.0f,  0.0f, -1.0f, 0.0f, 255.0f,
			 0.0f,  0.0f,  0.0f, 1.0f, 0.0f
        ]);

        using var filter = new ColorMatrixColorFilter(matrixInvert);
        paint.SetColorFilter(filter);
        canvas.DrawBitmap(bitmap, 0, 0, paint);

        using var invertedImage = InputImage.FromBitmap(invertedBitmap, 0);
        ProcessBarcodeResult(await scanner.Process(invertedImage), barcodeResults);

        return barcodeResults;
    }
    
    internal static void ProcessBarcodeResult(Java.Lang.Object inputResults, HashSet<BarcodeResult> outputResults, CoordinateTransform transform = null)
    {
        if (inputResults is null)
            return;
        using var javaList = inputResults.JavaCast<ArrayList>();
        if (javaList?.IsEmpty ?? true)
            return;

        foreach (var barcode in javaList.ToArray())
        {
            using var mapped = barcode.JavaCast<Barcode>();
            if (mapped is null)
                continue;

            using var rectF = mapped.BoundingBox.AsRectF();
            var imageRect = rectF.AsRectangleF();
            
            transform?.MapRect(rectF);
            var previewRect = transform is not null ? rectF.AsRectangleF() : RectF.Zero;

            outputResults.Add(new BarcodeResult()
            {
                BarcodeType = ConvertBarcodeResultTypes(mapped.ValueType),
                BarcodeFormat = (BarcodeFormats)mapped.Format,
                DisplayValue = mapped.DisplayValue,
                RawValue = mapped.RawValue,
                RawBytes = mapped.GetRawBytes(),
                PreviewBoundingBox = previewRect,
                ImageBoundingBox = imageRect
            });
        }
    }

    internal static void InvertLuminance(Image image)
    {
        var yBuffer = image.GetPlanes()[0].Buffer;
        if (yBuffer.IsDirect)
        {
            unsafe
            {
                ulong* data = (ulong*)yBuffer.GetDirectBufferAddress();
                Parallel.For(0, yBuffer.Capacity() / sizeof(ulong), parallelOptions, (i) => data[i] = ~data[i]);
            }
        }
        else
        {
            using var bits = BitSet.ValueOf(yBuffer);
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

        if (barcodeFormats.HasFlag(BarcodeFormats.Code128))
            formats |= Barcode.FormatCode128;
        if (barcodeFormats.HasFlag(BarcodeFormats.Code39))
            formats |= Barcode.FormatCode39;
        if (barcodeFormats.HasFlag(BarcodeFormats.Code93))
            formats |= Barcode.FormatCode93;
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
        if (barcodeFormats.HasFlag(BarcodeFormats.QRCode))
            formats |= Barcode.FormatQrCode;
        if (barcodeFormats.HasFlag(BarcodeFormats.Upca))
            formats |= Barcode.FormatUpcA;
        if (barcodeFormats.HasFlag(BarcodeFormats.Upce))
            formats |= Barcode.FormatUpcE;
        if (barcodeFormats.HasFlag(BarcodeFormats.Pdf417))
            formats |= Barcode.FormatPdf417;
        if (barcodeFormats.HasFlag(BarcodeFormats.Aztec))
            formats |= Barcode.FormatAztec;
        if (barcodeFormats.HasFlag(BarcodeFormats.All))
            formats = Barcode.FormatAllFormats;
        return formats;
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
