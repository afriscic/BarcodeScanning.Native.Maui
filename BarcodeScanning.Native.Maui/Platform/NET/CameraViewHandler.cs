namespace BarcodeScanning;

public partial class CameraViewHandler
{
    protected override BarcodeView CreatePlatformView()
    {
        _cameraManager = null;
        throw new PlatformNotSupportedException();
    }
}