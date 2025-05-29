using AVFoundation;
using CoreGraphics;
using UIKit;

namespace BarcodeScanning;

public class BarcodeView : UIView
{
    private readonly CameraManager _cameraManager;

    internal BarcodeView(CameraManager cameraManager) : base()
    {
        _cameraManager = cameraManager;
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();

        if (this.Layer is not null)
        {
            if (_cameraManager?.ShapeLayer is not null)
                _cameraManager.ShapeLayer.Position = new CGPoint(this.Layer.Bounds.GetMidX(), this.Layer.Bounds.GetMidY());

            if (_cameraManager?.PreviewLayer is not null)
                _cameraManager.PreviewLayer.Frame = this.Layer.Bounds;
        }

        var connection = _cameraManager?.PreviewLayer?.Connection;
        if (connection is null)
            return;
            
        /*if (OperatingSystem.IsIOSVersionAtLeast(17))
        {
            var angle = this.Window?.WindowScene?.InterfaceOrientation switch
            {
                UIInterfaceOrientation.LandscapeLeft => 270,
                UIInterfaceOrientation.PortraitUpsideDown => 180,
                UIInterfaceOrientation.LandscapeRight => 90,
                _ => 0
            };
            if (connection.IsVideoRotationAngleSupported(angle))
                connection.VideoRotationAngle = angle;
        }
        else
        {*/
            if (connection.SupportsVideoOrientation)
            {
                connection.VideoOrientation = this.Window?.WindowScene?.InterfaceOrientation switch
                {
                    UIInterfaceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeLeft,
                    UIInterfaceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
                    UIInterfaceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeRight,
                    _ => AVCaptureVideoOrientation.Portrait
                };
            }
        //}
    }
}