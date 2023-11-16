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
    private PreviewView _previewView;
    private UITapGestureRecognizer _uITapGestureRecognizer;

    protected override UIView CreatePlatformView()
    {
        _captureSession = new AVCaptureSession
        {
            SessionPreset = GetCaptureSessionResolution()
        };
        _queue = new DispatchQueue("BarcodeScannerQueue", new DispatchQueue.Attributes()
        {
            QualityOfService = DispatchQualityOfService.UserInitiated
        });
        _videoPreviewLayer = new AVCaptureVideoPreviewLayer(_captureSession)
        {
            VideoGravity = AVLayerVideoGravity.ResizeAspectFill
        };
        _previewView = new PreviewView(_videoPreviewLayer);

        _uITapGestureRecognizer = new UITapGestureRecognizer(() => FocusOnTap());
        _previewView.AddGestureRecognizer(_uITapGestureRecognizer);

        return _previewView;
    }

    private void Start()
    {
        if (_captureSession is not null)
        {
            if (_captureSession.Running)
                _captureSession.StopRunning();

            UpdateCamera();
            UpdateAnalyzer();
            UpdateResolution();
            UpdateTorch();

            _captureSession.StartRunning();
        }
    }

    private void Stop()
    {
        if (_captureSession is not null)
        {
            DisableTorchIfNeeded();

            if (_captureSession.Running)
                _captureSession.StopRunning();
        }
    }

    private void HandleCameraEnabled()
    {
        if (VirtualView is not null)
        {
            if (VirtualView.CameraEnabled)
                Start();
            else
                Stop();
        }
    }

    private void UpdateResolution()
    {
        if (_captureSession is not null)
        {
            _captureSession.BeginConfiguration();
            _captureSession.SessionPreset = GetCaptureSessionResolution();
            _captureSession.CommitConfiguration();
        }
    }
    private void UpdateAnalyzer()
    {
        if (_captureSession is not null)
        {
            _captureSession.BeginConfiguration();

            if (_videoDataOutput is not null && _captureSession.Outputs.Length > 0 && _captureSession.Outputs.Contains(_videoDataOutput))
            {
                _captureSession.RemoveOutput(_videoDataOutput);
                _videoDataOutput.Dispose();
                _videoDataOutput = null;
            }

            _videoDataOutput = new AVCaptureVideoDataOutput()
            {
                AlwaysDiscardsLateVideoFrames = true,
                WeakVideoSettings = new CVPixelBufferAttributes { PixelFormatType = CVPixelFormatType.CV32BGRA }.Dictionary
            };

            _videoDataOutput.SetSampleBufferDelegate(new BarcodeAnalyzer(VirtualView, _videoPreviewLayer), _queue);

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

            if (_captureInput is not null && _captureSession.Inputs.Length > 0 && _captureSession.Inputs.Contains(_captureInput))
            {
                _captureSession.RemoveInput(_captureInput);
                _captureInput.Dispose();
                _captureInput = null;
            }

            if (_captureDevice is not null)
            {
                _captureDevice.Dispose();
                _captureDevice = null;
            }

            _captureDevice = AVCaptureDevice.GetDefaultDevice(
                AVCaptureDeviceType.BuiltInWideAngleCamera,
                AVMediaTypes.Video,
                VirtualView.CameraFacing == CameraFacing.Front ? AVCaptureDevicePosition.Front : AVCaptureDevicePosition.Back);

            if (_captureDevice is not null)
            {
                if (_captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
                    CaptureDeviceLock(() => _captureDevice.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus);
                else
                    CaptureDeviceLock(() => _captureDevice.FocusMode = AVCaptureFocusMode.AutoFocus);

                _captureInput = new AVCaptureDeviceInput(_captureDevice, out _);

                if (_captureInput is not null)
                {
                    if (_captureSession.CanAddInput(_captureInput))
                        _captureSession.AddInput(_captureInput);
                }
            }

            _captureSession.CommitConfiguration();
        }
    }

    private void UpdateTorch()
    {
        if (_captureDevice is not null && _captureDevice.HasTorch && _captureDevice.TorchAvailable)
            CaptureDeviceLock(() => _captureDevice.TorchMode = VirtualView.TorchOn ? AVCaptureTorchMode.On : AVCaptureTorchMode.Off);
    }

    private void FocusOnTap()
    {
        if (_captureDevice is not null && VirtualView.TapToFocusEnabled && _captureDevice.FocusPointOfInterestSupported)
        {
            CaptureDeviceLock(() => _captureDevice.FocusPointOfInterest = _videoPreviewLayer.CaptureDevicePointOfInterestForPoint(_uITapGestureRecognizer.LocationInView(_previewView)));

            if (_captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
                CaptureDeviceLock(() => _captureDevice.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus);
            else
                CaptureDeviceLock(() => _captureDevice.FocusMode = AVCaptureFocusMode.AutoFocus);
        }
    }

    private void DisableTorchIfNeeded()
    {
        if (_captureDevice is not null && _captureDevice.TorchMode == AVCaptureTorchMode.On)
            CaptureDeviceLock(() => _captureDevice.TorchMode = AVCaptureTorchMode.Off);
    }

    private NSString GetCaptureSessionResolution()
    {
        return VirtualView.CaptureQuality switch
        {
            CaptureQuality.Lowest => AVCaptureSession.Preset352x288,
            CaptureQuality.Low => AVCaptureSession.Preset640x480,
            CaptureQuality.Medium => AVCaptureSession.Preset1280x720,
            CaptureQuality.High => AVCaptureSession.Preset1920x1080,
            CaptureQuality.Highest => AVCaptureSession.Preset3840x2160,
            _ => throw new ArgumentOutOfRangeException(nameof(VirtualView.CaptureQuality))
        };
    }
    private void CaptureDeviceLock(Action handler)
    {
        if (_captureDevice.LockForConfiguration(out _))
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
}
