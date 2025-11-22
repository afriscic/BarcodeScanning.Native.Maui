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
    private Size _previewSize;
    private readonly object _previewSizeLock = new object();

    private const int AimDotRadius = 20;

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
            Width = AimDotRadius,
            Height = AimDotRadius,
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(AimDotRadius / 2)
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
        _mediaPlayerElement.SizeChanged += (s, e) => _mediaPlayerElement_SizeChanged();

        _barcodeView.Children.Add(_mediaPlayerElement);
    }

    internal void Start() => MainThread.BeginInvokeOnMainThread(async () => await StartInternalAsync());

    internal void Stop() => MainThread.BeginInvokeOnMainThread(async () => await StopInternalAsync());

    internal void UpdateCamera()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var newCamera = await GetCameraAsync();
            if (newCamera is null || _selectedCamera?.Id == newCamera.Id)
                return;

            await StopInternalAsync();
            await StartInternalAsync();
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

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var frameSource = _mediaCapture?.FrameSources[_selectedCamera?.Id];
            if (frameSource is null)
                return;

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

    internal void UpdateBackgroundColor() { }

    internal void UpdateTapToFocus() { }

    internal void UpdateVibration() { }

    private async Task StartInternalAsync()
    {
        await _mediaCaptureLock.WaitAsync();

        try
        {
            if (_mediaCapture is not null)
                return;

            var camera = await GetCameraAsync();
            if (camera is null)
                return;

            _selectedCamera = camera;

            await InitializeMediaCaptureAsync();
            _mediaPlayerElement.Source = MediaSource.CreateFromMediaFrameSource(_mediaCapture.FrameSources[_selectedCamera.Id]);
            _mediaPlayerElement.MediaPlayer.Play();
            await InitializeFrameReaderAsync();

            _mediaPlayerElement_SizeChanged();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start camera: {ex.Message}");
        }
        finally
        {
            _mediaCaptureLock.Release();
        }
    }

    private async Task StopInternalAsync()
    {
        await _mediaCaptureLock.WaitAsync();

        try
        {
            if (_mediaCapture is null)
                return;

            await DisableTorchIfNeededAsync();
            await CleanupFrameReaderAsync();
            CleanupMediaCapture();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to stop camera: {ex.Message}");
        }
        finally
        {
            _mediaCaptureLock.Release();
        }
    }

    private async Task InitializeMediaCaptureAsync()
    {
        _mediaCapture = new MediaCapture();

        await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings()
        {
            SourceGroup = _selectedCamera.SourceGroup,
            SharingMode = MediaCaptureSharingMode.ExclusiveControl,
            StreamingCaptureMode = StreamingCaptureMode.Video,
            MemoryPreference = MediaCaptureMemoryPreference.Cpu
        });

        UpdateResolution();
        ReportZoomFactors();
    }

    private async Task InitializeFrameReaderAsync()
    {
        _mediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(
            _mediaCapture.FrameSources[_selectedCamera.Id],
            MediaEncodingSubtypes.Bgra8);

        _mediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
        _mediaFrameReader.FrameArrived += _mediaFrameReader_FrameArrived;
        await _mediaFrameReader.StartAsync();
    }

    private async Task DisableTorchIfNeededAsync()
    {
        if (_mediaCapture?.VideoDeviceController?.TorchControl?.Supported ?? false && (_mediaCapture.VideoDeviceController?.TorchControl?.Enabled ?? false))
        {
            _mediaCapture.VideoDeviceController.TorchControl.Enabled = false;
            if (_cameraView is not null)
                _cameraView.TorchOn = false;
        }
    }

    private async Task CleanupFrameReaderAsync()
    {
        if (_mediaFrameReader is not null)
        {
            await _mediaFrameReader.StopAsync();
            _mediaFrameReader.FrameArrived -= _mediaFrameReader_FrameArrived;
            _mediaFrameReader.Dispose();
            _mediaFrameReader = null;
        }
    }

    private void CleanupMediaCapture()
    {
        _mediaCapture?.Dispose();
        _mediaCapture = null;
        _selectedCamera = null;
    }

    private void _mediaFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (sender is null)
            return;

        _barcodeReader.TryInvert = _cameraView?.ForceInverted ?? false;

        using var frame = sender.TryAcquireLatestFrame();
        using var softwareBitmap = frame?.VideoMediaFrame?.SoftwareBitmap;

        if (softwareBitmap is not null && _cameraView is not null)
        {
            if (_cameraView.CaptureNextFrame)
                CaptureImage(softwareBitmap);
            else
                PerformBarcodeDetection(softwareBitmap);
        }
    }

    private async Task<MediaFrameSourceInfo> GetCameraAsync()
    {
        var mediaFrameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();
        var preferredPanel = _cameraView?.CameraFacing == CameraFacing.Front
            ? Windows.Devices.Enumeration.Panel.Front
            : Windows.Devices.Enumeration.Panel.Back;

        return FindCameraByPanel(mediaFrameSourceGroups, preferredPanel)
            ?? FindFirstAvailableCamera(mediaFrameSourceGroups);
    }

    private static MediaFrameSourceInfo FindCameraByPanel(
        IEnumerable<MediaFrameSourceGroup> groups,
        Windows.Devices.Enumeration.Panel panel)
    {
        return groups
            .SelectMany(g => g.SourceInfos)
            .FirstOrDefault(s => s.MediaStreamType == MediaStreamType.VideoRecord &&
                                 s.SourceKind == MediaFrameSourceKind.Color &&
                                 s.DeviceInformation?.EnclosureLocation?.Panel == panel);
    }

    private static MediaFrameSourceInfo FindFirstAvailableCamera(IEnumerable<MediaFrameSourceGroup> groups)
    {
        return groups
            .SelectMany(g => g.SourceInfos)
            .FirstOrDefault(s => s.MediaStreamType == MediaStreamType.VideoRecord &&
                                 s.SourceKind == MediaFrameSourceKind.Color);
    }

    private void ReportZoomFactors()
    {
        if (_cameraView is not null && (_mediaCapture?.VideoDeviceController?.ZoomControl?.Supported ?? false))
        {
            var zoomControl = _mediaCapture.VideoDeviceController.ZoomControl;
            _cameraView.CurrentZoomFactor = zoomControl.Value;
            _cameraView.MinZoomFactor = zoomControl.Min;
            _cameraView.MaxZoomFactor = zoomControl.Max;
        }
    }

    private void _mediaPlayerElement_SizeChanged()
    {
        if (_mediaPlayerElement?.DispatcherQueue is null)
            return;

        _mediaPlayerElement.DispatcherQueue.TryEnqueue(() =>
        {
            lock (_previewSizeLock)
            {
                _previewSize = new Size(_mediaPlayerElement.ActualWidth, _mediaPlayerElement.ActualHeight);
            }
        });
    }

    private async void _mediaPlayerElement_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (!(_cameraView?.TapToFocusEnabled ?? false))
            return;

        var regionsOfInterestControl = _mediaCapture?.VideoDeviceController?.RegionsOfInterestControl;
        if (regionsOfInterestControl is not null && regionsOfInterestControl.AutoFocusSupported && regionsOfInterestControl.MaxRegions >= 1)
            return;

        var focusControl = _mediaCapture?.VideoDeviceController?.FocusControl;
        if (focusControl is not null && focusControl.Supported)
            return;

        var tapPosition = e.GetPosition(_mediaPlayerElement);
        var x = tapPosition.X / _mediaPlayerElement.ActualWidth;
        var y = tapPosition.Y / _mediaPlayerElement.ActualHeight;

        var normalizedX = Math.Max(0, Math.Min(x, 1));
        var normalizedY = Math.Max(0, Math.Min(y, 1));

        var regionOfInterest = new RegionOfInterest
        {
            AutoFocusEnabled = regionsOfInterestControl.AutoFocusSupported,
            BoundsNormalized = true,
            Bounds = new Windows.Foundation.Rect(x - 0.05, y - 0.05, 0.1, 0.1),
            Type = RegionOfInterestType.Unknown,
            Weight = 100
        };

        var focusRange = focusControl.SupportedFocusRanges.Contains(AutoFocusRange.FullRange)
            ? AutoFocusRange.FullRange
            : focusControl.SupportedFocusRanges.FirstOrDefault();

        var focusMode = focusControl.SupportedFocusModes.Contains(FocusMode.Continuous)
            ? FocusMode.Continuous
            : focusControl.SupportedFocusModes.FirstOrDefault();

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

        Barcode[] barcodes = [];
        unsafe
        {
            ((Methods.IMemoryBufferByteAccess)reference).GetBuffer(out byte* dataInBytes, out uint capacity);
            var iv = new ImageView(new IntPtr(dataInBytes), bitmap.PixelWidth, bitmap.PixelHeight, Methods.ConvertImageFormats(bitmap.BitmapPixelFormat));
            barcodes = _barcodeReader.From(iv);
        }

        if (barcodes.Length > 0)
        {
            var imageSize = new Size(bitmap.PixelWidth, bitmap.PixelHeight);
            Size previewSize;
            lock (_previewSizeLock)
            {
                previewSize = _previewSize;
            }

            var barcodeResults = barcodes
                .Select(b => b.AsBarcodeResult(imageSize, previewSize))
                .ToHashSet();

            _cameraView.DetectionFinished(barcodeResults);
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
        }
    }
}