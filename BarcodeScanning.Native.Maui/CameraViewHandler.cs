using Microsoft.Maui.Handlers;

#if IOS
using NativeCameraView = UIKit.UIView;
#elif ANDROID
using NativeCameraView = AndroidX.Camera.View.PreviewView;
#endif

namespace BarcodeScanning;

public partial class CameraViewHandler : ViewHandler<ICameraView, NativeCameraView>
{
    public static readonly PropertyMapper<ICameraView, CameraViewHandler> CameraViewMapper = new()
    {
        [nameof(ICameraView.CameraEnabled)] = (handler, virtualView) => handler.HandleCameraEnabled(),
        [nameof(ICameraView.CameraFacing)] = (handler, virtualView) => handler.UpdateCamera(),
        [nameof(ICameraView.CaptureQuality)] = (handler, virtualView) => handler.UpdateResolution(),
        [nameof(ICameraView.BarcodeSymbologies)] = (handler, virtualView) => handler.UpdateAnalyzer(),
        [nameof(ICameraView.TorchOn)] = (handler, virtualView) => handler.UpdateTorch()
    };

    public static readonly CommandMapper<ICameraView, CameraViewHandler> CameraCommandMapper = new()
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