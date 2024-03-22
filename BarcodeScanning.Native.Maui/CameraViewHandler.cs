using Microsoft.Maui.Handlers;

namespace BarcodeScanning;

public partial class CameraViewHandler : ViewHandler<CameraView, BarcodeView>
{
    public static readonly PropertyMapper<CameraView, CameraViewHandler> CameraViewMapper = new()
    {
        [nameof(CameraView.CameraFacing)] = (handler, virtualView) => handler.PlatformView?.UpdateCamera(),
        [nameof(CameraView.CaptureQuality)] = (handler, virtualView) => handler.PlatformView?.UpdateResolution(),
        [nameof(CameraView.BarcodeSymbologies)] = (handler, virtualView) => handler.PlatformView?.UpdateAnalyzer(),
        [nameof(CameraView.TorchOn)] = (handler, virtualView) => handler.PlatformView?.UpdateTorch(),
        [nameof(CameraView.CameraEnabled)] = (handler, virtualView) => handler.PlatformView?.HandleCameraEnabled(),
        [nameof(CameraView.AimMode)] = (handler, virtualView) => handler.PlatformView?.HandleAimMode(),
        [nameof(CameraView.TapToFocusEnabled)] = (handler, virtualView) => handler.PlatformView?.HandleTapToFocus(),
        [nameof(CameraView.RequestZoomFactor)] = (handler, virtualView) => handler.PlatformView?.UpdateZoomFactor()
    };

    public static readonly CommandMapper<CameraView, CameraViewHandler> CameraCommandMapper = new()
    {
    };

    public CameraViewHandler() : base(CameraViewMapper, CameraCommandMapper)
    {
    }

    protected override void DisconnectHandler(BarcodeView barcodeView)
    {
        barcodeView.Dispose();
        base.DisconnectHandler(barcodeView);
    }
}