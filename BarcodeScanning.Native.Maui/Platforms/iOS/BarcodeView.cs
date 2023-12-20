using AVFoundation;
using CoreAnimation;
using CoreGraphics;
using UIKit;

namespace BarcodeScanning;

public class BarcodeView : UIView
{
    private readonly AVCaptureVideoPreviewLayer _previewLayer;
    private readonly CAShapeLayer _shapeLayer;

    internal BarcodeView(AVCaptureVideoPreviewLayer previewLayer) : base()
    {
        _previewLayer = previewLayer;
        _shapeLayer = new CAShapeLayer();

        this.Layer.AddSublayer(_previewLayer);
    }

    internal void AddAimingDot()
    {
        var radius = 8;
        _shapeLayer.Path = UIBezierPath.FromOval(new CGRect(-radius, -radius, 2 * radius, 2 * radius)).CGPath;
        _shapeLayer.FillColor = UIColor.Red.ColorWithAlpha(0.60f).CGColor;
        _shapeLayer.StrokeColor = UIColor.Clear.CGColor;
        _shapeLayer.LineWidth = 0;

        this.Layer.AddSublayer(_shapeLayer);
    }

    internal void RemoveAimingDot()
    {
        try
        {
            _shapeLayer.RemoveFromSuperLayer();
        }
        catch (Exception)
        {
            
        }
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();

        _previewLayer.Frame = this.Layer.Bounds;
        _shapeLayer.Position = new CGPoint(this.Layer.Bounds.Width / 2, this.Layer.Bounds.Height / 2);

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
