using Android.Content;
using Android.Gms.Extensions;
using Android.Graphics;
using Android.Widget;
using AndroidX.Camera.Core;
using AndroidX.Camera.View;
using AndroidX.Camera.View.Transform;
using AndroidX.Lifecycle;
using Java.Util.Concurrent;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Platform;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;

using static Android.Views.ViewGroup;

using Color = Android.Graphics.Color;
using MLKitBarcodeScanning = Xamarin.Google.MLKit.Vision.BarCode.BarcodeScanning;
using Paint = Android.Graphics.Paint;
using Point = Microsoft.Maui.Graphics.Point;
using RectF = Microsoft.Maui.Graphics.RectF;

namespace BarcodeScanning;

internal class CameraManager : IDisposable
{
    internal BarcodeView BarcodeView { get => _barcodeView; }

    private BarcodeAnalyzer _barcodeAnalyzer;
    private IBarcodeScanner _barcodeScanner;

    private readonly BarcodeView _barcodeView;
    private readonly CameraView _cameraView;
    private readonly Context _context;
    private readonly IExecutorService _cameraExecutor;
    private readonly ImageView _imageView;
    private readonly LifecycleCameraController _cameraController;
    private readonly PreviewView _previewView;
    private readonly RelativeLayout _relativeLayout;
    private readonly ZoomStateObserver _zoomStateObserver;

    private readonly HashSet<BarcodeResult> _barcodeResults = [];
    private const int aimRadius = 25;
    private bool _cameraRunning = false;

    internal CameraManager(CameraView cameraView, Context context)
    {
        _context = context;
        _cameraView = cameraView;

        _cameraExecutor = Executors.NewSingleThreadExecutor();
        _zoomStateObserver = new ZoomStateObserver(this, _cameraView);
        _cameraController = new LifecycleCameraController(_context)
        {
            TapToFocusEnabled = _cameraView?.TapToFocusEnabled ?? false,
            ImageAnalysisBackpressureStrategy = ImageAnalysis.StrategyKeepOnlyLatest
        };
        _cameraController.SetEnabledUseCases(CameraController.ImageAnalysis);
        _cameraController.ZoomState.ObserveForever(_zoomStateObserver);
        
        _previewView = new PreviewView(_context)
        {
            Controller = _cameraController,
            LayoutParameters = new RelativeLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
        };
        _previewView.SetBackgroundColor(_cameraView?.BackgroundColor?.ToPlatform() ?? Color.Transparent);
        _previewView.SetImplementationMode(PreviewView.ImplementationMode.Compatible);
        _previewView.SetScaleType(PreviewView.ScaleType.FillCenter);

        var layoutParams = new RelativeLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
        layoutParams.AddRule(LayoutRules.CenterInParent);
        var circleBitmap = Bitmap.CreateBitmap(2 * aimRadius, 2 * aimRadius, Bitmap.Config.Argb8888);
        var canvas = new Canvas(circleBitmap);
        canvas.DrawCircle(aimRadius, aimRadius, aimRadius, new Paint
        {
            AntiAlias = true,
            Color = Color.Red,
            Alpha = 150
        }); 
        _imageView = new ImageView(_context)
        {
            LayoutParameters = layoutParams
        };
        _imageView.SetImageBitmap(circleBitmap);

        _relativeLayout = new RelativeLayout(_context)
        {
            LayoutParameters = new RelativeLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
        };
        _relativeLayout.AddView(_previewView);

        _barcodeView = new BarcodeView(_context);
        _barcodeView.AddView(_relativeLayout);

        DeviceDisplay.Current.MainDisplayInfoChanged += MainDisplayInfoChanged;
    }

    //TODO Implement camera-mlkit-vision
    //https://developer.android.com/reference/androidx/camera/mlkit/vision/MlKitAnalyzer
    internal void Start()
    { 
        if (_cameraController is not null)
        {
            if (_cameraRunning)
            {
                _cameraController.Unbind();
                _cameraRunning = false;
            }

            ILifecycleOwner lifecycleOwner = null;
            if (_context is ILifecycleOwner)
                lifecycleOwner = _context as ILifecycleOwner;
            else if ((_context as ContextWrapper)?.BaseContext is ILifecycleOwner)
                lifecycleOwner = (_context as ContextWrapper)?.BaseContext as ILifecycleOwner;
            else if (Platform.CurrentActivity is ILifecycleOwner)
                lifecycleOwner = Platform.CurrentActivity as ILifecycleOwner;

            if (lifecycleOwner is null)
                return;

            if (_cameraController.CameraSelector is null)
                UpdateCamera();
            if (_cameraController.ImageAnalysisTargetSize is null)
                UpdateResolution();

            if (_cameraController is not null && _cameraExecutor is not null)
            {
                _cameraController.ClearImageAnalysisAnalyzer();
                _barcodeAnalyzer?.Dispose();
                _barcodeAnalyzer = new BarcodeAnalyzer(this);
                _cameraController.SetImageAnalysisAnalyzer(_cameraExecutor, _barcodeAnalyzer);
            }

            UpdateSymbologies();
            UpdateTorch();

            _cameraController.BindToLifecycle(lifecycleOwner);
            _cameraRunning = true;
        }
    }

    internal void Stop()
    {
        if (_cameraController is not null)
        {
            if ((int)_cameraController.TorchState.Value == TorchState.On)
            {
                _cameraController.EnableTorch(false);

                if (_cameraView is not null)
                    _cameraView.TorchOn = false;
            }
            
            if (_cameraRunning)
                _cameraController.Unbind();
            
            _cameraRunning = false;
        }
    }

    internal void UpdateAimMode()
    {
        if (_cameraView?.AimMode ?? false)
            _relativeLayout?.AddView(_imageView);
        else
            _relativeLayout?.RemoveView(_imageView);
    }

    internal void UpdateBackgroundColor()
    {
        _previewView?.SetBackgroundColor(_cameraView?.BackgroundColor?.ToPlatform() ?? Color.Transparent);
    }

    internal void UpdateCamera()
    {
        if (_cameraController is not null)
        {
            if (_cameraView?.CameraFacing == CameraFacing.Front)
                _cameraController.CameraSelector = CameraSelector.DefaultFrontCamera;
            else
                _cameraController.CameraSelector = CameraSelector.DefaultBackCamera;
        }
    }

    internal void UpdateCameraEnabled()
    {
        if (_cameraView?.CameraEnabled ?? false)
            Start();
        else
            Stop();
    }

    //TODO Implement setImageAnalysisResolutionSelector
    //https://developer.android.com/reference/androidx/camera/view/CameraController#setImageAnalysisResolutionSelector(androidx.camera.core.resolutionselector.ResolutionSelector)
    internal void UpdateResolution()
    {
        if (_cameraController is not null)
            _cameraController.ImageAnalysisTargetSize = new CameraController.OutputSize(Methods.TargetResolution(_cameraView?.CaptureQuality));

        if (_cameraRunning)
            Start();
    }

    internal void UpdateSymbologies()
    {
        if (_cameraView is not null)
        {
            _barcodeScanner?.Dispose();
            _barcodeScanner = MLKitBarcodeScanning.GetClient(new BarcodeScannerOptions.Builder()
                .SetBarcodeFormats(Methods.ConvertBarcodeFormats(_cameraView.BarcodeSymbologies))
                .Build());
        }
    }

    internal void UpdateTapToFocus() 
    {
        if (_cameraController is not null)
            _cameraController.TapToFocusEnabled = _cameraView?.TapToFocusEnabled ?? false;
    }

    internal void UpdateTorch()
    {
        _cameraController?.EnableTorch(_cameraView?.TorchOn ?? false);
    }

    internal void UpdateZoomFactor()
    {
        if (_cameraView is not null && (_cameraController?.ZoomState?.IsInitialized ?? false))
        {
            var factor = _cameraView.RequestZoomFactor;
            if (factor > 0)
            {
                factor = Math.Max(factor, _cameraView.MinZoomFactor);
                factor = Math.Min(factor, _cameraView.MaxZoomFactor);

                if (factor != _cameraView.CurrentZoomFactor)
                    _cameraController.SetZoomRatio(factor);
            }
        }            
    }

    internal void AnalyzeFrame(IImageProxy proxy)
    {
        if (proxy is not null && _cameraView is not null && _previewView is not null && _barcodeResults is not null && _barcodeScanner is not null && !_cameraView.PauseScanning)
        {
            DetectBarcode(proxy).Wait(2000);

            if (_cameraView.CaptureNextFrame)
                CaptureImage(proxy);
        }
    }

    private void CaptureImage(IImageProxy proxy)
    {
        _cameraView.CaptureNextFrame = false;
        var image = new PlatformImage(proxy.ToBitmap());
        _cameraView.TriggerOnImageCaptured(image);
    }

    private async Task DetectBarcode(IImageProxy proxy)
    {
        _barcodeResults.Clear();
        using var target = await MainThread.InvokeOnMainThreadAsync(() => _previewView.OutputTransform).ConfigureAwait(false);
        using var source = new ImageProxyTransformFactory
        {
            UsingRotationDegrees = true
        }
        .GetOutputTransform(proxy);
        using var coordinateTransform = new CoordinateTransform(source, target);

        using var image = InputImage.FromMediaImage(proxy.Image, proxy.ImageInfo.RotationDegrees);
        using var results = await _barcodeScanner.Process(image).AsAsync<Java.Lang.Object>().ConfigureAwait(false);
        
        Methods.ProcessBarcodeResult(results, _barcodeResults, coordinateTransform);

        if (_cameraView.ForceInverted)
        {
            Methods.InvertLuminance(proxy.Image);
            using var invertedimage = InputImage.FromMediaImage(proxy.Image, proxy.ImageInfo.RotationDegrees);
            using var invertedresults = await _barcodeScanner.Process(invertedimage).AsAsync<Java.Lang.Object>().ConfigureAwait(false);

            Methods.ProcessBarcodeResult(invertedresults, _barcodeResults, coordinateTransform);
        }

        if (_cameraView.AimMode)
        {
            var previewCenter = new Point(_previewView.Width / 2, _previewView.Height / 2);

            foreach (var barcode in _barcodeResults)
            {
                if (!barcode.PreviewBoundingBox.Contains(previewCenter))
                    _barcodeResults.Remove(barcode);
            }
        }

        if (_cameraView.ViewfinderMode)
        {
            var previewRect = new RectF(0, 0, _previewView.Width, _previewView.Height);

            foreach (var barcode in _barcodeResults)
            {
                if (!previewRect.Contains(barcode.PreviewBoundingBox))
                    _barcodeResults.Remove(barcode);
            }
        }

        _cameraView.DetectionFinished(_barcodeResults);
    }

    private void MainDisplayInfoChanged(object sender, DisplayInfoChangedEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(200);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (_cameraRunning && _cameraView.CameraEnabled)
                        UpdateResolution();
                }
                catch (Exception)
                {
                    DeviceDisplay.Current.MainDisplayInfoChanged -= MainDisplayInfoChanged;
                }
            });
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
            try
            {
                DeviceDisplay.Current.MainDisplayInfoChanged -= MainDisplayInfoChanged;
            }
            catch (Exception)
            {
            }

            Stop();

            _cameraController?.ZoomState.RemoveObserver(_zoomStateObserver);
            _barcodeView?.RemoveAllViews();
            _relativeLayout?.RemoveAllViews();
            
            _barcodeView?.Dispose();
            _relativeLayout?.Dispose();
            _imageView?.Dispose();
            _previewView?.Dispose();
            _cameraController?.Dispose();
            _zoomStateObserver?.Dispose();
            _barcodeAnalyzer?.Dispose();
            _barcodeScanner?.Dispose();
            _cameraExecutor?.Dispose();
        }
    }
}