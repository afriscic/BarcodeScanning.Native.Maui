using AVFoundation;
using CoreAnimation;
using CoreFoundation;
using CoreGraphics;
using CoreImage;
using CoreMedia;
using Foundation;
using Microsoft.Maui.Graphics.Platform;
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
    private readonly NSObject _subjectAreaChangedNotificaion;
    private readonly VNDetectBarcodesRequest _detectBarcodesRequest;
    private readonly VNSequenceRequestHandler _sequenceRequestHandler;
    private readonly UITapGestureRecognizer _uITapGestureRecognizer;

    private readonly HashSet<BarcodeResult> _barcodeResults = [];
    private readonly object _syncLock = new();
    private readonly object _configLock = new();
    private const int aimRadius = 8;

    internal CameraManager(CameraView cameraView)
    {
        _cameraView = cameraView;
        
        _captureSession = new AVCaptureSession();
        _sequenceRequestHandler = new VNSequenceRequestHandler();
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
            VideoGravity = AVLayerVideoGravity.ResizeAspectFill,
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
                _captureSession.StopRunning();
            
            if (_captureSession.Inputs.Length == 0)
                UpdateCamera();
            if (_captureSession.SessionPreset is null)
                UpdateResolution();
            if (!_captureSession.Outputs.Contains(_videoDataOutput) && _captureSession.CanAddOutput(_videoDataOutput))
            {
                _captureSession.BeginConfiguration();
                _captureSession.AddOutput(_videoDataOutput);
                _captureSession.CommitConfiguration();
            }
            
            UpdateOutput();
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
        if (_detectBarcodesRequest is not null)
            _detectBarcodesRequest.Symbologies = Methods.SelectedSymbologies(_cameraView.BarcodeSymbologies);
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
        if (_cameraView is not null && _captureDevice is not null)
        {
            _cameraView.CurrentZoomFactor = (float)_captureDevice.VideoZoomFactor;
            _cameraView.MinZoomFactor = (float)_captureDevice.MinAvailableVideoZoomFactor;
            _cameraView.MaxZoomFactor = (float)_captureDevice.MaxAvailableVideoZoomFactor;
            _cameraView.DeviceSwitchZoomFactor = _captureDevice.VirtualDeviceSwitchOverVideoZoomFactors.Select(s => (float)s).ToArray();
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
        }
    }
}