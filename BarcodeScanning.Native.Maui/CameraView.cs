using System.Windows.Input;
using Microsoft.Maui.Graphics.Platform;

namespace BarcodeScanning;

public partial class CameraView : View
{
    public static readonly BindableProperty OnCameraPreviewReadyCommandProperty = BindableProperty.Create(nameof(OnCameraPreviewReadyCommand)
        , typeof(ICommand)
        , typeof(CameraView)
        , null
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).OnCameraPreviewReadyCommand = (ICommand)newValue);
    public ICommand OnCameraPreviewReadyCommand
    {
        get => (ICommand)GetValue(OnCameraPreviewReadyCommandProperty);
        set => SetValue(OnCameraPreviewReadyCommandProperty, value);
    }

    public static readonly BindableProperty OnDetectionFinishedCommandProperty = BindableProperty.Create(nameof(OnDetectionFinishedCommand)
        , typeof(ICommand)
        , typeof(CameraView)
        , null
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).OnDetectionFinishedCommand = (ICommand)newValue);
    public ICommand OnDetectionFinishedCommand
    {
        get => (ICommand)GetValue(OnDetectionFinishedCommandProperty);
        set => SetValue(OnDetectionFinishedCommandProperty, value);
    }

    public static readonly BindableProperty OnImageCapturedCommandProperty = BindableProperty.Create(nameof(OnImageCapturedCommand)
        , typeof(ICommand)
        , typeof(CameraView)
        , null
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).OnImageCapturedCommand = (ICommand)newValue);
    public ICommand OnImageCapturedCommand
    {
        get => (ICommand)GetValue(OnImageCapturedCommandProperty);
        set => SetValue(OnImageCapturedCommandProperty, value);
    }

    public static readonly BindableProperty VibrationOnDetectedProperty = BindableProperty.Create(nameof(VibrationOnDetected)
        , typeof(bool)
        , typeof(CameraView)
        , false
        , BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).VibrationOnDetected = (bool)newValue);
    /// <summary>
    /// Disables or enables vibration on barcode detection.
    /// </summary>
    public bool VibrationOnDetected
    {
        get => (bool)GetValue(VibrationOnDetectedProperty);
        set => SetValue(VibrationOnDetectedProperty, value);
    }

    public static readonly BindableProperty CameraEnabledProperty = BindableProperty.Create(nameof(CameraEnabled)
        , typeof(bool)
        , typeof(CameraView)
        , false
        , BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).CameraEnabled = (bool)newValue);
    /// <summary>
    /// Disables or enables camera.
    /// </summary>
    public bool CameraEnabled
    {
        get => (bool)GetValue(CameraEnabledProperty);
        set => SetValue(CameraEnabledProperty, value);
    }

    public static readonly BindableProperty PauseScanningProperty = BindableProperty.Create(nameof(PauseScanning)
        , typeof(bool)
        , typeof(CameraView)
        , false
        , BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).PauseScanning = (bool)newValue);
    /// <summary>
    /// Pauses barcode scanning.
    /// </summary>
    public bool PauseScanning
    {
        get => (bool)GetValue(PauseScanningProperty);
        set => SetValue(PauseScanningProperty, value);
    }

    public static readonly BindableProperty ForceInvertedProperty = BindableProperty.Create(nameof(ForceInverted)
        , typeof(bool)
        , typeof(CameraView)
        , false
        , BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).ForceInverted = (bool)newValue);
    /// <summary>
    /// Forces scanning of inverted barcodes. Reduces performance significantly. Android only.
    /// </summary>
    public bool ForceInverted
    {
        get => (bool)GetValue(ForceInvertedProperty);
        set => SetValue(ForceInvertedProperty, value);
    }

    public static readonly BindableProperty PoolingIntervalProperty = BindableProperty.Create(nameof(PoolingInterval)
        , typeof(int)
        , typeof(CameraView)
        , 0
        , BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).PoolingInterval = (int)newValue);
    /// <summary>
    /// Enables pooling of detections for better detection of multiple barcodes at once. 
    /// Value in milliseconds. Default 0 (disabled).
    /// </summary>
    public int PoolingInterval
    {
        get => (int)GetValue(PoolingIntervalProperty);
        set => SetValue(PoolingIntervalProperty, value);
    }

    public static readonly BindableProperty TorchOnProperty = BindableProperty.Create(nameof(TorchOn)
        , typeof(bool)
        , typeof(CameraView)
        , false
        , BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).TorchOn = (bool)newValue);
    /// <summary>
    /// Disables or enables torch.
    /// </summary>
    public bool TorchOn
    {
        get => (bool)GetValue(TorchOnProperty);
        set => SetValue(TorchOnProperty, value);
    }

    public static readonly BindableProperty TapToFocusEnabledProperty = BindableProperty.Create(nameof(TapToFocusEnabled)
        , typeof(bool)
        , typeof(CameraView)
        , false
        , BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).TapToFocusEnabled = (bool)newValue);
    /// <summary>
    /// Disables or enables tap-to-focus.
    /// </summary>
    public bool TapToFocusEnabled
    {
        get => (bool)GetValue(TapToFocusEnabledProperty);
        set => SetValue(TapToFocusEnabledProperty, value);
    }

    public static readonly BindableProperty CameraFacingProperty = BindableProperty.Create(nameof(CameraFacing)
        , typeof(CameraFacing)
        , typeof(CameraView)
        , CameraFacing.Back
        , BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).CameraFacing = (CameraFacing)newValue);
    /// <summary>
    /// Select Back or Front camera.
    /// Default value is Back Camera.
    /// </summary>
    public CameraFacing CameraFacing
    {
        get => (CameraFacing)GetValue(CameraFacingProperty);
        set => SetValue(CameraFacingProperty, value);
    }

    public static readonly BindableProperty CaptureQualityProperty = BindableProperty.Create(nameof(CaptureQuality)
        , typeof(CaptureQuality)
        , typeof(CameraView)
        , CaptureQuality.Medium
        , BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).CaptureQuality = (CaptureQuality)newValue);
    /// <summary>
    /// Set the capture quality for the image analysis.
    /// Recommended and default value is Medium.
    /// Use highest values for more precision or lower for fast scanning.
    /// </summary>
    public CaptureQuality CaptureQuality
    {
        get => (CaptureQuality)GetValue(CaptureQualityProperty);
        set => SetValue(CaptureQualityProperty, value);
    }

    public static readonly BindableProperty BarcodeSymbologiesProperty = BindableProperty.Create(nameof(BarcodeSymbologies)
        , typeof(BarcodeFormats)
        , typeof(CameraView)
        , BarcodeFormats.All
        , BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).BarcodeSymbologies = (BarcodeFormats)newValue);
    /// <summary>
    /// Set the enabled symbologies.
    /// Default value All.
    /// </summary>
    public BarcodeFormats BarcodeSymbologies
    {
        get => (BarcodeFormats)GetValue(BarcodeSymbologiesProperty);
        set => SetValue(BarcodeSymbologiesProperty, value);
    }

    public static readonly BindableProperty AimModeProperty = BindableProperty.Create(nameof(AimMode)
        , typeof(bool)
        , typeof(CameraView)
        , false
        , BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).AimMode = (bool)newValue);
    /// <summary>
    /// Disables or enables aim mode. When enabled only barcode that is in the center of the preview will be detected.
    /// </summary>
    public bool AimMode
    {
        get => (bool)GetValue(AimModeProperty);
        set => SetValue(AimModeProperty, value);
    }

    public static readonly BindableProperty ViewfinderModeProperty = BindableProperty.Create(nameof(ViewfinderMode)
        , typeof(bool)
        , typeof(CameraView)
        , false
        , BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).ViewfinderMode = (bool)newValue);
    /// <summary>
    /// Disables or enables viewfinder mode. When enabled only barcode that is visible in the preview will be detected.
    /// </summary>
    public bool ViewfinderMode
    {
        get => (bool)GetValue(ViewfinderModeProperty);
        set => SetValue(ViewfinderModeProperty, value);
    }

    public static readonly BindableProperty CaptureNextFrameProperty = BindableProperty.Create(nameof(CaptureNextFrame)
        , typeof(bool)
        , typeof(CameraView)
        , false
        , BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).CaptureNextFrame = (bool)newValue);
    /// <summary>
    /// Captures the next frame from the camera feed.
    /// </summary>
    public bool CaptureNextFrame
    {
        get => (bool)GetValue(CaptureNextFrameProperty);
        set => SetValue(CaptureNextFrameProperty, value);
    }

    public static readonly BindableProperty ForceFrameCaptureProperty = BindableProperty.Create(nameof(ForceFrameCapture)
        , typeof(bool)
        , typeof(CameraView)
        , false
        , BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).ForceFrameCapture = (bool)newValue);
    /// <summary>
    /// Forces the capture of camera frames regardless of whether a barcode is detected or not.
    /// Is not automatically disabled after the frame is captured.
    /// </summary>
    public bool ForceFrameCapture
    {
        get => (bool)GetValue(ForceFrameCaptureProperty);
        set => SetValue(ForceFrameCaptureProperty, value);
    }

    public static readonly BindableProperty RequestZoomFactorProperty = BindableProperty.Create(nameof(RequestZoomFactor)
        , typeof(float)
        , typeof(CameraView)
        , -1f
        , BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).RequestZoomFactor = (float)newValue);
    /// <summary>
    /// Setting this value changes the zoom factor of the camera. Value has to be between MinZoomFactor and MaxZoomFactor.
    /// More info:
    /// iOS/macOS - https://developer.apple.com/documentation/avfoundation/avcapturedevice/zoom
    /// Android - https://developer.android.com/reference/kotlin/androidx/camera/view/CameraController#setZoomRatio(float)
    /// Only logical multi-camera is supported - https://developer.android.com/media/camera/camera2/multi-camera
    /// </summary>
    public float RequestZoomFactor
    {
        get => (float)GetValue(RequestZoomFactorProperty);
        set => SetValue(RequestZoomFactorProperty, value);
    }

    public static readonly BindableProperty AimIndicatorColorProperty = BindableProperty.Create(nameof(AimIndicatorColor)
        , typeof(Color)
        , typeof(CameraView)
        , new Color(255, 0, 0, 150)
        , BindingMode.OneTime);
    /// <summary>
    /// Gets or sets the color of the aim indicator in the camera view.
    /// Default value is <c>Color(255, 0, 0, 150)</c> (semi-transparent red).
    /// The property uses <see cref="BindingMode.OneTime"/> by default.
    /// </summary>
    public Color AimIndicatorColor
    {
        get => (Color)GetValue(AimIndicatorColorProperty);
        private set => SetValue(AimIndicatorColorProperty, value);
    }

    public static readonly BindableProperty CurrentZoomFactorProperty = BindableProperty.Create(nameof(CurrentZoomFactor)
        , typeof(float)
        , typeof(CameraView)
        , -1f
        , BindingMode.OneWayToSource);
    /// <summary>
    /// Returns current zoom factor value.
    /// </summary>
    public float CurrentZoomFactor
    {
        get => (float)GetValue(CurrentZoomFactorProperty);
        set => SetValue(CurrentZoomFactorProperty, value);
    }

    public static readonly BindableProperty MinZoomFactorProperty = BindableProperty.Create(nameof(MinZoomFactor)
        , typeof(float)
        , typeof(CameraView)
        , -1f
        , BindingMode.OneWayToSource);
    /// <summary>
    /// Returns minimum zoom factor for current camera.
    /// </summary>
    public float MinZoomFactor
    {
        get => (float)GetValue(MinZoomFactorProperty);
        set => SetValue(MinZoomFactorProperty, value);
    }

    public static readonly BindableProperty MaxZoomFactorProperty = BindableProperty.Create(nameof(MaxZoomFactor)
        , typeof(float)
        , typeof(CameraView)
        , -1f
        , BindingMode.OneWayToSource);
    /// <summary>
    /// Returns maximum zoom factor for current camera.
    /// </summary>
    public float MaxZoomFactor
    {
        get => (float)GetValue(MaxZoomFactorProperty);
        set => SetValue(MaxZoomFactorProperty, value);
    }

    public static readonly BindableProperty DeviceSwitchZoomFactorProperty = BindableProperty.Create(nameof(DeviceSwitchZoomFactor)
        , typeof(float[])
        , typeof(CameraView)
        , Array.Empty<float>()
        , BindingMode.OneWayToSource);
    /// <summary>
    /// Returns zoom factors that switch between camera lenses (iOS only).
    /// </summary>
    public float[] DeviceSwitchZoomFactor
    {
        get => (float[])GetValue(DeviceSwitchZoomFactorProperty);
        set => SetValue(DeviceSwitchZoomFactorProperty, value);
    }

    public event EventHandler<OnCameraPreviewReadyEventArg>? OnCameraPreviewReady;
    public event EventHandler<OnDetectionFinishedEventArg>? OnDetectionFinished;
    public event EventHandler<OnImageCapturedEventArg>? OnImageCaptured;

    internal bool ProcessingDetected { get; private set; }
    internal IDispatcherTimer? PoolingTimer { get; private set; }

    private readonly HashSet<BarcodeResult> _pooledResults;
    private readonly Lock _poolingLock;

    private PlatformImage? lastImage;
    private bool _cameraPreviewReadyFired;

    public CameraView()
    {
        _pooledResults = [];
        _poolingLock = new();
    }

    internal void ResetCameraPreviewReady()
    {
        _cameraPreviewReadyFired = false;
    }

    internal void TriggerCameraPreviewReady()
    {
        if (_cameraPreviewReadyFired)
            return;

        _cameraPreviewReadyFired = true;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var args = new OnCameraPreviewReadyEventArg();
                OnCameraPreviewReady?.Invoke(this, args);
                if (OnCameraPreviewReadyCommand?.CanExecute(args) == true)
                    OnCameraPreviewReadyCommand?.Execute(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });
    }

    internal void DetectionFinished(HashSet<BarcodeResult> barCodeResults, PlatformImage? image = null)
    {
        if (PoolingInterval > 0)
        {
            lock (_poolingLock)
            {
                if (PoolingTimer is null)
                {
                    PoolingTimer = Dispatcher.CreateTimer();
                    PoolingTimer.IsRepeating = false;
                    PoolingTimer.Tick += PoolingTimer_Elapsed;
                }

                if (!PoolingTimer.IsRunning)
                {
                    _pooledResults.Clear();
                    PoolingTimer.Interval = TimeSpan.FromMilliseconds(PoolingInterval);
                    PoolingTimer.Start();
                }

                foreach (var result in barCodeResults)
                {
                    _pooledResults.Remove(result);
                    _pooledResults.Add(result);
                }

                if (image is not null)
                {
                    lastImage?.Dispose();
                    lastImage = image;
                }
            }
        }
        else
        {
            if (PoolingTimer?.IsRunning == true)
                PoolingTimer.Stop();

            ProcessingDetected = true;
            TriggerOnDetectionFinished(barCodeResults, image);
        }
    }

    private void PoolingTimer_Elapsed(object? sender, EventArgs e)
    {
        try
        {
            lock (_poolingLock)
            {
                if (PoolingInterval > 0)
                    TriggerOnDetectionFinished(_pooledResults, lastImage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            StopTimer();
        }
    }

    private void TriggerOnDetectionFinished(HashSet<BarcodeResult> barcodeResults, PlatformImage? image = null)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (PauseScanning)
                    return;
                
                if (VibrationOnDetected && barcodeResults.Count > 0 && Vibration.Default.IsSupported)
                    Vibration.Default.Vibrate();

                OnDetectionFinished?.Invoke(this, new OnDetectionFinishedEventArg { BarcodeResults = barcodeResults });
                if (OnDetectionFinishedCommand?.CanExecute(barcodeResults) == true)
                    OnDetectionFinishedCommand?.Execute(barcodeResults);

                if (image is not null)
                {
                    CaptureNextFrame = false;

                    OnImageCaptured?.Invoke(this, new OnImageCapturedEventArg { Image = image });
                    if (OnImageCapturedCommand?.CanExecute(image) == true)
                        OnImageCapturedCommand?.Execute(image);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                ProcessingDetected = false;
            }
        });
    }

    internal void StopTimer()
    {
        try
        {
            PoolingTimer?.Stop();
            PoolingTimer?.Tick -= PoolingTimer_Elapsed;
            PoolingTimer = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}