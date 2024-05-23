using AndroidX.Camera.Core;
using System.Diagnostics;

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

    public void Analyze(IImageProxy proxy)
    {
        try
        {
            if (proxy is not null)
            {
                if (_cameraManager.CaptureNextFrame)
                    _cameraManager.CaptureImage(proxy);
                else
                    _cameraManager.PerformBarcodeDetection(proxy).Wait(2000);
            }
                
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            try
            {
                proxy?.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                MainThread.BeginInvokeOnMainThread(() => _cameraManager?.Start());
            }
        }
    }
}