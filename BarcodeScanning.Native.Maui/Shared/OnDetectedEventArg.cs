namespace BarcodeScanning;

public class OnDetectionFinishedEventArg : EventArgs
{
    public HashSet<BarcodeResult> BarcodeResults { get; set; } = new();
}