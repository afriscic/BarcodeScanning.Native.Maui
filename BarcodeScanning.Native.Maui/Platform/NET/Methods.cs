namespace BarcodeScanning;

public static partial class Methods
{
    public static Task<HashSet<BarcodeResult>> ScanFromImage(byte[] imageArray) => throw new NotImplementedException();
    public static Task<HashSet<BarcodeResult>> ScanFromImage(FileResult file) => throw new NotImplementedException();
    public static Task<HashSet<BarcodeResult>> ScanFromImage(string url) => throw new NotImplementedException();
    public static Task<HashSet<BarcodeResult>> ScanFromImage(Stream stream) => throw new NotImplementedException();
}
