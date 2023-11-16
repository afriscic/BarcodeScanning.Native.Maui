using Microsoft.Maui.Handlers;

#if IOS
using NativeCameraView = UIKit.UIView;
#elif ANDROID
using NativeCameraView = AndroidX.Camera.View.PreviewView;
#endif

namespace BarcodeScanning;

public partial class CameraViewHandler : ViewHandler<CameraView, NativeCameraView>
{
    public static readonly PropertyMapper<CameraView, CameraViewHandler> CameraViewMapper = new()
    {
        [nameof(CameraView.CameraEnabled)] = (handler, virtualView) => handler.HandleCameraEnabled(),
        [nameof(CameraView.CameraFacing)] = (handler, virtualView) => handler.UpdateCamera(),
        [nameof(CameraView.CaptureQuality)] = (handler, virtualView) => handler.UpdateResolution(),
        [nameof(CameraView.BarcodeSymbologies)] = (handler, virtualView) => handler.UpdateAnalyzer(),
        [nameof(CameraView.TorchOn)] = (handler, virtualView) => handler.UpdateTorch()
    };

    public static readonly CommandMapper<CameraView, CameraViewHandler> CameraCommandMapper = new()
    {
    };

    public CameraViewHandler() : base(CameraViewMapper, CameraCommandMapper)
    {
    }

    protected override void ConnectHandler(NativeCameraView nativeView)
    {
        base.ConnectHandler(nativeView);

        this.HandleCameraEnabled();
    }

    protected override void DisconnectHandler(NativeCameraView nativeView)
    {
        base.DisconnectHandler(nativeView);

        this.Stop();
        nativeView.Dispose();
    }
}