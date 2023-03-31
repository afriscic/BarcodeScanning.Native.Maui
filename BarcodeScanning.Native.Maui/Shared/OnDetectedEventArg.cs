namespace BarcodeScanning;

public class OnDetectionFinishedEventArg : EventArgs
{
    public HashSet<BarcodeResult> BarcodeResults { get; set; }

    public OnDetectionFinishedEventArg()
    {
        BarcodeResults = new HashSet<BarcodeResult>();
    }
}