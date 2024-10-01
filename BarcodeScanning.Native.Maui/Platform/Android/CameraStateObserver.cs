using AndroidX.Camera.Core;
using AndroidX.Lifecycle;

namespace BarcodeScanning;

internal class CameraStateObserver : Java.Lang.Object, IObserver
{
    private readonly CameraManager _cameraManager;
    private readonly CameraView _cameraView;

    internal CameraStateObserver(CameraManager cameraManager, CameraView cameraView)
    {
        _cameraManager = cameraManager;
        _cameraView = cameraView;
    }

    public void OnChanged(Java.Lang.Object value)
    {
        if (value is not null && _cameraView is not null && _cameraManager is not null)
        {
            if (value is IZoomState zoomState)
            {
                _cameraView.CurrentZoomFactor = zoomState.ZoomRatio;
                _cameraView.MinZoomFactor = zoomState.MinZoomRatio;
                _cameraView.MaxZoomFactor = zoomState.MaxZoomRatio;

                _cameraManager.UpdateZoomFactor();
            }

            if (value is CameraState cameraState)
            {
                _cameraManager.OpenedCameraState = cameraState;
            }

            _cameraManager.RecalculateCoordinateTransform = true;
        }
    }
}