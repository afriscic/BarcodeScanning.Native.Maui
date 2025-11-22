namespace BarcodeScanning;

public partial class CameraViewHandler
{
    protected override BarcodeView CreatePlatformView()
    {
        _cameraManager = new CameraManager(VirtualView);
        ArgumentNullException.ThrowIfNull(_cameraManager.BarcodeView);
        return _cameraManager.BarcodeView;
    }
}