namespace BarcodeScanning;

public partial class CameraViewHandler
{
    protected override BarcodeView CreatePlatformView()
    {
        _cameraManager = new CameraManager(VirtualView);
        return _cameraManager.BarcodeView;
    }
}