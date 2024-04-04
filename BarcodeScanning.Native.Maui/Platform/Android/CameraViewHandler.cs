namespace BarcodeScanning;

public partial class CameraViewHandler
{
    protected override BarcodeView CreatePlatformView()
    {
        _cameraManager = new CameraManager(VirtualView, Context);
        return _cameraManager.BarcodeView;
    }
}