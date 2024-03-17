namespace BarcodeScanning;

public partial class CameraViewHandler
{
    protected override BarcodeView CreatePlatformView()
    {   
        _barcodeView = new BarcodeView(VirtualView);
        return _barcodeView;
    }
}