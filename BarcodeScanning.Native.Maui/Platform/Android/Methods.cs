using Android.Gms.Extensions;
using Android.Graphics;
using Android.Runtime;
using Java.Net;
using Java.Util;
using System.Runtime.InteropServices;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;

using Image = Android.Media.Image;
using Paint = Android.Graphics.Paint;
using Size = Android.Util.Size;

namespace BarcodeScanning;

public static partial class Methods
{
    private static readonly bool neonSupported = IsNeonSupported();
    private static readonly ParallelOptions parallelOptions = new()
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount * 2
    };
    
    public static async Task<IReadOnlySet<BarcodeResult>> ScanFromImageAsync(byte[] imageArray)
        => await ProcessBitmapAsync(await BitmapFactory.DecodeByteArrayAsync(imageArray, 0, imageArray.Length));
    public static async Task<IReadOnlySet<BarcodeResult>> ScanFromImageAsync(FileResult file)
        => await ProcessBitmapAsync(await BitmapFactory.DecodeStreamAsync(await file.OpenReadAsync()));
    public static async Task<IReadOnlySet<BarcodeResult>> ScanFromImageAsync(string url)
        => await ProcessBitmapAsync(await BitmapFactory.DecodeStreamAsync(new URL(url).OpenStream()));
    public static async Task<IReadOnlySet<BarcodeResult>> ScanFromImageAsync(Stream stream)
        => await ProcessBitmapAsync(await BitmapFactory.DecodeStreamAsync(stream));
    private static async Task<IReadOnlySet<BarcodeResult>> ProcessBitmapAsync(Bitmap? bitmap)
    {
        var barcodeResults = new HashSet<BarcodeResult>();

        if (bitmap is null)
            return barcodeResults;
        
        using var scanner = Xamarin.Google.MLKit.Vision.BarCode.BarcodeScanning.GetClient(new BarcodeScannerOptions.Builder()
            .SetBarcodeFormats(Barcode.FormatAllFormats)
            .Build());

        using var image = InputImage.FromBitmap(bitmap, 0);
        using var results = await scanner.Process(image);
        ProcessBarcodeResult(results, barcodeResults);

        using var invertedBitmap = Bitmap.CreateBitmap(bitmap.Height, bitmap.Width, bitmap.GetConfig());
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
        using var invertedResults = await scanner.Process(invertedImage);
        ProcessBarcodeResult(invertedResults, barcodeResults);

        return barcodeResults;
    }
    
    private static void ProcessBarcodeResult(Java.Lang.Object? inputResults, HashSet<BarcodeResult> outputResults)
    {
        if (inputResults is not JavaList javaList)
            return;

        foreach (Barcode barcode in javaList)
        {
            if (barcode is null)
                continue;
            if (string.IsNullOrEmpty(barcode.DisplayValue) && string.IsNullOrEmpty(barcode.RawValue))
                continue;

            outputResults.Add(barcode.AsBarcodeResult());
        }
    }

    [LibraryImport("InvertBytes.so")]
    private static partial int InvertBytes(IntPtr data, int length);

    internal static void InvertLuminance(Image image)
    {
        var yBuffer = image.GetPlanes()?[0].Buffer;
        if (yBuffer is null)
            return;

        if (yBuffer.IsDirect)
        {
            var data = yBuffer.GetDirectBufferAddress();
            var length = yBuffer.Capacity();

            int result;
            if (neonSupported)
                result = InvertBytes(data, length);
            else
                result = -1;

            if (result != 0)
            {
                unsafe
                {
                    var dataPtr = (ulong*)data; 
                    Parallel.For(0, length >> 3, parallelOptions, (i) => dataPtr[i] = ~dataPtr[i]);
                }
            }
        }
        else
        {
            using var bits = BitSet.ValueOf(yBuffer);
            bits?.Flip(0, bits.Length());
            yBuffer.Rewind();
            yBuffer.Put(bits?.ToByteArray() ?? []);
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
        return captureQuality switch
        {
            CaptureQuality.Low => new Size(640, 480),
            CaptureQuality.Medium => new Size(1280, 720),
            CaptureQuality.High => new Size(1920, 1080),
            CaptureQuality.Highest => new Size(3840, 2160),
            _ => new Size(1280, 720)
        };
    }

    private static bool IsNeonSupported()
    {
        try
        {
            var info = File.ReadAllText("/proc/cpuinfo");
            return info.Contains("neon") || info.Contains("asimd");
        }
        catch (Exception)
        {
            return false;
        }
    }
}