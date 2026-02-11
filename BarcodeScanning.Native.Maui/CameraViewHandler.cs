using Microsoft.Maui.Handlers;

namespace BarcodeScanning;

public partial class CameraViewHandler : ViewHandler<CameraView, BarcodeView>
{
    private CameraManager? _cameraManager;

    public static readonly PropertyMapper<CameraView, CameraViewHandler> CameraViewMapper = new()
    {
        [nameof(CameraView.CameraFacing)] = (handler, virtualView) => handler?._cameraManager?.UpdateCamera(),
        [nameof(CameraView.CaptureQuality)] = (handler, virtualView) => handler?._cameraManager?.UpdateResolution(),
        [nameof(CameraView.BarcodeSymbologies)] = (handler, virtualView) => handler?._cameraManager?.UpdateSymbologies(),
        [nameof(CameraView.TorchOn)] = (handler, virtualView) => handler?._cameraManager?.UpdateTorch(),
        [nameof(CameraView.BackgroundColor)] = (handler, virtualView) => handler?._cameraManager?.UpdateBackgroundColor(),
        [nameof(CameraView.CameraEnabled)] = (handler, virtualView) => handler?._cameraManager?.UpdateCameraEnabled(),
        [nameof(CameraView.AimMode)] = (handler, virtualView) => handler._cameraManager?.UpdateAimMode(),
        [nameof(CameraView.TapToFocusEnabled)] = (handler, virtualView) => handler?._cameraManager?.UpdateTapToFocus(),
        [nameof(CameraView.RequestZoomFactor)] = (handler, virtualView) => handler?._cameraManager?.UpdateZoomFactor(),
        [nameof(CameraView.VibrationOnDetected)] = (handler, virtualView) => handler?._cameraManager?.UpdateVibration(),
        [nameof(CameraView.CameraPreviewScaleY)] = (handler, virtualView) => handler?._cameraManager?.UpdatePreviewScale(),
        [nameof(CameraView.CameraPreviewScaleX)] = (handler, virtualView) => handler?._cameraManager?.UpdatePreviewScale(),
    };

    public static readonly CommandMapper<CameraView, CameraViewHandler> CameraCommandMapper = new()
    {
    };

    public CameraViewHandler() : base(CameraViewMapper, CameraCommandMapper)
    {
    }

    protected override void DisconnectHandler(BarcodeView barcodeView)
    {
        _cameraManager?.Dispose();
        base.DisconnectHandler(barcodeView);
    }
}