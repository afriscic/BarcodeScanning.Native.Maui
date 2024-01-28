using System.Windows.Input;
using DependencyPropertyGenerator;
using Timer = System.Timers.Timer;

namespace BarcodeScanning;

[DependencyProperty<ICommand>("OnDetectionFinishedCommand",
    Description = "Command that will be executed when barcode is detected.")]
[DependencyProperty<bool>("VibrationOnDetected", DefaultValue = true,
    Description = "Disables or enables vibration on barcode detection. " +
                  "On Android make sure that the android.permission.VIBRATE permission " +
                  "is declared in the AndroidManifest.xml file.")]
[DependencyProperty<bool>("CameraEnabled",
    Description = "Disables or enables camera.")]
[DependencyProperty<bool>("PauseScanning",
    Description = "Pauses barcode scanning.")]
[DependencyProperty<bool>("ForceInverted",
    Description = "Forces scanning of inverted barcodes. Reduces performance significantly. Android only.")]
[DependencyProperty<int>("PoolingInterval",
    Description = "Enables pooling of detections for better detection of multiple barcodes at once. " +
                  "Value in milliseconds. " +
                  "Default 0 (disabled).")]
[DependencyProperty<bool>("TorchOn",
    Description = "Disables or enables torch.")]
[DependencyProperty<bool>("TapToFocusEnabled",
    Description = "Disables or enables tap-to-focus.")]
[DependencyProperty<CameraFacing>("CameraFacing",
    Description = "Select Back or Front camera." +
                  "Default value is Back Camera.")]
[DependencyProperty<CaptureQuality>("CaptureQuality", DefaultValue = CaptureQuality.Medium,
    Description = "Set the capture quality for the image analysis." +
                  "Recommended and default value is Medium." +
                  "Use highest values for more precision or lower for fast scanning.")]
[DependencyProperty<BarcodeFormats>("BarcodeSymbologies",
    DefaultValue = BarcodeFormats.All,
    Description = "Set the enabled symbologies." +
                  "Default value All.")]
[DependencyProperty<bool>("AimMode",
    Description = "Disables or enables aim mode. " +
                  "When enabled only barcode that is in the center of the preview will be detected.")]
[DependencyProperty<bool>("ViewfinderMode",
    Description = "Disables or enables viewfinder mode. " +
                  "When enabled only barcode that is visible in the preview will be detected.")]
[WeakEvent<OnDetectionFinishedEventArg>("OnDetectionFinished",
    Description = "Event that will be triggered when barcode is detected.")]
public partial class CameraView : View
{
    private readonly HashSet<BarcodeResult> _pooledResults = new();
    private readonly Timer _poolingTimer = new()
    {
        AutoReset = false,
    }; 

    public CameraView()
    {
        this.Loaded += CameraView_Loaded;
        this.Unloaded += CameraView_Unloaded;

        _poolingTimer.Elapsed += PoolingTimer_Elapsed;
    }

    private void CameraView_Loaded(object sender, EventArgs e)
    {
        if (this.Handler is not null)
            DeviceDisplay.Current.MainDisplayInfoChanged += ((CameraViewHandler)this.Handler).Current_MainDisplayInfoChanged;
    }

    private void CameraView_Unloaded(object sender, EventArgs e)
    {
        if (this.Handler is not null)
            DeviceDisplay.Current.MainDisplayInfoChanged -= ((CameraViewHandler)this.Handler).Current_MainDisplayInfoChanged;
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
                        currentResult.BoundingBox = result.BoundingBox;
                }
            }
        }
        else
        {
            TriggerOnDetectionFinished(barCodeResults);
        }
    }

    private void PoolingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        TriggerOnDetectionFinished(_pooledResults);
        _pooledResults.Clear();
    }

    private void TriggerOnDetectionFinished(HashSet<BarcodeResult> barCodeResults)
    {
        try
        {
            if (VibrationOnDetected && barCodeResults.Count > 0)
                Vibration.Vibrate();
        }
        catch (Exception)
        {

        }

        var results = barCodeResults.ToHashSet();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            RaiseOnDetectionFinishedEvent(this, new OnDetectionFinishedEventArg
            {
                BarcodeResults = results,
            });
            
            if (OnDetectionFinishedCommand?.CanExecute(results) ?? false)
                OnDetectionFinishedCommand?.Execute(results);
        });
    }
}
