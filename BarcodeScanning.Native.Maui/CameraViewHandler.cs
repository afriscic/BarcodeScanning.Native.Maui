using Microsoft.Maui.Handlers;

namespace BarcodeScanning;

public partial class CameraViewHandler : ViewHandler<CameraView, BarcodeView>
{
    private BarcodeView _barcodeView;

    public static readonly PropertyMapper<CameraView, CameraViewHandler> CameraViewMapper = new()
    {
        [nameof(CameraView.CameraFacing)] = (handler, virtualView) => handler._barcodeView?.UpdateCamera(),
        [nameof(CameraView.CaptureQuality)] = (handler, virtualView) => handler._barcodeView?.UpdateResolution(),
        [nameof(CameraView.BarcodeSymbologies)] = (handler, virtualView) => handler._barcodeView?.UpdateAnalyzer(),
        [nameof(CameraView.TorchOn)] = (handler, virtualView) => handler._barcodeView?.UpdateTorch(),
        [nameof(CameraView.CameraEnabled)] = (handler, virtualView) => handler._barcodeView?.HandleCameraEnabled(),
        [nameof(CameraView.AimMode)] = (handler, virtualView) => handler._barcodeView?.HandleAimMode(),
        [nameof(CameraView.TapToFocusEnabled)] = (handler, virtualView) => handler._barcodeView?.HandleTapToFocus()
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