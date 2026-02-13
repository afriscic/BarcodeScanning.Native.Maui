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
        var formats = new List<BarcodeFormat>();

        if (barcodeFormats.HasFlag(BarcodeFormats.Code128))
            formats.Add(BarcodeFormat.Code128);
        if (barcodeFormats.HasFlag(BarcodeFormats.Code39))
            formats.Add(BarcodeFormat.Code39);
        if (barcodeFormats.HasFlag(BarcodeFormats.Code93))
            formats.Add(BarcodeFormat.Code93);
        if (barcodeFormats.HasFlag(BarcodeFormats.CodaBar))
            formats.Add(BarcodeFormat.Codabar);
        if (barcodeFormats.HasFlag(BarcodeFormats.DataMatrix))
            formats.Add(BarcodeFormat.DataMatrix);
        if (barcodeFormats.HasFlag(BarcodeFormats.Ean13))
            formats.Add(BarcodeFormat.EAN13);
        if (barcodeFormats.HasFlag(BarcodeFormats.Ean8))
            formats.Add(BarcodeFormat.EAN8);
        if (barcodeFormats.HasFlag(BarcodeFormats.Itf))
        {
            formats.Add(BarcodeFormat.ITF);
            formats.Add(BarcodeFormat.ITF14);
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.QRCode))
        {
            formats.Add(BarcodeFormat.QRCode);
            formats.Add(BarcodeFormat.QRCodeModel1);
            formats.Add(BarcodeFormat.QRCodeModel2);
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.Upca))
            formats.Add(BarcodeFormat.UPCA);
        if (barcodeFormats.HasFlag(BarcodeFormats.Upce))
            formats.Add(BarcodeFormat.UPCE);
        if (barcodeFormats.HasFlag(BarcodeFormats.Pdf417))
        {
            formats.Add(BarcodeFormat.PDF417);
            formats.Add(BarcodeFormat.CompactPDF417);
            formats.Add(BarcodeFormat.MicroPDF417);
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.Aztec))
        {
            formats.Add(BarcodeFormat.Aztec);
            formats.Add(BarcodeFormat.AztecCode);
            formats.Add(BarcodeFormat.AztecRune);
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.MicroQR))
        {
            formats.Add(BarcodeFormat.MicroQRCode);
            formats.Add(BarcodeFormat.RMQRCode);
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.GS1DataBar))
        {
            formats.Add(BarcodeFormat.DataBar);
            formats.Add(BarcodeFormat.DataBarExp);
            formats.Add(BarcodeFormat.DataBarExpStk);
            formats.Add(BarcodeFormat.DataBarLtd);
            formats.Add(BarcodeFormat.DataBarOmni);
            formats.Add(BarcodeFormat.DataBarStk);
            formats.Add(BarcodeFormat.DataBarStkOmni);
        }
        if (barcodeFormats.HasFlag(BarcodeFormats.MaxiCode))
            formats.Add(BarcodeFormat.MaxiCode);
        if (barcodeFormats.HasFlag(BarcodeFormats.DXFilmEdge))
            formats.Add(BarcodeFormat.DXFilmEdge);
        if (barcodeFormats.HasFlag(BarcodeFormats.ISBN))
            formats.Add(BarcodeFormat.ISBN);
        if (barcodeFormats.HasFlag(BarcodeFormats.All))
            formats.Add(BarcodeFormat.AllReadable);

        if (formats.Count == 0)
            formats.Add(BarcodeFormat.None);

        return new ZXingCpp.BarcodeFormats(formats);
    }

    internal static BarcodeFormats ConvertFromZxingFormats(BarcodeFormat format)
    {
        if (format == BarcodeFormat.Code128)
            return BarcodeFormats.Code128;
        if (format == BarcodeFormat.Code39)
            return BarcodeFormats.Code39;
        if (format == BarcodeFormat.Code93)
            return BarcodeFormats.Code93;
        if (format == BarcodeFormat.Codabar)
            return BarcodeFormats.CodaBar;
        if (format == BarcodeFormat.DataMatrix)
            return BarcodeFormats.DataMatrix;
        if (format == BarcodeFormat.EAN13)
            return BarcodeFormats.Ean13;
        if (format == BarcodeFormat.EAN8)
            return BarcodeFormats.Ean8;
        if (format == BarcodeFormat.ITF || 
            format == BarcodeFormat.ITF14)
            return BarcodeFormats.Itf;
        if (format == BarcodeFormat.QRCode || 
            format == BarcodeFormat.QRCodeModel1 || 
            format == BarcodeFormat.QRCodeModel2)
            return BarcodeFormats.QRCode;
        if (format == BarcodeFormat.UPCA)
            return BarcodeFormats.Upca;
        if (format == BarcodeFormat.UPCE)
            return BarcodeFormats.Upce;
        if (format == BarcodeFormat.PDF417 || 
            format == BarcodeFormat.CompactPDF417 || 
            format == BarcodeFormat.MicroPDF417)
            return BarcodeFormats.Pdf417;
        if (format == BarcodeFormat.Aztec || 
            format == BarcodeFormat.AztecCode || 
            format == BarcodeFormat.AztecRune)
            return BarcodeFormats.Aztec;
        if (format == BarcodeFormat.MicroQRCode || 
            format == BarcodeFormat.RMQRCode)
            return BarcodeFormats.MicroQR;
        if (format == BarcodeFormat.DataBar || 
            format == BarcodeFormat.DataBarExp || 
            format == BarcodeFormat.DataBarExpStk ||
            format == BarcodeFormat.DataBarLtd || 
            format == BarcodeFormat.DataBarOmni ||
            format == BarcodeFormat.DataBarStk || 
            format == BarcodeFormat.DataBarStkOmni)
            return BarcodeFormats.GS1DataBar;
        if (format == BarcodeFormat.MaxiCode)
            return BarcodeFormats.MaxiCode;
        if (format == BarcodeFormat.DXFilmEdge)
            return BarcodeFormats.DXFilmEdge;
        if (format == BarcodeFormat.ISBN)
            return BarcodeFormats.ISBN;

        return BarcodeFormats.None;
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