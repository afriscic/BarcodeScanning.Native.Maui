using Android.Gms.Extensions;
using AndroidX.Camera.Core;
using AndroidX.Camera.View;
using AndroidX.Camera.View.Transform;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;
using Size = Android.Util.Size;

namespace BarcodeScanning;

internal class BarcodeAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
{
    public Size DefaultTargetResolution => Methods.TargetResolution(CaptureQuality.Medium);
    public int TargetCoordinateSystem => ImageAnalysis.CoordinateSystemOriginal;

    private readonly CameraView _cameraView;
    private readonly CameraViewHandler _cameraViewHandler;
    private readonly PreviewView _previewView;

    private IBarcodeScanner _barcodeScanner;

    internal BarcodeAnalyzer(CameraView cameraView, PreviewView previewView, CameraViewHandler cameraViewHandler)
    {
        _cameraView = cameraView;
        _cameraViewHandler = cameraViewHandler;
        _previewView = previewView;
    }

    public async void Analyze(IImageProxy proxy)
    {
        try
        {
            if (proxy is null || _cameraView.PauseScanning)
                return;
            
            _barcodeScanner ??= Xamarin.Google.MLKit.Vision.BarCode.BarcodeScanning.GetClient(new BarcodeScannerOptions.Builder()
                                    .SetBarcodeFormats(Methods.ConvertBarcodeFormats(_cameraView.BarcodeSymbologies))
                                    .Build());

            var target = await MainThread.InvokeOnMainThreadAsync(() => _previewView.OutputTransform);
            var source = new ImageProxyTransformFactory
            {
                UsingRotationDegrees = true
            }
            .GetOutputTransform(proxy);
            var coordinateTransform = new CoordinateTransform(source, target);

            var image = InputImage.FromMediaImage(proxy.Image, proxy.ImageInfo.RotationDegrees);
            var results = await _barcodeScanner.Process(image);

            var _barcodeResults = Methods.ProcessBarcodeResult(results, coordinateTransform);

            if (_cameraView.ForceInverted)
            {
                Methods.InvertLuminance(proxy.Image);
                image = InputImage.FromMediaImage(proxy.Image, proxy.ImageInfo.RotationDegrees);
                results = await _barcodeScanner.Process(image);

                _barcodeResults.UnionWith(Methods.ProcessBarcodeResult(results, coordinateTransform));
            }

            if (_cameraView.AimMode)
            {
                var previewCenter = new Point(_previewView.Width / 2, _previewView.Height / 2);

                foreach (var barcode in _barcodeResults)
                {
                    if (!barcode.BoundingBox.Contains(previewCenter))
                        _barcodeResults.Remove(barcode);
                }
            }

            if (_cameraView.ViewfinderMode)
            {
                var previewRect = new RectF(0, 0, _previewView.Width, _previewView.Height);

                foreach (var barcode in _barcodeResults)
                {
                    if (!previewRect.Contains(barcode.BoundingBox))
                        _barcodeResults.Remove(barcode);
                }
            }

            if (_barcodeResults is not null && _cameraView is not null)
                _cameraView.DetectionFinished(_barcodeResults);
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
                        _cameraViewHandler.Start(); 
                    } 
                    catch (Exception) 
                    { 
                    } 
                });
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _barcodeScanner?.Dispose();

        base.Dispose(disposing);
    }
}