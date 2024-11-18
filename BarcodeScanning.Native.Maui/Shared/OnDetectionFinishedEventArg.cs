namespace BarcodeScanning;

public class OnDetectionFinishedEventArg : EventArgs
{
    public required IReadOnlySet<BarcodeResult> BarcodeResults { get; set; }
}