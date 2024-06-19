namespace BarcodeScanning;

public static partial class Methods
{
    public static Task<HashSet<BarcodeResult>> ScanFromImageAsync(byte[] imageArray) => throw new NotImplementedException();
    public static Task<HashSet<BarcodeResult>> ScanFromImageAsync(FileResult file) => throw new NotImplementedException();
    public static Task<HashSet<BarcodeResult>> ScanFromImageAsync(string url) => throw new NotImplementedException();
    public static Task<HashSet<BarcodeResult>> ScanFromImageAsync(Stream stream) => throw new NotImplementedException();
}