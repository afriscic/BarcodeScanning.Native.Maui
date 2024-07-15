using Microsoft.Graphics.Canvas;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using ZXingCpp;


using Border = Microsoft.UI.Xaml.Controls.Border;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace BarcodeScanning;

internal partial class CameraManager : IDisposable
{
    internal BarcodeView BarcodeView { get => _barcodeView; }

    private readonly BarcodeView _barcodeView;
    private readonly Border _aimDot;
    private readonly CameraView _cameraView;
    private readonly BarcodeReader _barcodeReader; 
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
        _barcodeReader = new BarcodeReader
        {
            TryHarder = true,
            TryRotate = true,
            TryDownscale = true,
            IsPure = false,
            TextMode = TextMode.Plain
        };

        _mediaPlayerElement.Tapped += _mediaPlayerElement_Tapped;

        _barcodeView.Children.Add(_mediaPlayerElement);
    }

    private async void _mediaPlayerElement_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (!_cameraView?.TapToFocusEnabled ?? true)
            return;

        var regionsOfInterestControl = _mediaCapture?.VideoDeviceController?.RegionsOfInterestControl;
        if (regionsOfInterestControl is null || !regionsOfInterestControl.AutoFocusSupported || regionsOfInterestControl.MaxRegions < 1)
            return;

        var focusControl = _mediaCapture?.VideoDeviceController?.FocusControl;
        if (focusControl is null || !focusControl.Supported)
            return;

        var tapPosition = e.GetPosition(_mediaPlayerElement);
        var x = tapPosition.X / _mediaPlayerElement.ActualWidth;
        var y = tapPosition.Y / _mediaPlayerElement.ActualHeight;

        x = Math.Max(0, Math.Min(x, 1));
        y = Math.Max(0, Math.Min(y, 1));

        var regionOfInterest = new RegionOfInterest
        {
            AutoFocusEnabled = regionsOfInterestControl.AutoFocusSupported,
            BoundsNormalized = true,
            Bounds = new Windows.Foundation.Rect(x - 0.05, y - 0.05, 0.1, 0.1),
            Type = RegionOfInterestType.Unknown,
            Weight = 100
        };

        var focusRange = focusControl.SupportedFocusRanges.Contains(AutoFocusRange.FullRange) ? AutoFocusRange.FullRange : focusControl.SupportedFocusRanges.FirstOrDefault();
        var focusMode = focusControl.SupportedFocusModes.Contains(FocusMode.Continuous) ? FocusMode.Continuous : focusControl.SupportedFocusModes.FirstOrDefault();

        var focusSettings = new FocusSettings
        {
            AutoFocusRange = focusRange,
            Mode = focusMode,
            DisableDriverFallback = true,
            WaitForFocus = false
        };

        focusControl.Configure(focusSettings);
        await regionsOfInterestControl.ClearRegionsAsync();
        await regionsOfInterestControl.SetRegionsAsync([regionOfInterest]);
        await focusControl.FocusAsync();
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
                var mediaPlayer = _mediaPlayerElement.MediaPlayer;

            }

            if (_mediaFrameReader is null)
            {
                _mediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(_mediaCapture?.FrameSources[_selectedCamera?.Id], MediaEncodingSubtypes.Bgra8);
                _mediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
                _mediaFrameReader.FrameArrived += _mediaFrameReader_FrameArrived;
            }

            await _mediaFrameReader.StartAsync();
        });
    }

    private void _mediaFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {

        if (sender is not null)
        {
            _barcodeReader.TryInvert = _cameraView?.ForceInverted ?? false;

            using var frame = sender.TryAcquireLatestFrame();
            using var videoBitmap = frame?.VideoMediaFrame?.SoftwareBitmap;

            if (videoBitmap is not null && _cameraView is not null)
            {
                if (_cameraView.CaptureNextFrame)
                    CaptureImage(videoBitmap);
                else
                    PerformBarcodeDetection(videoBitmap);
            }
        }
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
                    .Where(w => w.MediaStreamType == MediaStreamType.VideoRecord && w.SourceKind == MediaFrameSourceKind.Color && w.DeviceInformation?.EnclosureLocation?.Panel == Windows.Devices.Enumeration.Panel.Front)
                    .FirstOrDefault();
            }
            newCamera ??= mediaFrameSourceGroups
                .SelectMany(s => s.SourceInfos)
                .Where(w => w.MediaStreamType == MediaStreamType.VideoRecord && w.SourceKind == MediaFrameSourceKind.Color)
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

                Start();
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

            factor = Math.Max(factor, _cameraView?.MinZoomFactor ?? -1);
            factor = Math.Min(factor, _cameraView?.MaxZoomFactor ?? -1);

            if (factor > 0)
                _mediaCapture.VideoDeviceController.ZoomControl.Value = factor;
        }

        ReportZoomFactors();
    }

    internal void UpdateResolution()
    {
        if (_selectedCamera is null)
            return;
        //TODO resolution translator & fallback
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

    internal void UpdateSymbologies()
    {
        if (_cameraView is not null)
            _barcodeReader.Formats = Methods.ConvertBarcodeFormats(_cameraView.BarcodeSymbologies);
    }

    internal void UpdateCameraEnabled()
    {
        if (_cameraView?.CameraEnabled ?? false)
            Start();
        else
            Stop();
    }

    internal void UpdateAimMode()
    {
        if (_cameraView?.AimMode ?? false)
            _barcodeView?.Children.Add(_aimDot);
        else
            _barcodeView?.Children.Remove(_aimDot);
    }

    internal void UpdateTapToFocus() { }

    private void ReportZoomFactors()
    {
        if (_cameraView is not null && (_mediaCapture?.VideoDeviceController?.ZoomControl?.Supported ?? false))
        {
            _cameraView.CurrentZoomFactor = _mediaCapture.VideoDeviceController.ZoomControl.Value;
            _cameraView.MinZoomFactor = _mediaCapture.VideoDeviceController.ZoomControl.Min;
            _cameraView.MaxZoomFactor = _mediaCapture.VideoDeviceController.ZoomControl.Max;
        }
    }

    private void CaptureImage(SoftwareBitmap bitmap)
    {
        _cameraView.CaptureNextFrame = false;
        var device = CanvasDevice.GetSharedDevice();
        var image = new PlatformImage(device, CanvasBitmap.CreateFromSoftwareBitmap(device, bitmap));
        _cameraView.TriggerOnImageCaptured(image);
    }

    private void PerformBarcodeDetection(SoftwareBitmap bitmap)
    {
        using var buffer = bitmap.LockBuffer(BitmapBufferAccessMode.Read);
        using var reference = buffer.CreateReference();

        List<Barcode> barcodes = [];
        unsafe
        {
            ((Methods.IMemoryBufferByteAccess)reference).GetBuffer(out byte* dataInBytes, out uint capacity);
            var iv = new ImageView(new IntPtr(dataInBytes), bitmap.PixelWidth, bitmap.PixelHeight, Methods.ConvertImageFormats(bitmap.BitmapPixelFormat));
            barcodes = _barcodeReader.From(iv);
        }

        foreach (var barcode in barcodes)
        {
            barcode.Position.
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