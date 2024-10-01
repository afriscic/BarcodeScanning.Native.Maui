using static Android.Views.View;

namespace BarcodeScanning;

internal class PreviewViewOnLayoutChangeListener : Java.Lang.Object, IOnLayoutChangeListener
{
    private readonly CameraManager _cameraManager;

    internal PreviewViewOnLayoutChangeListener(CameraManager cameraManager)
    {
        _cameraManager = cameraManager;
    }

    public void OnLayoutChange(Android.Views.View v, int left, int top, int right, int bottom, int oldLeft, int oldTop, int oldRight, int oldBottom)
    {
        if (_cameraManager is not null)
            _cameraManager.RecalculateCoordinateTransform = true;
    }
}