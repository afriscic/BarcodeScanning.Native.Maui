using System.Windows.Input;

namespace BarcodeScanning;

public interface ICameraView : IView
{
    public static BindableProperty OnDetectionFinishedCommandProperty { get; set; }
    public ICommand OnDetectionFinishedCommand { get; set; }

    public static BindableProperty VibrationOnDetectedProperty { get; set; }
    /// <summary>
    /// Disables or enables vibration on barcode detection.
    /// </summary>
    public bool VibrationOnDetected { get; set; }

    public static BindableProperty CameraEnabledProperty { get; set; }
    /// <summary>
    /// Disables or enables camera.
    /// </summary>
    public bool CameraEnabled { get; set; }

    public static BindableProperty PauseScanningProperty { get; set; }
    /// <summary>
    /// Pauses barcode scanning.
    /// </summary>
    public bool PauseScanning { get; set; }

    public static BindableProperty ForceInvertedProperty { get; set; }
    /// <summary>
    /// Forces scanning of inverted barcodes. Reduces performance significantly. Android only.
    /// </summary>
    public bool ForceInverted { get; set; }

    public static BindableProperty PoolingIntervalProperty { get; set; }
    /// <summary>
    /// Enables pooling of detections for better detection of multiple barcodes at once. 
    /// Value in milliseconds. Default 0 (disabled).
    /// </summary>
    public int PoolingInterval { get; set; }

    public static BindableProperty TorchOnProperty { get; set; }
    /// <summary>
    /// Disables or enables torch.
    /// </summary>
    public bool TorchOn { get; set; }

    public static BindableProperty TapToFocusEnabledProperty { get; set; }
    /// <summary>
    /// Disables or enables tap-to-focus.
    /// </summary>
    public bool TapToFocusEnabled { get; set; }

    public static BindableProperty CameraFacingProperty { get; set; }
    /// <summary>
    /// Select Back or Front camera.
    /// Default value is Back Camera.
    /// </summary>
    public CameraFacing CameraFacing { get; set; }

    public static BindableProperty CaptureQualityProperty { get; set; }
    /// <summary>
    /// Set the capture quality for the image analysys.
    /// Reccomended and default value is Medium.
    /// Use highest values for more precision or lower for fast scanning.
    /// </summary>
    public CaptureQuality CaptureQuality { get; set; }

    public static BindableProperty BarcodeSymbologiesProperty { get; set; }
    /// <summary>
    /// Set the barcode symbologies for the image analysys.
    /// Default value is All.
    /// </summary>
    public BarcodeFormats BarcodeSymbologies { get; set; }

    public event EventHandler<OnDetectionFinishedEventArg> OnDetectionFinished;
    internal void DetectionFinished(HashSet<BarcodeResult> barCodeResults);
}