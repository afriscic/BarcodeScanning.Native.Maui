﻿using System.Windows.Input;
using Microsoft.Maui.Graphics.Platform;

using Timer = System.Timers.Timer;

namespace BarcodeScanning;

public partial class CameraView : View
{
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
        , true
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
    /// Set the capture quality for the image analysys.
    /// Reccomended and default value is Medium.
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

    public static readonly BindableProperty RequestZoomFactorProperty = BindableProperty.Create(nameof(RequestZoomFactor)
        , typeof(float)
        , typeof(CameraView)
        , -1f
        , BindingMode.TwoWay
        , propertyChanged: (bindable, value, newValue) => ((CameraView)bindable).RequestZoomFactor = (float)newValue);
    /// <summary>
    /// Setting this value changes the zoom factor of the camera. Value has to be between MinZoomFactor and MaxZoomFactor.
    /// Changing the camera resets this value.
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
    /// /// Returns maximum zoom factor for current camera.
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
    /// /// Returns zoom factors that switch between camera lenses (iOS only).
    /// </summary>
    public float[] DeviceSwitchZoomFactor
    {
        get => (float[])GetValue(DeviceSwitchZoomFactorProperty);
        set => SetValue(DeviceSwitchZoomFactorProperty, value);
    }

    public event EventHandler<OnDetectionFinishedEventArg> OnDetectionFinished;
    public event EventHandler<OnImageCapturedEventArg> OnImageCaptured;

    private readonly HashSet<BarcodeResult> _pooledResults;
    private readonly Timer _poolingTimer; 

    public CameraView()
    {
        _pooledResults = [];
        _poolingTimer = new()
        {
            AutoReset = false
        };
        _poolingTimer.Elapsed += PoolingTimer_Elapsed;
    }

    internal void DetectionFinished(HashSet<BarcodeResult> barCodeResults)
    {
        if (barCodeResults is null)
            return;

        if (PoolingInterval > 0)
        {
            if (!_poolingTimer.Enabled)
            {
                _poolingTimer.Interval = PoolingInterval;
                _poolingTimer.Start();
            }

            foreach (var result in barCodeResults)
            {
                if (!_pooledResults.Add(result))
                {
                    if (_pooledResults.TryGetValue(result, out var currentResult))
                    {
                        currentResult.PreviewBoundingBox = result.PreviewBoundingBox;
                        currentResult.ImageBoundingBox = result.ImageBoundingBox;
                    }
                }
            }
        }
        else
        {
            TriggerOnDetectionFinished(barCodeResults);
        }
    }

    internal void ResetRequestZoomFactor()
    {
        RequestZoomFactor = -1f;
    }

    private void PoolingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        TriggerOnDetectionFinished(_pooledResults);
        _pooledResults.Clear();
    }

    private void TriggerOnDetectionFinished(HashSet<BarcodeResult> barcodeResults)
    {
        if (PauseScanning)
            return;
    
        try
        {
            if (VibrationOnDetected && barcodeResults.Count > 0)
                Vibration.Vibrate();
        }
        catch (Exception)
        {
        }

        BarcodeResult[] results;
        if (barcodeResults.Count == 0)
            results = [];
        else
            results = [.. barcodeResults];

        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnDetectionFinished?.Invoke(this, new OnDetectionFinishedEventArg { BarcodeResults = results });
            if (OnDetectionFinishedCommand?.CanExecute(results) ?? false)
                OnDetectionFinishedCommand?.Execute(results);
        });
    }
    
    internal void TriggerOnImageCaptured(PlatformImage image)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnImageCaptured?.Invoke(this, new OnImageCapturedEventArg { Image = image });
            if (OnImageCapturedCommand?.CanExecute(image) ?? false)
                OnImageCapturedCommand?.Execute(image);
        });
    }
}
