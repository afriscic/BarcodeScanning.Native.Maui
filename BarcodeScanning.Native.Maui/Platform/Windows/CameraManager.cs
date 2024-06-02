using Microsoft.UI.Xaml.Controls;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;

using Border = Microsoft.UI.Xaml.Controls.Border;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace BarcodeScanning;

internal class CameraManager : IDisposable
{
    internal BarcodeView BarcodeView { get => _barcodeView; }

    private readonly BarcodeView _barcodeView;
    private readonly Border _aimDot;
    private readonly CameraView _cameraView;
    private MediaCapture _mediaCapture;
    private MediaFrameSourceInfo _selectedCamera;
    private MediaFrameReader _mediaFrameReader;
    private readonly MediaPlayerElement _mediaPlayerElement;
    private readonly SemaphoreSlim _mediaCaptureLock;

    private const int aimDotRadius = 20;

    internal CameraManager(CameraView cameraView)
    {
        _cameraView = cameraView;
        _mediaCaptureLock = new SemaphoreSlim(1);

        _barcodeView = new BarcodeView();
        
        _mediaPlayerElement = new MediaPlayerElement()
        {
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
            Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
        };
        _aimDot = new Border()
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(156, 255, 0, 0)),
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
            Width = aimDotRadius,
            Height = aimDotRadius,
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(aimDotRadius / 2)
        };

        _barcodeView.Children.Add(_mediaPlayerElement);
    }

    internal void Start()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_selectedCamera is null)
                UpdateCamera();

            if (_mediaCapture is null)
            {
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings()
                {
                    SourceGroup = await MediaFrameSourceGroup.FromIdAsync(_selectedCamera.Id),
                    SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu
                });

                UpdateResolution();

                _mediaPlayerElement.Source = MediaSource.CreateFromMediaFrameSource(_mediaCapture?.FrameSources[_selectedCamera?.Id]);
            }

            if (_mediaFrameReader is null)
            {
                _mediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(_mediaCapture?.FrameSources[_selectedCamera?.Id], MediaEncodingSubtypes.Argb32);
                _mediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
                //_mediaFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
            }

            await _mediaFrameReader.StartAsync();
        });
    }

    internal void Stop()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_mediaCapture?.VideoDeviceController?.TorchControl?.Enabled ?? false)
            {
                _mediaCapture.VideoDeviceController.TorchControl.Enabled = false;

                if (_cameraView is not null)
                    _cameraView.TorchOn = false;
            }

            if (_mediaFrameReader is not null)
                await _mediaFrameReader.StopAsync();
        });
    }

    internal void UpdateCamera()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var mediaFrameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();

            MediaFrameSourceInfo newCamera = null;
            if (_cameraView?.CameraFacing == CameraFacing.Front)
            {
                newCamera = mediaFrameSourceGroups
                    .SelectMany(s => s.SourceInfos)
                    .Where(w => w.MediaStreamType == MediaStreamType.VideoRecord && w.DeviceInformation?.EnclosureLocation?.Panel == Windows.Devices.Enumeration.Panel.Front)
                    .FirstOrDefault();
            }
            newCamera ??= mediaFrameSourceGroups
                .SelectMany(s => s.SourceInfos)
                .Where(w => w.MediaStreamType == MediaStreamType.VideoRecord)
                .FirstOrDefault();

            if (newCamera is null)
                return;

            if (_selectedCamera != newCamera && _mediaCapture is not null)
            {
                if (_mediaCapture.CameraStreamState == CameraStreamState.Streaming)
                    await _mediaFrameReader?.StopAsync();

                //_mediaFrameReader.FrameArrived -= ColorFrameReader_FrameArrived;
                _mediaFrameReader.Dispose();
                _mediaCapture.Dispose();

                _selectedCamera = newCamera;

                ReportZoomFactors();
            }
        });
    }

    internal void UpdateTorch()
    {
        if (_mediaCapture?.VideoDeviceController?.TorchControl?.Supported ?? false)
            _mediaCapture.VideoDeviceController.TorchControl.Enabled = _cameraView?.TorchOn ?? false;
    }

    internal void UpdateZoomFactor()
    {
        if (_mediaCapture?.VideoDeviceController?.ZoomControl?.Supported ?? false)
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

            if (factor > 0)
                _mediaCapture.VideoDeviceController.ZoomControl.Value = factor;
        }

        ReportZoomFactors();
    }

    internal void UpdateResolution()
    {
        if (_selectedCamera is null)
            return;
        //TODO resolution translator
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var frameSource = _mediaCapture?.FrameSources[_selectedCamera?.Id];
            var preferredFormat = frameSource.SupportedFormats
                .Where(w => w.VideoFormat.Width == 1280 && w.VideoFormat.Height == 720)
                .OrderByDescending(o => (decimal)o.FrameRate.Numerator / (decimal)o.FrameRate.Denominator)
                .FirstOrDefault();

            if (preferredFormat is not null)
                await frameSource.SetFormatAsync(preferredFormat);
        });
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
            _barcodeView?.Children.Add(_aimDot);
        else
            _barcodeView?.Children.Remove(_aimDot);
    }

    private void ReportZoomFactors()
    {
        if (_cameraView is not null && (_mediaCapture?.VideoDeviceController?.ZoomControl?.Supported ?? false))
        {
            _cameraView.CurrentZoomFactor = _mediaCapture.VideoDeviceController.ZoomControl.Value;
            _cameraView.MinZoomFactor = _mediaCapture.VideoDeviceController.ZoomControl.Min;
            _cameraView.MaxZoomFactor = _mediaCapture.VideoDeviceController.ZoomControl.Max;
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

        }
    }
}
