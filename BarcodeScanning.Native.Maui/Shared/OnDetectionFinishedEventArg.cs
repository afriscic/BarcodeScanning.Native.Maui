namespace BarcodeScanning;

public class OnDetectionFinishedEventArg : EventArgs
{
    public BarcodeResult[] BarcodeResults { get; set; } = [];
}