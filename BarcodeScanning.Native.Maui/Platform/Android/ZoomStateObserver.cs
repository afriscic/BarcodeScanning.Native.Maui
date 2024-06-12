using AndroidX.Camera.Core;
using AndroidX.Lifecycle;

namespace BarcodeScanning;

internal class ZoomStateObserver : Java.Lang.Object, IObserver
{
    private readonly CameraView _cameraView;

    internal ZoomStateObserver(CameraView cameraView)
    {
        _cameraView = cameraView;
    }

    public void OnChanged(Java.Lang.Object value)
    {
        if (value is not null && _cameraView is not null && value is IZoomState state)
        {
            _cameraView.CurrentZoomFactor = state.ZoomRatio;
            _cameraView.MinZoomFactor = state.MinZoomRatio;
            _cameraView.MaxZoomFactor = state.MaxZoomRatio;
        }
    }
}