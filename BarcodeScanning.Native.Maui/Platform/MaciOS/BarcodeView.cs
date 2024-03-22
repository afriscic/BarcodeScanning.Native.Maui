using AVFoundation;
using CoreAnimation;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using UIKit;

namespace BarcodeScanning;

public class BarcodeView : UIView
{
    internal AVCaptureVideoPreviewLayer PreviewLayer { get => _previewLayer; }

    private AVCaptureDevice _captureDevice;
    private AVCaptureInput _captureInput;
    private AVCaptureVideoDataOutput _videoDataOutput;
    private BarcodeAnalyzer _barcodeAnalyzer;

    private readonly AVCaptureVideoPreviewLayer _previewLayer;
    private readonly AVCaptureSession _captureSession;
    private readonly CameraView _cameraView;
    private readonly CAShapeLayer _shapeLayer;
    private readonly NSObject _subjectAreaChangedNotificaion;
    private readonly UITapGestureRecognizer _uITapGestureRecognizer;

    private readonly object _syncLock = new();
    private readonly object _configLock = new();

    internal BarcodeView(CameraView cameraView) : base()
    {
        _cameraView = cameraView;
        _shapeLayer = new CAShapeLayer();
        _captureSession = new AVCaptureSession();
        _uITapGestureRecognizer = new UITapGestureRecognizer(FocusOnTap);
        _previewLayer = new AVCaptureVideoPreviewLayer(_captureSession)
        {
            VideoGravity = AVLayerVideoGravity.ResizeAspectFill
        };
        _subjectAreaChangedNotificaion = NSNotificationCenter.DefaultCenter.AddObserver(AVCaptureDevice.SubjectAreaDidChangeNotification, (n) => 
        {
            if (n.Name == AVCaptureDevice.SubjectAreaDidChangeNotification)
                ResetFocus();
        });

        this.AddGestureRecognizer(_uITapGestureRecognizer);
        this.Layer.AddSublayer(_previewLayer);
    }

    internal void Start()
    {
        if (_captureSession is not null)
        {
            if (_captureSession.Running)
                _captureSession.StopRunning();
            
            if (_captureSession.Inputs.Length == 0)
                UpdateCamera();
            if (_captureSession.SessionPreset is null)
                UpdateResolution();

            UpdateAnalyzer();
            UpdateTorch();

            lock (_syncLock)
            {
                _captureSession.StartRunning();
            }

            UpdateZoomFactor();
        }
    }

    internal void Stop()
    {
        if (_captureSession is not null)
        {
            if (_captureDevice is not null && _captureDevice.TorchActive)
                CaptureDeviceLock(() => _captureDevice.TorchMode = AVCaptureTorchMode.Off);

            if (_captureSession.Running)
                _captureSession.StopRunning();
        }
    }

    internal void UpdateResolution()
    {
        if (_captureSession is not null)
        {
            var quality = _cameraView?.CaptureQuality ?? CaptureQuality.Medium;
            while (!_captureSession.CanSetSessionPreset(GetCaptureSessionResolution(quality)) && quality != CaptureQuality.Low)
            {
                quality -= 1;
            }

            lock (_syncLock)
            {
                _captureSession.BeginConfiguration();
                _captureSession.SessionPreset = GetCaptureSessionResolution(quality);
                _captureSession.CommitConfiguration();
            }
        }
    }

    internal void UpdateAnalyzer()
    {
        if (_captureSession is not null)
        {
            lock (_syncLock)
            {
                _captureSession.BeginConfiguration();

                if (_videoDataOutput is not null)
                {
                    if (_captureSession.Outputs.Contains(_videoDataOutput))
                        _captureSession.RemoveOutput(_videoDataOutput);

                    _videoDataOutput.Dispose();
                }

                _videoDataOutput = new AVCaptureVideoDataOutput()
                {
                    AlwaysDiscardsLateVideoFrames = true
                };
                
                _barcodeAnalyzer?.Dispose();
                _barcodeAnalyzer = new BarcodeAnalyzer(_cameraView, this);
                _videoDataOutput.SetSampleBufferDelegate(_barcodeAnalyzer, DispatchQueue.MainQueue);
                
                if (_captureSession.CanAddOutput(_videoDataOutput))
                    _captureSession.AddOutput(_videoDataOutput);

                _captureSession.CommitConfiguration();
            }
        }
    }

    internal void UpdateCamera()
    {
        if (_captureSession is not null)
        {
            lock (_syncLock)
            {
                _captureSession.BeginConfiguration();

                _captureSession.SessionPreset = AVCaptureSession.Preset1280x720;

                if (_captureInput is not null)
                {
                    if (_captureSession.Inputs.Contains(_captureInput))
                        _captureSession.RemoveInput(_captureInput);

                    _captureInput.Dispose();
                }

                _captureDevice?.Dispose();
                if (_cameraView?.CameraFacing == CameraFacing.Front)
                {
                    _captureDevice = AVCaptureDevice.GetDefaultDevice(AVCaptureDeviceType.BuiltInWideAngleCamera, AVMediaTypes.Video, AVCaptureDevicePosition.Front);
                }
                else
                {
                    _captureDevice = AVCaptureDevice.GetDefaultDevice(AVCaptureDeviceType.BuiltInTripleCamera, AVMediaTypes.Video, AVCaptureDevicePosition.Back);
                    _captureDevice ??= AVCaptureDevice.GetDefaultDevice(AVCaptureDeviceType.BuiltInDualWideCamera, AVMediaTypes.Video, AVCaptureDevicePosition.Back);
                    _captureDevice ??= AVCaptureDevice.GetDefaultDevice(AVCaptureDeviceType.BuiltInDualCamera, AVMediaTypes.Video, AVCaptureDevicePosition.Back);
                    _captureDevice ??= AVCaptureDevice.GetDefaultDevice(AVCaptureDeviceType.BuiltInWideAngleCamera, AVMediaTypes.Video, AVCaptureDevicePosition.Back);
                }
                _captureDevice ??= AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);

                if (_captureDevice is not null)
                {
                    _captureInput = new AVCaptureDeviceInput(_captureDevice, out _);
                    
                    if (_captureSession.CanAddInput(_captureInput))
                        _captureSession.AddInput(_captureInput);
                }

                _captureSession.CommitConfiguration();
            }

            ReportZoomFactors();
            ResetFocus();
            UpdateResolution();

            _cameraView?.ResetRequestZoomFactor();
        }
    }
    internal void UpdateTorch()
    {
        if (_captureDevice is not null && _captureDevice.HasTorch && _captureDevice.TorchAvailable)
        {
            if (_cameraView?.TorchOn ?? false)
                CaptureDeviceLock(() => 
                {
                    if(_captureDevice.IsTorchModeSupported(AVCaptureTorchMode.On))
                        _captureDevice.TorchMode = AVCaptureTorchMode.On;
                });
            else
                CaptureDeviceLock(() =>
                {
                    if(_captureDevice.IsTorchModeSupported(AVCaptureTorchMode.Off))
                        _captureDevice.TorchMode = AVCaptureTorchMode.Off;
                });
        }
    }

    internal void UpdateZoomFactor()
    {
        var factor = _cameraView?.RequestZoomFactor ?? -1;

        if (factor < 0)
            return;

        var minValue = _cameraView?.MinZoomFactor ?? -1;
        var maxValue = _cameraView?.MaxZoomFactor ?? -1;

        if (factor < minValue)
            factor = minValue;
        if (factor > maxValue)
            factor = maxValue;
        
        if (factor > 0 && _captureDevice is not null)
            CaptureDeviceLock(() => _captureDevice.VideoZoomFactor = factor);

        ReportZoomFactors();
    }

    internal void HandleCameraEnabled()
    {
        if (_cameraView?.CameraEnabled ?? false)
            Start();
        else
            Stop();
    }

    internal void HandleAimMode()
    {
        if (_cameraView?.AimMode ?? false)
        {
            var radius = 8;
            _shapeLayer.Path = UIBezierPath.FromOval(new CGRect(-radius, -radius, 2 * radius, 2 * radius)).CGPath;
            _shapeLayer.FillColor = UIColor.Red.ColorWithAlpha(0.60f).CGColor;
            _shapeLayer.StrokeColor = UIColor.Clear.CGColor;
            _shapeLayer.LineWidth = 0;

            this.Layer.AddSublayer(_shapeLayer);
        }
        else
        {
            try
            {
                _shapeLayer.RemoveFromSuperLayer();
            }
            catch (Exception)
            {
            }
        }
    }

    internal void HandleTapToFocus() {}

    private void FocusOnTap()
    {
        if (_cameraView?.TapToFocusEnabled ?? false && _captureDevice is not null  && _captureDevice.FocusPointOfInterestSupported)
        {
            CaptureDeviceLock(() =>
            {
                _captureDevice.FocusPointOfInterest = _previewLayer.CaptureDevicePointOfInterestForPoint(_uITapGestureRecognizer.LocationInView(this));
                _captureDevice.FocusMode = AVCaptureFocusMode.AutoFocus;
                _captureDevice.SubjectAreaChangeMonitoringEnabled = true;
            });
        }
    }

    private void ResetFocus()
    {
        if (_captureDevice is not null)
        {
            CaptureDeviceLock(() => 
            {
                if (_captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
                    _captureDevice.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
                else
                    _captureDevice.FocusMode = AVCaptureFocusMode.AutoFocus;
                
                _captureDevice.SubjectAreaChangeMonitoringEnabled = false;
            });
        }
    }

    private static NSString GetCaptureSessionResolution(CaptureQuality quality)
    {
        return quality switch
        {
            CaptureQuality.Low => AVCaptureSession.Preset640x480,
            CaptureQuality.Medium => AVCaptureSession.Preset1280x720,
            CaptureQuality.High => AVCaptureSession.Preset1920x1080,
            CaptureQuality.Highest => AVCaptureSession.Preset3840x2160,
            _ => AVCaptureSession.Preset1280x720
        };
    }

    private void CaptureDeviceLock(Action handler)
    {
        MainThread.BeginInvokeOnMainThread(() => 
        {
            lock (_configLock)
            {
                if (_captureDevice?.LockForConfiguration(out _) ?? false)
                {
                    try
                    {
                        handler();
                    }
                    catch (Exception)
                    {      
                    }
                    finally
                    {
                        _captureDevice.UnlockForConfiguration();
                    }
                }
            }
        });
    }

    private void ReportZoomFactors()
    {
        try
        {
            _cameraView.CurrentZoomFactor = (float)_captureDevice.VideoZoomFactor;
            _cameraView.MinZoomFactor = (float)_captureDevice.MinAvailableVideoZoomFactor;
            _cameraView.MaxZoomFactor = (float)_captureDevice.MaxAvailableVideoZoomFactor;
            _cameraView.DeviceSwitchZoomFactor = _captureDevice.VirtualDeviceSwitchOverVideoZoomFactors.Select(s => (float)s).ToArray();
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

        var interfaceOrientation = Window.WindowScene?.InterfaceOrientation;

        var videoOrientation = interfaceOrientation switch
        {
            UIInterfaceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeLeft,
            UIInterfaceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeRight,
            UIInterfaceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
            _ => AVCaptureVideoOrientation.Portrait
        };

        if (_previewLayer.Connection is not null && _previewLayer.Connection.SupportsVideoOrientation)
            _previewLayer.Connection.VideoOrientation = videoOrientation;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                Stop();

                _previewLayer?.RemoveFromSuperLayer();
                _shapeLayer?.RemoveFromSuperLayer();

                _shapeLayer?.Dispose();
                _previewLayer?.Dispose();
                _subjectAreaChangedNotificaion?.Dispose();
                _uITapGestureRecognizer?.Dispose();
                _captureSession?.Dispose();
                _videoDataOutput?.Dispose();
                _captureInput?.Dispose();
                _captureDevice?.Dispose();
                _barcodeAnalyzer?.Dispose();
            }
            catch (Exception)
            {
            }
        }

        base.Dispose(disposing);
    }
}
