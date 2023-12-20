using Microsoft.Maui.Handlers;

namespace BarcodeScanning;

public partial class CameraViewHandler : ViewHandler<CameraView, BarcodeView>
{
    public static readonly PropertyMapper<CameraView, CameraViewHandler> CameraViewMapper = new()
    {
        [nameof(CameraView.CameraFacing)] = (handler, virtualView) => handler.UpdateCamera(),
        [nameof(CameraView.CaptureQuality)] = (handler, virtualView) => handler.UpdateResolution(),
        [nameof(CameraView.BarcodeSymbologies)] = (handler, virtualView) => handler.UpdateAnalyzer(),
        [nameof(CameraView.TorchOn)] = (handler, virtualView) => handler.UpdateTorch(),
        [nameof(CameraView.CameraEnabled)] = (handler, virtualView) => handler.HandleCameraEnabled(),
        [nameof(CameraView.AimMode)] = (handler, virtualView) => handler.HandleAimModeEnabled()
    };

    public static readonly CommandMapper<CameraView, CameraViewHandler> CameraCommandMapper = new()
    {
    };

    public CameraViewHandler() : base(CameraViewMapper, CameraCommandMapper)
    {
    }

    protected override void DisconnectHandler(BarcodeView barcodeView)
    {
        this.Stop();
        base.DisconnectHandler(barcodeView);
        this.DisposeView();
    }
}