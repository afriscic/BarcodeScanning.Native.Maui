namespace BarcodeScanning;

public partial class CameraViewHandler
{
    protected override BarcodeView CreatePlatformView()
    {
        _barcodeView = new BarcodeView(Context, VirtualView);
        return _barcodeView;
    }
}