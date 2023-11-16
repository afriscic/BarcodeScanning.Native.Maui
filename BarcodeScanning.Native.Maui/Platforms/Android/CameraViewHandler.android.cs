using Android.Content;
using AndroidX.Camera.Core;
using AndroidX.Camera.View;
using AndroidX.Lifecycle;
using BarcodeScanning.Platforms.Android;
using Java.Util.Concurrent;

namespace BarcodeScanning;

public partial class CameraViewHandler
{
    private IExecutorService _cameraExecutor;
    private PreviewView _previewView;
    private LifecycleCameraController _cameraController;

    protected override PreviewView CreatePlatformView()
    {
        _cameraExecutor = Executors.NewSingleThreadExecutor();
        _cameraController = new LifecycleCameraController(Context)
        {
            TapToFocusEnabled = VirtualView.TapToFocusEnabled
        };
        _previewView = new PreviewView(Context)
        {
            Controller = _cameraController
        };
        return _previewView;
    }

    private void Start()
    {
        if (_cameraController is not null)
        {
            _cameraController.Unbind();

            ILifecycleOwner lifecycleOwner = null;
            if (Context is ILifecycleOwner)
                lifecycleOwner = Context as ILifecycleOwner;
            else if ((Context as ContextWrapper)?.BaseContext is ILifecycleOwner)
                lifecycleOwner = (Context as ContextWrapper)?.BaseContext as ILifecycleOwner;
            else if (Platform.CurrentActivity is ILifecycleOwner)
                lifecycleOwner = Platform.CurrentActivity as ILifecycleOwner;

            if (lifecycleOwner is null)
                return;

            UpdateResolution();
            UpdateCamera();
            UpdateAnalyzer();
            UpdateTorch();

            _cameraController.BindToLifecycle(lifecycleOwner);
        }
    }

    private void Stop()
    {
        if (_cameraController is not null)
        {
            _cameraController.EnableTorch(false);
            _cameraController.Unbind();
        }
    }

    private void HandleCameraEnabled()
    {
        //Delay to let transition animation finish
        //https://stackoverflow.com/a/67765792
        if (VirtualView is not null)
        {
            if (VirtualView.CameraEnabled)
                _ = Task.Run(async () =>
                {
                    await Task.Delay(200);
                    MainThread.BeginInvokeOnMainThread(Start);
                });
            else
                MainThread.BeginInvokeOnMainThread(Stop);
        }
    }

    //TODO Implement camera-mlkit-vision
    //https://developer.android.com/reference/androidx/camera/mlkit/vision/MlKitAnalyzer
    private void UpdateAnalyzer()
    {
        if (_cameraExecutor is not null && _cameraController is not null)
        {
            _cameraController.ClearImageAnalysisAnalyzer();
            _cameraController.SetImageAnalysisAnalyzer(_cameraExecutor, new BarcodeAnalyzer(VirtualView, _previewView));
            _cameraController.ImageAnalysisBackpressureStrategy = ImageAnalysis.StrategyKeepOnlyLatest;
        }
    }

    private void UpdateCamera()
    {
        if (_cameraController is not null)
        {
            if (VirtualView.CameraFacing == CameraFacing.Front)
                _cameraController.CameraSelector = CameraSelector.DefaultFrontCamera;
            else
                _cameraController.CameraSelector = CameraSelector.DefaultBackCamera;
        }
    }

    private void UpdateResolution()
    {
        if (_cameraController is not null)
            _cameraController.ImageAnalysisTargetSize = new CameraController.OutputSize(Platforms.Android.Methods.TargetResolution(VirtualView.CaptureQuality));
    }

    private void UpdateTorch()
    {
        if (_cameraController is not null) 
            _cameraController.EnableTorch(VirtualView.TorchOn);
    }
}
