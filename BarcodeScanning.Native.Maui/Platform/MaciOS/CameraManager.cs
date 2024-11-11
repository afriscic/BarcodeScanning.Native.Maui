using AVFoundation;
using CoreAnimation;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platform;
using System.Diagnostics;
using UIKit;

namespace BarcodeScanning;

internal class CameraManager : IDisposable
{
    internal AVCaptureVideoPreviewLayer PreviewLayer { get => _previewLayer; }
    internal BarcodeView BarcodeView { get => _barcodeView; }
    internal CameraView? CameraView { get => _cameraView; }

    private AVCaptureDevice? _captureDevice;
    private AVCaptureInput? _captureInput;

    private readonly AVCaptureVideoDataOutput _videoDataOutput;
    private readonly AVCaptureVideoPreviewLayer _previewLayer;
    private readonly AVCaptureSession _captureSession;
    private readonly BarcodeAnalyzer _barcodeAnalyzer;
    private readonly BarcodeView _barcodeView;
    private readonly CAShapeLayer _shapeLayer;
    private readonly DispatchQueue _dispatchQueue;
    private readonly NSObject _subjectAreaChangedNotificaion;
    private readonly UITapGestureRecognizer _uITapGestureRecognizer;

    private readonly CameraView? _cameraView;

    private const int aimRadius = 8;

    internal CameraManager(CameraView cameraView)
    {
        _cameraView = cameraView;
        
        _captureSession = new AVCaptureSession();
        _barcodeAnalyzer = new BarcodeAnalyzer(this);
        _dispatchQueue = new DispatchQueue("com.barcodescanning.maui.sessionQueue", new DispatchQueue.Attributes()
        {
            QualityOfService = DispatchQualityOfService.UserInitiated
        });
        _videoDataOutput = new AVCaptureVideoDataOutput()
        {
            AlwaysDiscardsLateVideoFrames = true
        };

        _uITapGestureRecognizer = new UITapGestureRecognizer(FocusOnTap);
        _subjectAreaChangedNotificaion = NSNotificationCenter.DefaultCenter.AddObserver(AVCaptureDevice.SubjectAreaDidChangeNotification, (n) => 
        {
            if (n.Name == AVCaptureDevice.SubjectAreaDidChangeNotification)
                ResetFocus();
        });

        _previewLayer = new AVCaptureVideoPreviewLayer(_captureSession)
        {
            BackgroundColor = _cameraView?.BackgroundColor?.ToPlatform().CGColor,
            VideoGravity = AVLayerVideoGravity.ResizeAspectFill
        };
        _shapeLayer = new CAShapeLayer()
        {
            Path = UIBezierPath.FromOval(new CGRect(-aimRadius, -aimRadius, 2 * aimRadius, 2 * aimRadius)).CGPath,
            FillColor = UIColor.Red.ColorWithAlpha(0.60f).CGColor,
            StrokeColor = UIColor.Clear.CGColor,
            LineWidth = 0
        };
        
        _barcodeView = new BarcodeView(_previewLayer, _shapeLayer);
        _barcodeView.Layer.AddSublayer(_previewLayer);
        _barcodeView.AddGestureRecognizer(_uITapGestureRecognizer);
    }

    internal void Start()
    {
        if (_captureSession is not null)
        {
            if (_captureSession.Running)
                _dispatchQueue.DispatchAsync(_captureSession.StopRunning);
            
            UpdateCamera();
            UpdateResolution();

            if (!_captureSession.Outputs.Contains(_videoDataOutput) && _captureSession.CanAddOutput(_videoDataOutput))
            {
                _dispatchQueue.DispatchAsync(() =>
                {
                    _captureSession.BeginConfiguration();
                    _captureSession.AddOutput(_videoDataOutput);
                    _captureSession.CommitConfiguration();
                });
            }

            _dispatchQueue.DispatchAsync(() =>
            {
                _captureSession.StartRunning();

                if (_videoDataOutput is not null)
                {
                    _videoDataOutput.SetSampleBufferDelegate(null, null);
                    _videoDataOutput.SetSampleBufferDelegate(_barcodeAnalyzer, DispatchQueue.DefaultGlobalQueue);
                }

                UpdateSymbologies();
                UpdateTorch();
                UpdateZoomFactor();
            });
        }
    }

    internal void Stop()
    {
        if (_captureSession is not null)
        {
            if (_captureDevice is not null && _captureDevice.TorchActive)
                DeviceLock(() => _captureDevice.TorchMode = AVCaptureTorchMode.Off);

            if (_captureSession.Running)
                _dispatchQueue.DispatchAsync(_captureSession.StopRunning);
        }
    }

    internal void UpdateAimMode()
    {
        if (_cameraView?.AimMode ?? false)
            _barcodeView?.Layer?.AddSublayer(_shapeLayer);
        else
            _shapeLayer?.RemoveFromSuperLayer();
    }

    internal void UpdateBackgroundColor()
    {
        if (_previewLayer is not null)
            _previewLayer.BackgroundColor = _cameraView?.BackgroundColor?.ToPlatform().CGColor;
    }

    internal void UpdateCamera()
    {
        if (_captureSession is not null)
        {
            _dispatchQueue.DispatchAsync(() =>
            {
                _captureSession.BeginConfiguration();

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

                _captureSession.SessionPreset = Methods.GetBestSupportedPreset(_captureSession, _cameraView?.CaptureQuality ?? CaptureQuality.Medium);
                _captureSession.CommitConfiguration();

                UpdateZoomFactor();
                ResetFocus();
            });
        }
    }
    
    internal void UpdateCameraEnabled()
    {
        if (_cameraView?.CameraEnabled ?? false)
            Start();
        else
            Stop();
    }

    internal void UpdateResolution()
    {
        if (_captureSession is not null)
        {
            _dispatchQueue.DispatchAsync(() => 
            {
                _captureSession.BeginConfiguration();
                _captureSession.SessionPreset = Methods.GetBestSupportedPreset(_captureSession, _cameraView?.CaptureQuality ?? CaptureQuality.Medium);
                _captureSession.CommitConfiguration();
            });
        }
    }

    internal void UpdateSymbologies()
    {
        _barcodeAnalyzer?.UpdateSymbologies();
    }
    
    internal void UpdateTapToFocus() {}

    internal void UpdateTorch()
    {
        if (_captureDevice is not null && _captureDevice.HasTorch && _captureDevice.TorchAvailable)
        {
            if (_cameraView?.TorchOn ?? false)
                DeviceLock(() => 
                {
                    if(_captureDevice.IsTorchModeSupported(AVCaptureTorchMode.On))
                        _captureDevice.TorchMode = AVCaptureTorchMode.On;
                });
            else
                DeviceLock(() =>
                {
                    if(_captureDevice.IsTorchModeSupported(AVCaptureTorchMode.Off))
                        _captureDevice.TorchMode = AVCaptureTorchMode.Off;
                });
        }
    }

    internal void UpdateZoomFactor()
    {
        if (_cameraView is not null && _captureDevice is not null)
        {
            _cameraView.MinZoomFactor = (float)_captureDevice.MinAvailableVideoZoomFactor;
            _cameraView.MaxZoomFactor = (float)_captureDevice.MaxAvailableVideoZoomFactor;
            _cameraView.DeviceSwitchZoomFactor = _captureDevice.VirtualDeviceSwitchOverVideoZoomFactors?.Select(s => (float)s).ToArray() ?? [];

            var factor = _cameraView.RequestZoomFactor;

            if (factor > 0)
            {
                factor = Math.Max(factor, _cameraView.MinZoomFactor);
                factor = Math.Min(factor, _cameraView.MaxZoomFactor);
                
                DeviceLock(() => 
                {
                    _captureDevice.VideoZoomFactor = factor;
                    _cameraView.CurrentZoomFactor = factor;
                });
            }
        }
    }

    private void DeviceLock(Action action)
    {
        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            if (_captureDevice?.LockForConfiguration(out _) ?? false)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);      
                }
                finally
                {
                    _captureDevice?.UnlockForConfiguration();
                }
            }
        });
    }

    private void FocusOnTap()
    {
        if ((_cameraView?.TapToFocusEnabled ?? false) && _captureDevice is not null && _captureDevice.FocusPointOfInterestSupported)
        {
            DeviceLock(() =>
            {
                _captureDevice.FocusPointOfInterest = _previewLayer.CaptureDevicePointOfInterestForPoint(_uITapGestureRecognizer.LocationInView(_barcodeView));
                _captureDevice.FocusMode = AVCaptureFocusMode.AutoFocus;
                _captureDevice.SubjectAreaChangeMonitoringEnabled = true;
            });
        }
    }

    private void ResetFocus()
    {
        if (_captureDevice is not null)
        {
            DeviceLock(() => 
            {
                if (_captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
                    _captureDevice.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
                else if (_captureDevice.IsFocusModeSupported(AVCaptureFocusMode.AutoFocus))
                    _captureDevice.FocusMode = AVCaptureFocusMode.AutoFocus;
                
                _captureDevice.SubjectAreaChangeMonitoringEnabled = false;
            });
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Stop();

            if (_subjectAreaChangedNotificaion is not null)
                NSNotificationCenter.DefaultCenter.RemoveObserver(_subjectAreaChangedNotificaion);
            if (_uITapGestureRecognizer is not null)
                _barcodeView?.RemoveGestureRecognizer(_uITapGestureRecognizer);

            _videoDataOutput?.SetSampleBufferDelegate(null, null);
            
            _previewLayer?.RemoveFromSuperLayer();
            _shapeLayer?.RemoveFromSuperLayer();

            _barcodeView?.Dispose();
            _previewLayer?.Dispose();
            _shapeLayer?.Dispose();

            _captureSession?.Dispose();
            _videoDataOutput?.Dispose();
            _captureInput?.Dispose();

            _barcodeAnalyzer?.Dispose();
            _captureDevice?.Dispose();
            _uITapGestureRecognizer?.Dispose();
            _subjectAreaChangedNotificaion?.Dispose();
            _dispatchQueue?.Dispose();
        }
    }
}