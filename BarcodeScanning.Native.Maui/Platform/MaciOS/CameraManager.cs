using AVFoundation;
using CoreAnimation;
using CoreFoundation;
using CoreGraphics;
using CoreImage;
using CoreMedia;
using Foundation;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Platform;
using System.Diagnostics;
using UIKit;
using Vision;

namespace BarcodeScanning;

internal class CameraManager : IDisposable
{
    internal BarcodeView BarcodeView { get => _barcodeView; }
    internal bool CaptureNextFrame { get => _cameraView?.CaptureNextFrame ?? false; }

    private AVCaptureDevice _captureDevice;
    private AVCaptureInput _captureInput;
    private BarcodeAnalyzer _barcodeAnalyzer;

    private readonly AVCaptureVideoDataOutput _videoDataOutput;
    private readonly AVCaptureVideoPreviewLayer _previewLayer;
    private readonly AVCaptureSession _captureSession;
    private readonly BarcodeView _barcodeView;
    private readonly CameraView _cameraView;
    private readonly CAShapeLayer _shapeLayer;
    private readonly DispatchQueue _dispatchQueue;
    private readonly NSObject _subjectAreaChangedNotificaion;
    private readonly VNDetectBarcodesRequest _detectBarcodesRequest;
    private readonly VNSequenceRequestHandler _sequenceRequestHandler;
    private readonly UITapGestureRecognizer _uITapGestureRecognizer;

    private readonly HashSet<BarcodeResult> _barcodeResults = [];
    private const int aimRadius = 8;

    internal CameraManager(CameraView cameraView)
    {
        _cameraView = cameraView;
        
        _captureSession = new AVCaptureSession();
        _sequenceRequestHandler = new VNSequenceRequestHandler();
        _dispatchQueue = new DispatchQueue("com.barcodescanning.maui.sessionQueue", new DispatchQueue.Attributes()
        {
            QualityOfService = DispatchQualityOfService.UserInitiated
        });
        _videoDataOutput = new AVCaptureVideoDataOutput()
        {
            AlwaysDiscardsLateVideoFrames = true
        };
        _detectBarcodesRequest = new VNDetectBarcodesRequest((request, error) => 
        {
            if (error is null)
                Methods.ProcessBarcodeResult(request.GetResults<VNBarcodeObservation>(), _barcodeResults, _previewLayer);
        });

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
            
            if (_captureSession.Inputs.Length == 0)
                UpdateCamera();
            if (_captureSession.SessionPreset is null)
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

                UpdateOutput();
                UpdateAnalyzer();
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
            {
                CaptureDeviceLock(() => _captureDevice.TorchMode = AVCaptureTorchMode.Off);

                if (_cameraView is not null)
                    _cameraView.TorchOn = false;
            }

            if (_captureSession.Running)
                _dispatchQueue.DispatchAsync(_captureSession.StopRunning);
        }
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

    internal void UpdateAnalyzer()
    {
        if (_detectBarcodesRequest is not null)
            _detectBarcodesRequest.Symbologies = Methods.SelectedSymbologies(_cameraView.BarcodeSymbologies);
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
                
                CaptureDeviceLock(() => 
                {
                    _captureDevice.VideoZoomFactor = factor;
                    _cameraView.CurrentZoomFactor = factor;
                });
            }
        }
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
            _barcodeView?.Layer?.AddSublayer(_shapeLayer);
        else
            _shapeLayer?.RemoveFromSuperLayer();
    }

    internal void HandleTapToFocus() {}

    internal void PerformBarcodeDetection(CMSampleBuffer sampleBuffer)
    {
        if (_cameraView.PauseScanning)
            return;

        _barcodeResults.Clear();
        _sequenceRequestHandler?.Perform([_detectBarcodesRequest], sampleBuffer, out _);

        if (_cameraView.AimMode)
        {
            var previewCenter = new Point(_previewLayer.Bounds.Width / 2, _previewLayer.Bounds.Height / 2);

            foreach (var barcode in _barcodeResults)
            {
                if (!barcode.PreviewBoundingBox.Contains(previewCenter))
                    _barcodeResults.Remove(barcode);
            }
        }

        if (_cameraView.ViewfinderMode)
        {
            var previewRect = new RectF(0, 0, (float)_previewLayer.Bounds.Width, (float)_previewLayer.Bounds.Height);

            foreach (var barcode in _barcodeResults)
            {
                if (!previewRect.Contains(barcode.PreviewBoundingBox))
                    _barcodeResults.Remove(barcode);
            }
        }

        _cameraView.DetectionFinished(_barcodeResults);
    }

    internal void CaptureImage(CMSampleBuffer sampleBuffer)
    {
        _cameraView.CaptureNextFrame = false;
        using var imageBuffer = sampleBuffer.GetImageBuffer();
        using var cIImage = new CIImage(imageBuffer);
        using var cIContext = new CIContext();
        using var cGImage = cIContext.CreateCGImage(cIImage, cIImage.Extent);
        var image = new PlatformImage(new UIImage(cGImage));
        _cameraView.TriggerOnImageCaptured(image);
    }

    private void FocusOnTap()
    {
        if ((_cameraView?.TapToFocusEnabled ?? false) && _captureDevice is not null && _captureDevice.FocusPointOfInterestSupported)
        {
            CaptureDeviceLock(() =>
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
            CaptureDeviceLock(() => 
            {
                if (_captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
                    _captureDevice.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
                else if (_captureDevice.IsFocusModeSupported(AVCaptureFocusMode.AutoFocus))
                    _captureDevice.FocusMode = AVCaptureFocusMode.AutoFocus;
                
                _captureDevice.SubjectAreaChangeMonitoringEnabled = false;
            });
        }
    }

    private void UpdateOutput()
    {
        if (_videoDataOutput is not null)
        {
            _videoDataOutput.SetSampleBufferDelegate(null, null);
            _barcodeAnalyzer?.Dispose();
            _barcodeAnalyzer = new BarcodeAnalyzer(this);
            _videoDataOutput.SetSampleBufferDelegate(_barcodeAnalyzer, DispatchQueue.DefaultGlobalQueue);
        }
    }

    private void CaptureDeviceLock(Action action)
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
            _sequenceRequestHandler?.Dispose();
            _detectBarcodesRequest?.Dispose();
            _uITapGestureRecognizer?.Dispose();
            _subjectAreaChangedNotificaion?.Dispose();
            _dispatchQueue?.Dispose();
        }
    }
}