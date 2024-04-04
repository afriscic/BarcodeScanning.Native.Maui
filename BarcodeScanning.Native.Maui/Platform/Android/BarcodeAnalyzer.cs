using AndroidX.Camera.Core;
using Size = Android.Util.Size;

namespace BarcodeScanning;

internal class BarcodeAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
{
    public Size DefaultTargetResolution => Methods.TargetResolution(CaptureQuality.Medium);
    public int TargetCoordinateSystem => ImageAnalysis.CoordinateSystemOriginal;

    private readonly CameraManager _cameraManager;

    internal BarcodeAnalyzer(CameraManager cameraManager)
    {
        _cameraManager = cameraManager;
    }

    public async void Analyze(IImageProxy proxy)
    {
        try
        {
            if (proxy is not null)
                await _cameraManager.PerformBarcodeDetection(proxy);
        }
        catch (Exception)
        {
        }
        finally
        {
            try
            {
                proxy?.Close();
            }
            catch (Exception)
            {
                MainThread.BeginInvokeOnMainThread(() => 
                { 
                    try 
                    { 
                        _cameraManager?.Start(); 
                    } 
                    catch (Exception) 
                    { 
                    } 
                });
            }
        }
    }
}