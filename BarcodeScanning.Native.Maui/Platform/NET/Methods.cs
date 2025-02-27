namespace BarcodeScanning;

public static partial class Methods
{
    public static Task<IReadOnlySet<BarcodeResult>> ScanFromImageAsync(byte[] imageArray) => throw new NotImplementedException();
    public static Task<IReadOnlySet<BarcodeResult>> ScanFromImageAsync(FileResult file) => throw new NotImplementedException();
    public static Task<IReadOnlySet<BarcodeResult>> ScanFromImageAsync(string url) => throw new NotImplementedException();
    public static Task<IReadOnlySet<BarcodeResult>> ScanFromImageAsync(Stream stream) => throw new NotImplementedException();
}
