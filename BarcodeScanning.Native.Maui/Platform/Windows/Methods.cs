using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Graphics.Imaging;
using ZXingCpp;

namespace BarcodeScanning;

public static partial class Methods
{
    public static async Task<IReadOnlySet<BarcodeResult>> ScanFromImageAsync(byte[] imageArray)
        => await ScanFromStreamAsync(new MemoryStream(imageArray));
    public static async Task<IReadOnlySet<BarcodeResult>> ScanFromImageAsync(FileResult file)
        => await ScanFromStreamAsync(await file.OpenReadAsync());
    public static async Task<IReadOnlySet<BarcodeResult>> ScanFromStreamAsync(Stream stream)
    {
        var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
        using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

        var bitmap = softwareBitmap.BitmapPixelFormat switch
        {
            BitmapPixelFormat.Bgra8 => softwareBitmap,
            BitmapPixelFormat.Rgba8 => softwareBitmap,
            BitmapPixelFormat.Gray8 => softwareBitmap,
            _ => SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Gray8)
        };

        using var buffer = bitmap.LockBuffer(BitmapBufferAccessMode.Read);
        using var reference = buffer.CreateReference();

        var barcodeReader = new BarcodeReader
        {
            TryHarder = true,
            TryRotate = true,
            TryDownscale = true,
            TryInvert = true,
            IsPure = false,
            TextMode = TextMode.Plain
        };

        Barcode[] barcodes = [];
        unsafe
        {
            ((IMemoryBufferByteAccess)reference).GetBuffer(out byte* dataInBytes, out _);
            var iv = new ImageView(new IntPtr(dataInBytes), bitmap.PixelWidth, bitmap.PixelHeight, ConvertImageFormats(bitmap.BitmapPixelFormat));
            barcodes = barcodeReader.From(iv);
        }

        return barcodes.Select(s => s.AsBarcodeResult()).ToHashSet();
    }

    internal static ZXingCpp.BarcodeFormats ConvertBarcodeFormats(BarcodeFormats barcodeFormats)
    {
        var formats = ZXingCpp.BarcodeFormats.None;

        if (barcodeFormats.HasFlag(BarcodeFormats.Code128))
            formats |= ZXingCpp.BarcodeFormats.Code128;
        if (barcodeFormats.HasFlag(BarcodeFormats.Code39))
            formats |= ZXingCpp.BarcodeFormats.Code39;
        if (barcodeFormats.HasFlag(BarcodeFormats.Code93))
            formats |= ZXingCpp.BarcodeFormats.Code93;
        if (barcodeFormats.HasFlag(BarcodeFormats.CodaBar))
            formats |= ZXingCpp.BarcodeFormats.Codabar;
        if (barcodeFormats.HasFlag(BarcodeFormats.DataMatrix))
            formats |= ZXingCpp.BarcodeFormats.DataMatrix;
        if (barcodeFormats.HasFlag(BarcodeFormats.Ean13))
            formats |= ZXingCpp.BarcodeFormats.EAN13;
        if (barcodeFormats.HasFlag(BarcodeFormats.Ean8))
            formats |= ZXingCpp.BarcodeFormats.EAN8;
        if (barcodeFormats.HasFlag(BarcodeFormats.Itf))
            formats |= ZXingCpp.BarcodeFormats.ITF;
        if (barcodeFormats.HasFlag(BarcodeFormats.QRCode))
            formats |= ZXingCpp.BarcodeFormats.QRCode;
        if (barcodeFormats.HasFlag(BarcodeFormats.Upca))
            formats |= ZXingCpp.BarcodeFormats.UPCA;
        if (barcodeFormats.HasFlag(BarcodeFormats.Upce))
            formats |= ZXingCpp.BarcodeFormats.UPCE;
        if (barcodeFormats.HasFlag(BarcodeFormats.Pdf417))
            formats |= ZXingCpp.BarcodeFormats.PDF417;
        if (barcodeFormats.HasFlag(BarcodeFormats.Aztec))
            formats |= ZXingCpp.BarcodeFormats.Aztec;
        if (barcodeFormats.HasFlag(BarcodeFormats.MicroQR))
        {
            formats |= ZXingCpp.BarcodeFormats.MicroQRCode;
            formats |= ZXingCpp.BarcodeFormats.RMQRCode;
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.GS1DataBar))
        {
            formats |= ZXingCpp.BarcodeFormats.DataBar;
            formats |= ZXingCpp.BarcodeFormats.DataBarExpanded;
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.MaxiCode))
            formats |= ZXingCpp.BarcodeFormats.MaxiCode;
        if (barcodeFormats.HasFlag(BarcodeFormats.DXFilmEdge))
            formats |= ZXingCpp.BarcodeFormats.DXFilmEdge;
        if (barcodeFormats.HasFlag(BarcodeFormats.All))
            formats = ZXingCpp.BarcodeFormats.Any;
        return formats;
    }

    internal static BarcodeFormats ConvertFromZxingFormats(ZXingCpp.BarcodeFormats format)
    {
        return format switch
        {
            ZXingCpp.BarcodeFormats.Aztec => BarcodeFormats.Aztec,
            ZXingCpp.BarcodeFormats.Codabar => BarcodeFormats.CodaBar,
            ZXingCpp.BarcodeFormats.Code39 => BarcodeFormats.Code39,
            ZXingCpp.BarcodeFormats.Code93 => BarcodeFormats.Code93,
            ZXingCpp.BarcodeFormats.Code128 => BarcodeFormats.Code128,
            ZXingCpp.BarcodeFormats.DataBar => BarcodeFormats.GS1DataBar,
            ZXingCpp.BarcodeFormats.DataBarExpanded => BarcodeFormats.GS1DataBar,
            ZXingCpp.BarcodeFormats.DataMatrix => BarcodeFormats.DataMatrix,
            ZXingCpp.BarcodeFormats.EAN8 => BarcodeFormats.Ean8,
            ZXingCpp.BarcodeFormats.EAN13 => BarcodeFormats.Ean13,
            ZXingCpp.BarcodeFormats.ITF => BarcodeFormats.Itf,
            ZXingCpp.BarcodeFormats.MaxiCode => BarcodeFormats.MaxiCode,
            ZXingCpp.BarcodeFormats.PDF417 => BarcodeFormats.Pdf417,
            ZXingCpp.BarcodeFormats.QRCode => BarcodeFormats.QRCode,
            ZXingCpp.BarcodeFormats.UPCA => BarcodeFormats.Upca,
            ZXingCpp.BarcodeFormats.UPCE => BarcodeFormats.Upce,
            ZXingCpp.BarcodeFormats.MicroQRCode => BarcodeFormats.MicroQR,
            ZXingCpp.BarcodeFormats.RMQRCode => BarcodeFormats.MicroQR,
            ZXingCpp.BarcodeFormats.DXFilmEdge => BarcodeFormats.DXFilmEdge,
            _ => BarcodeFormats.None
        };
    }

    internal static ZXingCpp.ImageFormat ConvertImageFormats(BitmapPixelFormat pixelFormat)
    {
        return pixelFormat switch
        {
            BitmapPixelFormat.Bgra8 => ZXingCpp.ImageFormat.BGRA,
            BitmapPixelFormat.Rgba8 => ZXingCpp.ImageFormat.RGBA,
            BitmapPixelFormat.Gray8 => ZXingCpp.ImageFormat.Lum,
            _ => ZXingCpp.ImageFormat.None
        };
    }

    internal static int TargetHeight(CaptureQuality? captureQuality)
    {
        return captureQuality switch
        {
            CaptureQuality.Low => 480,
            CaptureQuality.Medium => 720,
            CaptureQuality.High => 1080,
            CaptureQuality.Highest =>  2160,
            _ => 720
        };
    }

    [GeneratedComInterface]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe internal partial interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}