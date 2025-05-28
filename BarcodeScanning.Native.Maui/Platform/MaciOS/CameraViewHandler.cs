using UIKit;

namespace BarcodeScanning;

public partial class CameraViewHandler
{
    protected override BarcodeView CreatePlatformView()
    {
        _cameraManager = new CameraManager(VirtualView);
        var uiView = _cameraManager.BarcodeView;

        // Re-layout UIView also when device is rotated 180° at once
        // -> https://github.com/afriscic/BarcodeScanning.Native.Maui/issues/146
        UIDevice.Notifications.ObserveOrientationDidChange((_, __) =>
        {
            uiView.SetNeedsLayout();
        });

        return uiView;
    }
}