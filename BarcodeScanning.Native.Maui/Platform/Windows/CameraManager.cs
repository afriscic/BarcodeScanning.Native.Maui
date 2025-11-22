using Microsoft.Graphics.Canvas;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Diagnostics;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.Devices;
using ZXingCpp;

using Border = Microsoft.UI.Xaml.Controls.Border;
using Color = Windows.UI.Color;
using CornerRadius = Microsoft.UI.Xaml.CornerRadius;
using HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using Stretch = Microsoft.UI.Xaml.Media.Stretch;
using VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;

namespace BarcodeScanning;

internal partial class CameraManager : IAsyncDisposable
{
    internal BarcodeView? BarcodeView { get => _barcodeView; }

    private readonly BarcodeView? _barcodeView;
    private readonly BarcodeReader _barcodeReader;
    private readonly Border _aimDot;
    private readonly CameraView? _cameraView;
    private readonly HashSet<BarcodeResult> _barcodeResults;
    private readonly Lock _previewRectLock;
    private readonly MediaPlayerElement _mediaPlayerElement;
    private readonly SemaphoreSlim _mediaCaptureLock;

    private MediaCapture? _mediaCapture;
    private MediaFrameReader? _mediaFrameReader;
    private MediaFrameSourceInfo? _selectedCamera;
    private Rect _previewRect;

    private const int AimDotRadius = 20;

    internal CameraManager(CameraView cameraView)
    {
        _cameraView = cameraView;

        _mediaCaptureLock = new SemaphoreSlim(1);
        _previewRectLock = new Lock();
        _barcodeResults = [];

        _mediaPlayerElement = new MediaPlayerElement()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Stretch = Stretch.UniformToFill
        };
        _mediaPlayerElement.Tapped += MediaPlayerElement_Tapped;
        _mediaPlayerElement.SizeChanged += MediaPlayerElement_SizeChanged;

        _aimDot = new Border()
        {
            Background = _cameraView?.AimIndicatorColor.ToPlatform() ?? new SolidColorBrush(Color.FromArgb(156, 255, 0, 0)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Width = AimDotRadius,
            Height = AimDotRadius,
            CornerRadius = new CornerRadius(AimDotRadius * 0.5)
        };

        _barcodeReader = new BarcodeReader
        {
            TryHarder = true,
            TryRotate = true,
            TryDownscale = true,
            IsPure = false,
            TextMode = TextMode.Plain
        };

        _barcodeView = new BarcodeView();
        _barcodeView.Children.Add(_mediaPlayerElement);
    }

    private async Task Start()
    {
        try
        {
            await _mediaCaptureLock.WaitAsync();

            if (_mediaCapture is not null)
                return;

            if (_selectedCamera is null)
            {
                var camera = await GetCameraAsync();
                if (camera is null)
                    return;

                _selectedCamera = camera;
            }

            _mediaCapture = new MediaCapture();
            await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings()
            {
                SourceGroup = _selectedCamera.SourceGroup,
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
            });

            await SetNewResolution();

            _mediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(_mediaCapture.FrameSources[_selectedCamera.Id]);
            _mediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
            _mediaFrameReader.FrameArrived += MediaFrameReader_FrameArrived;
            await _mediaFrameReader.StartAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _mediaPlayerElement?.Source = MediaSource.CreateFromMediaFrameSource(_mediaCapture.FrameSources[_selectedCamera.Id]);
                _mediaPlayerElement?.MediaPlayer.Play();
                UpdatePreviewRect();
            });

            ReportZoomFactors();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to start camera: {ex.Message}");
        }
        finally
        {
            _mediaCaptureLock?.Release();
        }
    }

    private async Task Stop()
    {
        try
        {
            await _mediaCaptureLock.WaitAsync();

            var torchControl = _mediaCapture?.VideoDeviceController?.TorchControl;
            if (torchControl is not null && torchControl.Supported && torchControl.Enabled)
            {
                torchControl.Enabled = false;
                _cameraView?.TorchOn = false;
            }

            if (_mediaFrameReader is not null)
            {
                await _mediaFrameReader.StopAsync();
                _mediaFrameReader.FrameArrived -= MediaFrameReader_FrameArrived;
                _mediaFrameReader.Dispose();
                _mediaFrameReader = null;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _mediaPlayerElement?.MediaPlayer.Pause();
                _mediaPlayerElement?.Source = null;
            });

            _mediaCapture?.Dispose();
            _mediaCapture = null;
            _selectedCamera = null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to stop camera: {ex.Message}");
        }
        finally
        {
            _mediaCaptureLock?.Release();
        }
    }

    internal void UpdateAimMode()
    {
        if (_cameraView?.AimMode ?? false)
            _barcodeView?.Children.Add(_aimDot);
        else
            _barcodeView?.Children.Remove(_aimDot);
    }

    internal void UpdateBackgroundColor() 
    {
        _mediaPlayerElement.Background = _cameraView?.BackgroundColor?.ToPlatform() ?? new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
    }

    internal async void UpdateCamera()
    {
        var newCamera = await GetCameraAsync();
        if (newCamera is null || _selectedCamera?.Id == newCamera.Id)
            return;

        await Stop();
        _selectedCamera = newCamera;
        await Start();
    }

    internal async void UpdateCameraEnabled()
    {
        if (_cameraView?.CameraEnabled ?? false)
            await Start();
        else
            await Stop();
    }

    internal async void UpdateResolution()
    {
        await SetNewResolution();
    }

    internal void UpdateSymbologies()
    {
        if (_cameraView is not null)
            _barcodeReader?.Formats = Methods.ConvertBarcodeFormats(_cameraView.BarcodeSymbologies);
    }

    internal void UpdateTapToFocus() { }

    internal void UpdateTorch()
    {
        if (_mediaCapture?.VideoDeviceController?.TorchControl?.Supported ?? false)
            _mediaCapture.VideoDeviceController.TorchControl.Enabled = _cameraView?.TorchOn ?? false;
    }

    internal void UpdateVibration() { }

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

    private void MediaFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        try
        {
            using var frame = sender?.TryAcquireLatestFrame();
            using var softwareBitmap = frame?.VideoMediaFrame?.SoftwareBitmap;

            ArgumentNullException.ThrowIfNull(softwareBitmap);
            ArgumentNullException.ThrowIfNull(_barcodeReader);

            var bitmap = softwareBitmap.BitmapPixelFormat switch
            {
                BitmapPixelFormat.Bgra8 => softwareBitmap,
                BitmapPixelFormat.Rgba8 => softwareBitmap,
                BitmapPixelFormat.Gray8 => softwareBitmap,
                _ => SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Gray8)
            };

            using var buffer = bitmap.LockBuffer(BitmapBufferAccessMode.Read);
            using var reference = buffer.CreateReference();

            _barcodeReader.TryInvert = _cameraView?.ForceInverted ?? false;

            Barcode[] barcodes = [];
            unsafe
            {
                ((Methods.IMemoryBufferByteAccess)reference).GetBuffer(out byte* dataInBytes, out _);
                var iv = new ImageView(new IntPtr(dataInBytes), bitmap.PixelWidth, bitmap.PixelHeight, Methods.ConvertImageFormats(bitmap.BitmapPixelFormat));
                barcodes = _barcodeReader.From(iv);
            }

            var imageSize = new Size(bitmap.PixelWidth, bitmap.PixelHeight);

            ArgumentNullException.ThrowIfNull(_cameraView);
            _barcodeResults.Clear();
            lock (_previewRectLock)
            {
                foreach (var barcode in barcodes)
                {
                    var barcodeResult = barcode.AsBarcodeResult(imageSize, _previewRect.Size);

                    if (_cameraView.AimMode && !barcodeResult.PreviewBoundingBox.Contains(_previewRect.Center))
                        continue;
                    if (_cameraView.ViewfinderMode && !_previewRect.Contains(barcodeResult.PreviewBoundingBox))
                        continue;

                    _barcodeResults.Add(barcodeResult);
                }
            }

            PlatformImage? image = null;
            if (_cameraView.ForceFrameCapture || (_cameraView.CaptureNextFrame && _barcodeResults.Count > 0))
            {
                using var device = CanvasDevice.GetSharedDevice();
                image = new PlatformImage(device, CanvasBitmap.CreateFromSoftwareBitmap(device, softwareBitmap));
            }

            _cameraView.DetectionFinished(_barcodeResults, image);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private void MediaPlayerElement_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(UpdatePreviewRect);
    }

    private async void MediaPlayerElement_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (!(_cameraView?.TapToFocusEnabled ?? false) || _mediaPlayerElement is null)
            return;

        var regionsOfInterestControl = _mediaCapture?.VideoDeviceController?.RegionsOfInterestControl;
        if (regionsOfInterestControl is null || !regionsOfInterestControl.AutoFocusSupported || regionsOfInterestControl.MaxRegions < 1)
            return;

        var focusControl = _mediaCapture?.VideoDeviceController?.FocusControl;
        if (!(focusControl?.Supported ?? false))
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
            Bounds = new Windows.Foundation.Rect(normalizedX - 0.05, normalizedY - 0.05, 0.1, 0.1),
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

    private async Task<MediaFrameSourceInfo?> GetCameraAsync()
    {
        var mediaFrameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();
        var preferredPanel = _cameraView?.CameraFacing == CameraFacing.Front
            ? Windows.Devices.Enumeration.Panel.Front
            : Windows.Devices.Enumeration.Panel.Back;

        return mediaFrameSourceGroups
            .SelectMany(g => g.SourceInfos)
            .Where(s => s.MediaStreamType == MediaStreamType.VideoRecord)
            .OrderByDescending(s => s.DeviceInformation?.EnclosureLocation?.Panel == preferredPanel)
            .FirstOrDefault();
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

    private async Task SetNewResolution()
    {
        var frameSource = _mediaCapture?.FrameSources[_selectedCamera?.Id];
        if (frameSource is null || !frameSource.SupportedFormats.Any())
            return;

        var format = frameSource.SupportedFormats
            .GroupBy(g => g.VideoFormat.Height)
            .OrderBy(o => Math.Abs(o.Key - Methods.TargetHeight(_cameraView?.CaptureQuality)))
            .First()
            .OrderBy(o => Math.Abs((double)o.VideoFormat.Width / (double)o.VideoFormat.Height - 16.0 / 9.0))
            .ThenByDescending(t => (double)t.FrameRate.Numerator / (double)t.FrameRate.Denominator)
            .First();

        if (format is not null)
            await frameSource.SetFormatAsync(format);
    }

    private void UpdatePreviewRect()
    {
        if (_mediaPlayerElement is null)
            return;

        lock (_previewRectLock)
        {
            var size = new Size(_mediaPlayerElement.ActualWidth, _mediaPlayerElement.ActualHeight);
            _previewRect = new Rect(Point.Zero, size);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        _mediaPlayerElement?.Tapped -= MediaPlayerElement_Tapped;
        _mediaPlayerElement?.SizeChanged -= MediaPlayerElement_SizeChanged;

        await Stop();

        _mediaCaptureLock?.Dispose();
    }
}