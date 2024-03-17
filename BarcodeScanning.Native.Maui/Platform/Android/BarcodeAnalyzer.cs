using Android.Gms.Extensions;
using AndroidX.Camera.Core;
using AndroidX.Camera.View.Transform;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;
using Size = Android.Util.Size;

namespace BarcodeScanning;

internal class BarcodeAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
{
    public Size DefaultTargetResolution => Methods.TargetResolution(CaptureQuality.Medium);
    public int TargetCoordinateSystem => ImageAnalysis.CoordinateSystemOriginal;

    private readonly HashSet<BarcodeResult> _barcodeResults;
    private readonly BarcodeView _barcodeView;
    private readonly CameraView _cameraView;

    private IBarcodeScanner _barcodeScanner;

    internal BarcodeAnalyzer(CameraView cameraView, BarcodeView barcodeView)
    {
        _cameraView = cameraView;
        _barcodeView = barcodeView;
        
        _barcodeResults = [];
    }

    public async void Analyze(IImageProxy proxy)
    {
        try
        {
            _barcodeScanner ??= Xamarin.Google.MLKit.Vision.BarCode.BarcodeScanning.GetClient(new BarcodeScannerOptions.Builder()
                        .SetBarcodeFormats(Methods.ConvertBarcodeFormats(_cameraView.BarcodeSymbologies))
                        .Build());

            if (proxy is null || _cameraView.PauseScanning)
                return;

            using var target = await MainThread.InvokeOnMainThreadAsync(() => _barcodeView.PreviewView.OutputTransform);
            using var source = new ImageProxyTransformFactory
            {
                UsingRotationDegrees = true
            }
            .GetOutputTransform(proxy);
            using var coordinateTransform = new CoordinateTransform(source, target);

            using var image = InputImage.FromMediaImage(proxy.Image, proxy.ImageInfo.RotationDegrees);
            using var results = await _barcodeScanner.Process(image);
            
            Methods.ProcessBarcodeResult(results, _barcodeResults, coordinateTransform);

            if (_cameraView.ForceInverted)
            {
                Methods.InvertLuminance(proxy.Image);
                using var invertedimage = InputImage.FromMediaImage(proxy.Image, proxy.ImageInfo.RotationDegrees);
                using var invertedresults = await _barcodeScanner.Process(invertedimage);

                Methods.ProcessBarcodeResult(invertedresults, _barcodeResults, coordinateTransform);
            }

            if (_cameraView.AimMode)
            {
                var previewCenter = new Point(_barcodeView.PreviewView.Width / 2, _barcodeView.PreviewView.Height / 2);

                foreach (var barcode in _barcodeResults)
                {
                    if (!barcode.BoundingBox.Contains(previewCenter))
                        _barcodeResults.Remove(barcode);
                }
            }

            if (_cameraView.ViewfinderMode)
            {
                var previewRect = new RectF(0, 0, _barcodeView.PreviewView.Width, _barcodeView.PreviewView.Height);

                foreach (var barcode in _barcodeResults)
                {
                    if (!previewRect.Contains(barcode.BoundingBox))
                        _barcodeResults.Remove(barcode);
                }
            }

            _cameraView?.DetectionFinished(_barcodeResults);
        }
        catch (Exception)
        {
        }
        finally
        {
            try
            {
                _barcodeResults.Clear();
                proxy?.Close();
            }
            catch (Exception)
            {
                MainThread.BeginInvokeOnMainThread(() => 
                { 
                    try 
                    { 
                        _barcodeView?.Start(); 
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
        {
            try
            {
                _barcodeScanner?.Dispose();
            }
            catch (Exception)
            {
            }
        }

        base.Dispose(disposing);
    }
}