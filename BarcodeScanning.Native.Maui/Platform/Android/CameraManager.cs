using Android.Content;
using Android.Graphics;
using Android.Widget;
using AndroidX.Camera.Core;
using AndroidX.Camera.View;
using AndroidX.Camera.View.Transform;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Java.Util.Concurrent;
using Microsoft.Maui.Platform;
using Xamarin.Google.MLKit.Vision.BarCode;

using static Android.Views.ViewGroup;

using Color = Android.Graphics.Color;
using MLKitBarcodeScanning = Xamarin.Google.MLKit.Vision.BarCode.BarcodeScanning;
using Paint = Android.Graphics.Paint;

namespace BarcodeScanning;

internal class CameraManager : IDisposable
{
    internal BarcodeView BarcodeView { get => _barcodeView; }
    internal IBarcodeScanner BarcodeScanner { get => _barcodeScanner; }
    internal CameraView CameraView { get => _cameraView; }
    internal PreviewView PreviewView { get => _previewView; }

    internal CameraState OpenedCameraState { get; set; }
    internal bool RecalculateCoordinateTransform { get; set; }

    private IBarcodeScanner _barcodeScanner;
    private ICameraInfo _currentCameraInfo;

    private readonly BarcodeAnalyzer _barcodeAnalyzer;
    private readonly BarcodeView _barcodeView;
    private readonly CameraView _cameraView;
    private readonly Context _context;
    private readonly IExecutorService _cameraExecutor;
    private readonly ImageView _imageView;
    private readonly LifecycleCameraController _cameraController;
    private readonly ILifecycleOwner _lifecycleOwner;
    private readonly PreviewView _previewView;
    private readonly PreviewViewOnLayoutChangeListener _previewViewOnLayoutChangeListener;
    private readonly RelativeLayout _relativeLayout;
    private readonly CameraStateObserver _cameraStateObserver;

    private const int aimRadius = 25;

    internal CameraManager(CameraView cameraView, Context context)
    {
        _context = context;
        _cameraView = cameraView;

        if (_context is ILifecycleOwner)
            _lifecycleOwner = _context as ILifecycleOwner;
        else if ((_context as ContextWrapper)?.BaseContext is ILifecycleOwner)
            _lifecycleOwner = (_context as ContextWrapper)?.BaseContext as ILifecycleOwner;
        else if (Platform.CurrentActivity is ILifecycleOwner)
            _lifecycleOwner = Platform.CurrentActivity as ILifecycleOwner;
        else
            _lifecycleOwner = null;

        _cameraExecutor = Executors.NewSingleThreadExecutor();
        _barcodeAnalyzer = new BarcodeAnalyzer(this);

        _cameraStateObserver = new CameraStateObserver(this, _cameraView);
        _cameraController = new LifecycleCameraController(_context)
        {
            TapToFocusEnabled = _cameraView?.TapToFocusEnabled ?? false,
            ImageAnalysisBackpressureStrategy = ImageAnalysis.StrategyKeepOnlyLatest
        };
        _cameraController.SetEnabledUseCases(CameraController.ImageAnalysis);
        _cameraController.ZoomState.ObserveForever(_cameraStateObserver);
        _cameraController.InitializationFuture.AddListener(new Java.Lang.Runnable(() => 
        {
            _currentCameraInfo?.CameraState.RemoveObserver(_cameraStateObserver);
            _currentCameraInfo = _cameraController.CameraInfo;
            _currentCameraInfo?.CameraState.ObserveForever(_cameraStateObserver);
        }), ContextCompat.GetMainExecutor(_context));

        _previewViewOnLayoutChangeListener = new PreviewViewOnLayoutChangeListener(this);
        _previewView = new PreviewView(_context)
        {
            Controller = _cameraController,
            LayoutParameters = new RelativeLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
        };
        _previewView.SetBackgroundColor(_cameraView?.BackgroundColor?.ToPlatform() ?? Color.Transparent);
        _previewView.SetImplementationMode(PreviewView.ImplementationMode.Compatible);
        _previewView.SetScaleType(PreviewView.ScaleType.FillCenter);
        _previewView.AddOnLayoutChangeListener(_previewViewOnLayoutChangeListener);

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
    internal void Start(bool skipResolution = false)
    { 
        if (_cameraController is not null)
        {
            if (OpenedCameraState?.GetType() != CameraState.Type.Closed)
                _cameraController.Unbind();

            if (_cameraController.CameraSelector is null)
                UpdateCamera();
            if (_cameraController.ImageAnalysisTargetSize is null && !skipResolution)
                UpdateResolution();
            if (_barcodeAnalyzer is not null && _cameraExecutor is not null)
                _cameraController.SetImageAnalysisAnalyzer(_cameraExecutor, _barcodeAnalyzer);

            UpdateSymbologies();
            UpdateTorch();

            if (_lifecycleOwner is not null)
                _cameraController.BindToLifecycle(_lifecycleOwner);
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
            
            _cameraController.Unbind();
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

        if (OpenedCameraState?.GetType() == CameraState.Type.Open || OpenedCameraState?.GetType() == CameraState.Type.Opening || OpenedCameraState?.GetType() == CameraState.Type.PendingOpen)
            Start(true);
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

    internal CoordinateTransform GetCoordinateTransform(IImageProxy proxy)
    {
        var imageOutputTransform = new ImageProxyTransformFactory
        {
            UsingRotationDegrees = true
        }
        .GetOutputTransform(proxy);
        var previewOutputTransform = MainThread.InvokeOnMainThreadAsync(() => _previewView?.OutputTransform).Result;

        if (imageOutputTransform is not null && previewOutputTransform is not null)
            return new CoordinateTransform(imageOutputTransform, previewOutputTransform);
        else
            return null;
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
            DeviceDisplay.Current.MainDisplayInfoChanged -= MainDisplayInfoChanged;

            Stop();

            _cameraController?.ZoomState.RemoveObserver(_cameraStateObserver);
            _currentCameraInfo?.CameraState.RemoveObserver(_cameraStateObserver);
            _previewView?.RemoveOnLayoutChangeListener(_previewViewOnLayoutChangeListener);
            _barcodeView?.RemoveAllViews();
            _relativeLayout?.RemoveAllViews();
            
            _barcodeView?.Dispose();
            _relativeLayout?.Dispose();
            _imageView?.Dispose();
            _previewView?.Dispose();
            _previewViewOnLayoutChangeListener?.Dispose();
            _cameraController?.Dispose();
            _currentCameraInfo?.Dispose();
            _cameraStateObserver?.Dispose();
            _barcodeAnalyzer?.Dispose();
            _barcodeScanner?.Dispose();
            _cameraExecutor?.Dispose();
        }
    }
}