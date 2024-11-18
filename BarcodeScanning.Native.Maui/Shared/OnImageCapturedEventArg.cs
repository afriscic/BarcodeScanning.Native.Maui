using Microsoft.Maui.Graphics.Platform;

namespace BarcodeScanning;

public class OnImageCapturedEventArg : EventArgs
{
    public required PlatformImage Image { get; set; }
}