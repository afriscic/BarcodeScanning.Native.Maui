using Microsoft.Maui.Handlers;

namespace BarcodeScanning;

public partial class CameraViewHandler : ViewHandler<CameraView, BarcodeView>
{
    private CameraManager _cameraManager = null;

    public static readonly PropertyMapper<CameraView, CameraViewHandler> CameraViewMapper = new()
    {
        [nameof(CameraView.CameraFacing)] = (handler, virtualView) => handler._cameraManager?.UpdateCamera(),
        [nameof(CameraView.CaptureQuality)] = (handler, virtualView) => handler._cameraManager?.UpdateResolution(),
        [nameof(CameraView.BarcodeSymbologies)] = (handler, virtualView) => handler._cameraManager?.UpdateAnalyzer(),
        [nameof(CameraView.TorchOn)] = (handler, virtualView) => handler._cameraManager?.UpdateTorch(),
        [nameof(CameraView.CameraEnabled)] = (handler, virtualView) => handler._cameraManager?.HandleCameraEnabled(),
        [nameof(CameraView.AimMode)] = (handler, virtualView) => handler._cameraManager?.HandleAimMode(),
        [nameof(CameraView.TapToFocusEnabled)] = (handler, virtualView) => handler._cameraManager?.HandleTapToFocus(),
        [nameof(CameraView.RequestZoomFactor)] = (handler, virtualView) => handler._cameraManager?.UpdateZoomFactor()
    };

    public static readonly CommandMapper<CameraView, CameraViewHandler> CameraCommandMapper = new()
    {
    };

    public CameraViewHandler() : base(CameraViewMapper, CameraCommandMapper)
    {
    }

    protected override void DisconnectHandler(BarcodeView barcodeView)
    {
        _cameraManager.Dispose();
        base.DisconnectHandler(barcodeView);
    }
}