using AndroidX.Camera.Core;
using AndroidX.Lifecycle;

namespace BarcodeScanning;

internal class CameraStateObserver : Java.Lang.Object, IObserver
{
    private readonly CameraManager? _cameraManager;
    private readonly CameraView? _cameraView;

    internal CameraStateObserver(CameraManager? cameraManager, CameraView? cameraView)
    {
        _cameraManager = cameraManager;
        _cameraView = cameraView;
    }

    public void OnChanged(Java.Lang.Object? value)
    {
        if (value is IZoomState zoomState)
        {
            _cameraView?.CurrentZoomFactor = zoomState.ZoomRatio;
            _cameraView?.MinZoomFactor = zoomState.MinZoomRatio;
            _cameraView?.MaxZoomFactor = zoomState.MaxZoomRatio;
        }

        if (value is CameraState cameraState)
        {
            _cameraManager?.OpenedCameraState = cameraState;

            if (cameraState.GetType() == CameraState.Type.Open)
                _cameraManager?.UpdateZoomFactor();
        }
    }
}