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

        var layer = this.Layer;
        var window = this.Window;
        if (window is null || layer is null) {
            return;
        }

        if (_previewLayer is not null)
            _previewLayer.Frame =  layer.Bounds;
        if (_shapeLayer is not null)
            _shapeLayer.Position = new CGPoint(layer.Bounds.Width / 2, layer.Bounds.Height / 2);

        var previewLayerConnection = _previewLayer?.Connection;
        if (previewLayerConnection is not null && previewLayerConnection.SupportsVideoOrientation)
            previewLayerConnection.VideoOrientation = window.WindowScene?.InterfaceOrientation switch
            {
                UIInterfaceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeLeft,
                UIInterfaceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeRight,
                UIInterfaceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
                _ => AVCaptureVideoOrientation.Portrait
            };
    }
}