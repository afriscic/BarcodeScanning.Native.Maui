namespace BarcodeScanning;

public partial class CameraViewHandler
{
    protected override BarcodeView CreatePlatformView()
    {   
        return new BarcodeView(VirtualView);
    }
}