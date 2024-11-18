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

        if (layer is not null) 
        {
            if (_shapeLayer is not null)
                _shapeLayer.Position = new CGPoint(layer.Bounds.GetMidX(), layer.Bounds.GetMidY());

            if (_previewLayer is not null)
            {
                _previewLayer.Frame = layer.Bounds;

                var connection = _previewLayer.Connection;
                if (connection is not null && connection.SupportsVideoOrientation)
                {
                    connection.VideoOrientation = this.Window?.WindowScene?.InterfaceOrientation switch
                    {
                        UIInterfaceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeLeft,
                        UIInterfaceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeRight,
                        UIInterfaceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
                        _ => AVCaptureVideoOrientation.Portrait
                    };
                }
            }
        }
    }
}