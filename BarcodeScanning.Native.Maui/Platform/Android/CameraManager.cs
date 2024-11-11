using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Widget;
using AndroidX.Camera.Core;
using AndroidX.Camera.Core.ResolutionSelector;
using AndroidX.Camera.View;
using AndroidX.Camera.View.Transform;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Java.Util.Concurrent;
using Microsoft.Maui.Platform;

using static Android.Views.ViewGroup;

using Color = Android.Graphics.Color;
using Paint = Android.Graphics.Paint;
using Path = Android.Graphics.Path;

namespace BarcodeScanning;

internal class CameraManager : IDisposable
{
    internal BarcodeView BarcodeView { get => _barcodeView; }
    internal CameraView? CameraView { get => _cameraView; }
    internal PreviewView PreviewView { get => _previewView; }

    internal CameraState? OpenedCameraState { get; set; }

    private ICameraInfo? _currentCameraInfo;

    private readonly BarcodeAnalyzer _barcodeAnalyzer;
    private readonly BarcodeView _barcodeView;
    private readonly Context _context;
    private readonly IExecutorService _cameraExecutor;
    private readonly ImageView _imageView;
    private readonly LifecycleCameraController _cameraController;
    private readonly ILifecycleOwner _lifecycleOwner;
    private readonly PreviewView _previewView;
    private readonly RelativeLayout _relativeLayout;
    private readonly CameraStateObserver _cameraStateObserver;

    private readonly CameraView? _cameraView;

    private const int aimRadius = 25;

    internal CameraManager(CameraView cameraView, Context context)
    {
        _context = context;
        _cameraView = cameraView;

        ILifecycleOwner? owner = null;
        if (_context is ILifecycleOwner)
            owner = _context as ILifecycleOwner;
        else if ((_context as ContextWrapper)?.BaseContext is ILifecycleOwner)
            owner = (_context as ContextWrapper)?.BaseContext as ILifecycleOwner;
        else if (Platform.CurrentActivity is ILifecycleOwner)
            owner = Platform.CurrentActivity as ILifecycleOwner;
        
        var executor = Executors.NewSingleThreadExecutor();

        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(executor);
        _lifecycleOwner = owner;
        _cameraExecutor = executor;

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

        ArgumentNullException.ThrowIfNull(PreviewView.ImplementationMode.Compatible);
        ArgumentNullException.ThrowIfNull(PreviewView.ScaleType.FillCenter);
        ArgumentNullException.ThrowIfNull(Bitmap.Config.Argb8888);

        _previewView = new PreviewView(_context)
        {
            Controller = _cameraController,
            LayoutParameters = new RelativeLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
        };
        _previewView.SetBackgroundColor(_cameraView?.BackgroundColor?.ToPlatform() ?? Color.Transparent);
        _previewView.SetImplementationMode(PreviewView.ImplementationMode.Compatible);
        _previewView.SetScaleType(PreviewView.ScaleType.FillCenter);
        
        using var layoutParams = new RelativeLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
        layoutParams.AddRule(LayoutRules.CenterInParent);
        using var circleBitmap = Bitmap.CreateBitmap(2 * aimRadius, 2 * aimRadius, Bitmap.Config.Argb8888);
        using var canvas = new Canvas(circleBitmap);
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

    internal void Start()
    { 
        if (_cameraController is not null)
        {
            if (OpenedCameraState?.GetType() != CameraState.Type.Closed)
                _cameraController.Unbind();

            if (_barcodeAnalyzer is not null && _cameraExecutor is not null)
                _cameraController.SetImageAnalysisAnalyzer(_cameraExecutor, _barcodeAnalyzer);

            UpdateResolution();
            UpdateCamera();
            UpdateSymbologies();
            UpdateTorch();

            if (_lifecycleOwner is not null)
                _cameraController.BindToLifecycle(_lifecycleOwner);
        }   
    }

    internal void Stop()
    {
        _cameraController?.Unbind();
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

    internal void UpdateResolution()
    {
        if (_cameraController is not null)
        {
            using var analysisStrategy = new ResolutionStrategy(Methods.TargetResolution(_cameraView?.CaptureQuality), ResolutionStrategy.FallbackRuleClosestHigherThenLower);
            using var resolutionBuilder = new ResolutionSelector.Builder();   
            resolutionBuilder.SetAllowedResolutionMode(ResolutionSelector.PreferHigherResolutionOverCaptureRate);
            resolutionBuilder.SetResolutionStrategy(analysisStrategy);
            resolutionBuilder.SetAspectRatioStrategy(AspectRatioStrategy.Ratio169FallbackAutoStrategy);
            _cameraController.ImageAnalysisResolutionSelector = resolutionBuilder.Build();
            _cameraController.PreviewResolutionSelector = resolutionBuilder.Build();
        }
    }

    internal void UpdateSymbologies()
    {
        _barcodeAnalyzer?.UpdateSymbologies();
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

    internal CoordinateTransform? GetCoordinateTransform(IImageProxy proxy)
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

    private async void MainDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
    {
        if (_previewView is not null)
        {
            _previewView.Controller = null;
            await Task.Delay(50);
            _previewView.Controller = _cameraController;
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
            DeviceDisplay.Current.MainDisplayInfoChanged -= MainDisplayInfoChanged;

            Stop();
            
            if (_cameraStateObserver is not null)
            {
                _cameraController?.ZoomState.RemoveObserver(_cameraStateObserver);
                _currentCameraInfo?.CameraState.RemoveObserver(_cameraStateObserver);
            }

            _barcodeView?.RemoveAllViews();
            _relativeLayout?.RemoveAllViews();
            
            _barcodeView?.Dispose();
            _relativeLayout?.Dispose();
            _imageView?.Dispose();
            _previewView?.Dispose();
            _cameraController?.Dispose();
            _currentCameraInfo?.Dispose();
            _cameraStateObserver?.Dispose();
            _barcodeAnalyzer?.Dispose();
            _cameraExecutor?.Dispose();
        }
    }
}