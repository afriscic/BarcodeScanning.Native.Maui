using AVFoundation;
using CoreAnimation;
using CoreGraphics;
using UIKit;

namespace BarcodeScanning;

public class BarcodeView : UIView
{
    private readonly AVCaptureVideoPreviewLayer _previewLayer;
    private readonly CAShapeLayer _shapeLayer;

    internal BarcodeView(AVCaptureVideoPreviewLayer previewLayer, CAShapeLayer shapeLayer) : base()
    {
        _previewLayer = previewLayer;
        _shapeLayer = shapeLayer;
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();

        if (_previewLayer is not null)
            _previewLayer.Frame =  this.Layer.Bounds;
        if (_shapeLayer is not null)
            _shapeLayer.Position = new CGPoint(this.Layer.Bounds.Width / 2, this.Layer.Bounds.Height / 2);

        if (_previewLayer?.Connection is not null && _previewLayer.Connection.SupportsVideoOrientation)
            _previewLayer.Connection.VideoOrientation = this.Window.WindowScene?.InterfaceOrientation switch
            {
                UIInterfaceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeLeft,
                UIInterfaceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeRight,
                UIInterfaceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
                _ => AVCaptureVideoOrientation.Portrait
            };
    }
}