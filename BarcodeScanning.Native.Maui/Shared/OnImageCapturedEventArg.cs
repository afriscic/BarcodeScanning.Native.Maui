using Microsoft.Maui.Graphics.Platform;

namespace BarcodeScanning;

public class OnImageCapturedEventArg : EventArgs
{
    public PlatformImage Image { get; set; } = null;
}