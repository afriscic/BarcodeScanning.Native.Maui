namespace BarcodeScanning;

public static partial class Methods
{
    public static Task<IReadOnlySet<BarcodeResult>> ScanFromImageAsync(byte[] imageArray) => throw new PlatformNotSupportedException();
    public static Task<IReadOnlySet<BarcodeResult>> ScanFromImageAsync(FileResult file) => throw new PlatformNotSupportedException();
    public static Task<IReadOnlySet<BarcodeResult>> ScanFromImageAsync(string url) => throw new PlatformNotSupportedException();
    public static Task<IReadOnlySet<BarcodeResult>> ScanFromImageAsync(Stream stream) => throw new PlatformNotSupportedException();
}
