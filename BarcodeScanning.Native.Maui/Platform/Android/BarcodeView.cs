using Android.Content;
using Android.Graphics;
using Android.Widget;
using AndroidX.Camera.Core;
using AndroidX.Camera.View;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;

using Color = Android.Graphics.Color;
using Paint = Android.Graphics.Paint;

namespace BarcodeScanning;

public class BarcodeView : CoordinatorLayout
{
    internal PreviewView PreviewView { get => _previewView; }

    private BarcodeAnalyzer _barcodeAnalyzer;

    private readonly CameraView _cameraView;
    private readonly Context _context;
    private readonly ImageView _imageView;
    private readonly LifecycleCameraController _cameraController;
    private readonly PreviewView _previewView;
    private readonly RelativeLayout _relativeLayout;
    private readonly ZoomStateObserver _zoomStateObserver;

    private bool _cameraRunning = false;

    internal BarcodeView(Context context, CameraView cameraView) : base(context)
    {
        _context = context;
        _cameraView = cameraView;
        _zoomStateObserver = new ZoomStateObserver(_cameraView);
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
        _previewView.SetScaleType(PreviewView.ScaleType.FillCenter);

        var layoutParams = new RelativeLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
        layoutParams.AddRule(LayoutRules.CenterInParent);
        _imageView = new ImageView(_context)
        {
            LayoutParameters = layoutParams
        };

        _relativeLayout = new RelativeLayout(_context)
        {
            LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
        };
        _relativeLayout.AddView(_previewView);

        this.AddView(_relativeLayout);

        DeviceDisplay.Current.MainDisplayInfoChanged += Current_MainDisplayInfoChanged;
    }

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

            UpdateAnalyzer();
            UpdateTorch();
            
            _cameraController.BindToLifecycle(lifecycleOwner);
            _cameraRunning = true;

            UpdateZoomFactor();
        }
    }

    internal void Stop()
    {
        if (_cameraController is not null)
        {
            if ((int)_cameraController.TorchState.Value == TorchState.On)
                _cameraController.EnableTorch(false);
            
            if (_cameraRunning)
                _cameraController.Unbind();
            
            _cameraRunning = false;
        }
    }

    //TODO Implement camera-mlkit-vision
    //https://developer.android.com/reference/androidx/camera/mlkit/vision/MlKitAnalyzer
    internal void UpdateAnalyzer()
    {
        if (_cameraController is not null)
        {
            _cameraController.ClearImageAnalysisAnalyzer();
            _barcodeAnalyzer?.Dispose();
            _barcodeAnalyzer = new BarcodeAnalyzer(_cameraView, this);
            _cameraController.SetImageAnalysisAnalyzer(ContextCompat.GetMainExecutor(_context), _barcodeAnalyzer);
        }
    }

    internal void UpdateCamera()
    {
        if (_cameraController is not null)
        {
            if (_cameraView?.CameraFacing == CameraFacing.Front)
                _cameraController.CameraSelector = CameraSelector.DefaultFrontCamera;
            else
                _cameraController.CameraSelector = CameraSelector.DefaultBackCamera;

            _cameraView?.ResetRequestZoomFactor();
        }
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

    internal void UpdateTorch()
    {
        if (_cameraController is not null)
            _cameraController.EnableTorch(_cameraView?.TorchOn ?? false);
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
        
        if (factor > 0 && _cameraController is not null)
            _cameraController.SetZoomRatio(factor);
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
        {
            var radius = 25;
            var circleBitmap = Bitmap.CreateBitmap(2 * radius, 2 * radius, Bitmap.Config.Argb8888);
            var canvas = new Canvas(circleBitmap);
            var paint = new Paint
            {
                AntiAlias = true,
                Color = Color.Red,
                Alpha = 150
            };
            canvas.DrawCircle(radius, radius, radius, paint);

            _imageView?.SetImageBitmap(circleBitmap);
            _relativeLayout?.AddView(_imageView);
        }
        else
        {
            try
            {
                _relativeLayout?.RemoveView(_imageView);
            }
            catch (Exception)
            {
            }
        }
    }

    internal void HandleTapToFocus() 
    {
        if (_cameraController is not null)
            _cameraController.TapToFocusEnabled = _cameraView?.TapToFocusEnabled ?? false;
    }
    
    private void Current_MainDisplayInfoChanged(object sender, DisplayInfoChangedEventArgs e)
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
                    DeviceDisplay.Current.MainDisplayInfoChanged -= Current_MainDisplayInfoChanged;
                }
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                DeviceDisplay.Current.MainDisplayInfoChanged -= Current_MainDisplayInfoChanged;

                Stop();

                this?.RemoveAllViews();
                _relativeLayout?.RemoveAllViews();
                _cameraController?.ZoomState.RemoveObserver(_zoomStateObserver);

                _relativeLayout?.Dispose();
                _imageView?.Dispose();
                _previewView?.Dispose();
                _cameraController?.Dispose();
                _zoomStateObserver.Dispose();
                _barcodeAnalyzer?.Dispose();
            }
            catch (Exception)
            {
            }
        }

        base.Dispose(disposing);
    }
}
