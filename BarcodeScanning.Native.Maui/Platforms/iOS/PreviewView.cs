using AVFoundation;
using UIKit;

namespace BarcodeScanning.Platforms.iOS;

internal class PreviewView : UIView
{
    private readonly AVCaptureVideoPreviewLayer _previewLayer;

    internal PreviewView(AVCaptureVideoPreviewLayer previewLayer) : base()
    {
        _previewLayer = previewLayer;
        this.Layer.AddSublayer(_previewLayer);
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();

        _previewLayer.Frame = this.Layer.Bounds;

        var videoOrientation = UIDevice.CurrentDevice.Orientation switch
        {
            UIDeviceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeRight,
            UIDeviceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeLeft,
            UIDeviceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
            _ => AVCaptureVideoOrientation.Portrait
        };

        if (_previewLayer.Connection is not null && _previewLayer.Connection.SupportsVideoOrientation)
            _previewLayer.Connection.VideoOrientation = videoOrientation;
    }
}
