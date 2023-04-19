using Android.Util;
using AndroidX.Camera.Core;
using AndroidX.Camera.View;
using AndroidX.Camera.View.Transform;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;

namespace BarcodeScanning.Platforms.Android;

internal class BarcodeAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
{
    private readonly IBarcodeScanner _barcodeScanner;
    private readonly ICameraView _cameraView;
    private readonly PreviewView _previewView;

    internal BarcodeAnalyzer(ICameraView cameraView, PreviewView previewView)
    {
        _cameraView = cameraView;
        _previewView = previewView;
        _barcodeScanner = Xamarin.Google.MLKit.Vision.BarCode.BarcodeScanning.GetClient(new BarcodeScannerOptions.Builder()
            .SetBarcodeFormats(Methods.ConvertBarcodeFormats(_cameraView.BarcodeSymbologies))
            .Build());
    }

    public async void Analyze(IImageProxy proxy)
    {
        try
        {
            if (proxy is null || proxy.Image is null || _cameraView.PauseScanning)
                return;

            var target = await MainThread.InvokeOnMainThreadAsync(() => _previewView.OutputTransform);
            var source = new ImageProxyTransformFactory
            {
                UsingRotationDegrees = true
            }
            .GetOutputTransform(proxy);
            var coordinateTransform = new CoordinateTransform(source, target);

            var image = InputImage.FromMediaImage(proxy.Image, proxy.ImageInfo.RotationDegrees);
            var results = await ToAwaitableTask(_barcodeScanner.Process(image));

            var _barcodeResults = Methods.ProcessBarcodeResult(results, coordinateTransform);

            if (_cameraView.ForceInverted)
            {
                Methods.InvertLuminance(proxy.Image);
                image = InputImage.FromMediaImage(proxy.Image, proxy.ImageInfo.RotationDegrees);
                results = await ToAwaitableTask(_barcodeScanner.Process(image));

                _barcodeResults.UnionWith(Methods.ProcessBarcodeResult(results, coordinateTransform));
            }

            if (_barcodeResults is not null && _cameraView is not null)
                _cameraView.DetectionFinished(_barcodeResults.ToHashSet());
        }
        catch (Java.Lang.Exception ex)
        {
            Log.Debug(nameof(BarcodeAnalyzer), ex.ToString());
        }
        catch (Exception ex)
        {
            Log.Debug(nameof(BarcodeAnalyzer), ex.ToString());
        }
        finally
        {
            SafeCloseImageProxy(proxy);
        }
    }

    private static void SafeCloseImageProxy(IImageProxy proxy)
    {
        try
        {
            proxy?.Close();
        }
        catch (Exception) 
        {

        }
    }

    private static Task<Java.Lang.Object> ToAwaitableTask(global::Android.Gms.Tasks.Task task)
    {
        var taskCompletionSource = new TaskCompletionSource<Java.Lang.Object>();
        var taskCompleteListener = new TaskCompleteListener(taskCompletionSource);
        task.AddOnCompleteListener(taskCompleteListener);

        return taskCompletionSource.Task;
    }
}
