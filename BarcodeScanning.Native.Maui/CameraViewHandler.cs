using Microsoft.Maui.Handlers;

namespace BarcodeScanning;

public partial class CameraViewHandler()
    : ViewHandler<CameraView, BarcodeView>(CameraViewMapper, CameraCommandMapper)
{
    public static readonly PropertyMapper<CameraView, CameraViewHandler> CameraViewMapper = new()
    {
        [nameof(CameraView.CameraFacing)] = (handler, _) => handler.UpdateCamera(),
        [nameof(CameraView.CaptureQuality)] = (handler, _) => handler.UpdateResolution(),
        [nameof(CameraView.BarcodeSymbologies)] = (handler, _) => handler.UpdateAnalyzer(),
        [nameof(CameraView.TorchOn)] = (handler, _) => handler.UpdateTorch(),
        [nameof(CameraView.CameraEnabled)] = (handler, _) => handler.HandleCameraEnabled(),
        [nameof(CameraView.AimMode)] = (handler, _) => handler.HandleAimModeEnabled()
    };

    public static readonly CommandMapper<CameraView, CameraViewHandler> CameraCommandMapper = new();

    protected override void DisconnectHandler(BarcodeView barcodeView)
    {
        base.DisconnectHandler(barcodeView);
        this.DisposeView();
    }
}