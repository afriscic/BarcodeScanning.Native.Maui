using AVFoundation;
using BarcodeScanning.Platforms.iOS;
using CoreFoundation;
using CoreVideo;
using Foundation;
using UIKit;

namespace BarcodeScanning;

public partial class CameraViewHandler
{
    private AVCaptureVideoPreviewLayer _videoPreviewLayer;
    private AVCaptureVideoDataOutput _videoDataOutput;
    private AVCaptureSession _captureSession;
    private AVCaptureDevice _captureDevice;
    private AVCaptureInput _captureInput;
    private DispatchQueue _queue;
    private BarcodeAnalyzer _barcodeAnalyzer;
    private BarcodeView _barcodeView;
    private UITapGestureRecognizer _uITapGestureRecognizer;

    protected override UIView CreatePlatformView()
    {
        _captureSession = new AVCaptureSession();
        _uITapGestureRecognizer = new UITapGestureRecognizer(FocusOnTap);
        _queue = new DispatchQueue("BarcodeScannerQueue", new DispatchQueue.Attributes()
        {
            QualityOfService = DispatchQualityOfService.UserInitiated
        });
        _videoPreviewLayer = new AVCaptureVideoPreviewLayer(_captureSession)
        {
            VideoGravity = AVLayerVideoGravity.ResizeAspectFill
        };
        _barcodeView = new BarcodeView(_videoPreviewLayer);
        _barcodeView.AddGestureRecognizer(_uITapGestureRecognizer);

        return _barcodeView;
    }

    private void Start()
    {
        if (_captureSession is not null)
        {
            if (_captureSession.Running)
                _captureSession.StopRunning();

            _captureSession.StartRunning();
        }
    }

    private void Stop()
    {
        if (_captureSession is not null)
        {
            if (_captureDevice is not null && _captureDevice.TorchMode == AVCaptureTorchMode.On)
                CaptureDeviceLock(() => _captureDevice.TorchMode = AVCaptureTorchMode.Off);

            if (_captureSession.Running)
                _captureSession.StopRunning();
        }
    }

    private void HandleCameraEnabled()
    {
        if (VirtualView?.CameraEnabled ?? false)
            Start();
        else
            Stop();
    }

    private void UpdateResolution()
    {
        if (_captureSession is not null)
        {
            var quality = VirtualView?.CaptureQuality ?? CaptureQuality.Medium;

            _captureSession.BeginConfiguration();

            while (!_captureSession.CanSetSessionPreset(GetCaptureSessionResolution(quality)) && quality != CaptureQuality.Low)
            {
                quality -= 1;
            }

            _captureSession.SessionPreset = GetCaptureSessionResolution(quality);
            _captureSession.CommitConfiguration();
        }
    }
    private void UpdateAnalyzer()
    {
        if (_captureSession is not null)
        {
            _captureSession.BeginConfiguration();

            if (_videoDataOutput is not null)
            {
                if (_captureSession.Outputs.Length > 0 && _captureSession.Outputs.Contains(_videoDataOutput))
                    _captureSession.RemoveOutput(_videoDataOutput);

                _videoDataOutput.Dispose();
            }

            _videoDataOutput = new AVCaptureVideoDataOutput()
            {
                AlwaysDiscardsLateVideoFrames = true,
                WeakVideoSettings = new CVPixelBufferAttributes { PixelFormatType = CVPixelFormatType.CV32BGRA }.Dictionary
            };

            if (VirtualView is not null && _videoPreviewLayer is not null && _queue is not null)
            {
                _barcodeAnalyzer?.Dispose();
                _barcodeAnalyzer = new BarcodeAnalyzer(VirtualView, _videoPreviewLayer);
                _videoDataOutput.SetSampleBufferDelegate(_barcodeAnalyzer, _queue);
            }

            if (_captureSession.CanAddOutput(_videoDataOutput))
                _captureSession.AddOutput(_videoDataOutput);

            _captureSession.CommitConfiguration();
        }
    }
    private void UpdateCamera()
    {
        if (_captureSession is not null)
        {
            _captureSession.BeginConfiguration();

            if (_captureInput is not null)
            {
                if (_captureSession.Inputs.Length > 0 && _captureSession.Inputs.Contains(_captureInput))
                    _captureSession.RemoveInput(_captureInput);

                _captureInput.Dispose();
            }

            _captureDevice?.Dispose();
            _captureDevice = AVCaptureDevice.GetDefaultDevice(
                AVCaptureDeviceType.BuiltInWideAngleCamera,
                AVMediaTypes.Video,
                VirtualView?.CameraFacing == CameraFacing.Front ? AVCaptureDevicePosition.Front : AVCaptureDevicePosition.Back);
            _captureDevice ??= AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);

            if (_captureDevice is not null)
            {
                if (_captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
                    CaptureDeviceLock(() => _captureDevice.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus);
                else
                    CaptureDeviceLock(() => _captureDevice.FocusMode = AVCaptureFocusMode.AutoFocus);

                _captureInput = new AVCaptureDeviceInput(_captureDevice, out _);

                if (_captureSession.CanAddInput(_captureInput))
                    _captureSession.AddInput(_captureInput);
            }

            UpdateResolution();

            _captureSession.CommitConfiguration();
        }
    }

    private void UpdateTorch()
    {
        if (_captureDevice is not null && _captureDevice.HasTorch && _captureDevice.TorchAvailable)
        {
            if (VirtualView?.TorchOn ?? false)
                CaptureDeviceLock(() => _captureDevice.TorchMode = AVCaptureTorchMode.On);
            else
                CaptureDeviceLock(() => _captureDevice.TorchMode = AVCaptureTorchMode.Off);
        }
    }

    private void HandleAimModeEnabled()
    {
        if (_barcodeView is not null && VirtualView is not null)
        {
            if (VirtualView.AimMode)
                _barcodeView.AddAimingDot();
            else
                _barcodeView.RemoveAimingDot();
        }
    }

    private void FocusOnTap()
    {
        if (_captureDevice is not null && (VirtualView?.TapToFocusEnabled ?? false) && _captureDevice.FocusPointOfInterestSupported)
        {
            CaptureDeviceLock(() =>
            {
                _captureDevice.FocusPointOfInterest = _videoPreviewLayer.CaptureDevicePointOfInterestForPoint(_uITapGestureRecognizer.LocationInView(_barcodeView));
                if (_captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
                    _captureDevice.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
                else
                    _captureDevice.FocusMode = AVCaptureFocusMode.AutoFocus;
            });
        }
    }

    private NSString GetCaptureSessionResolution(CaptureQuality quality)
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
        try
        {
            if (_captureDevice.LockForConfiguration(out _))
                handler();
        }
        catch (Exception)
        {
                
        }
        finally
        {
            _captureDevice?.UnlockForConfiguration();
        }
    }

    private void DisposeView()
    {
        _barcodeView?.Dispose();
        _uITapGestureRecognizer?.Dispose();
        _videoPreviewLayer?.Dispose();
        _captureSession?.Dispose();
        _videoDataOutput?.Dispose();
        _captureInput?.Dispose();
        _captureDevice?.Dispose();
        _barcodeAnalyzer?.Dispose();
        _queue?.Dispose();
    }
}
