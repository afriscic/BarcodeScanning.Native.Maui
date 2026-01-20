using Android.Gms.Tasks;
using Android.Graphics;
using Android.Runtime;
using AndroidX.Camera.Core;
using AndroidX.Camera.View.Transform;
using Microsoft.Maui.Graphics.Platform;
using System.Diagnostics;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;

using Point = Microsoft.Maui.Graphics.Point;
using Rect = Microsoft.Maui.Graphics.Rect;
using Scanner = Xamarin.Google.MLKit.Vision.BarCode.BarcodeScanning;
using Size = Android.Util.Size;

namespace BarcodeScanning;

internal class BarcodeAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
{
    public Size DefaultTargetResolution => Methods.TargetResolution(null);
    public int TargetCoordinateSystem => ImageAnalysis.CoordinateSystemViewReferenced;

    private readonly CameraManager? _cameraManager;
    private readonly HashSet<BarcodeResult> _barcodeResults;

    private IBarcodeScanner? _barcodeScanner;
    private CoordinateTransform? _coordinateTransform;

    private bool _updateCoordinateTransform = false;
    private Point _previewViewCenter = Point.Zero;
    private Rect _previewViewRect = Rect.Zero;

    internal BarcodeAnalyzer(CameraManager? cameraManager)
    {
        _cameraManager = cameraManager;
        _barcodeResults = [];
    }

    internal void UpdateSymbologies()
    {
        _barcodeScanner?.Dispose();
        _barcodeScanner = Scanner.GetClient(new BarcodeScannerOptions.Builder()
            .SetBarcodeFormats(Methods.ConvertBarcodeFormats(_cameraManager?.CameraView?.BarcodeSymbologies ?? BarcodeFormats.All))
            .Build());
    }

    public void Analyze(IImageProxy? proxy)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(proxy?.Image);
            ArgumentNullException.ThrowIfNull(_cameraManager?.CameraView);
            ArgumentNullException.ThrowIfNull(_cameraManager?.PreviewView);
            ArgumentNullException.ThrowIfNull(_barcodeScanner);

            if (_cameraManager.CameraView.PauseScanning || _cameraManager.CameraView.ProcessingDetected)
                return;

            if (_updateCoordinateTransform)
            {
                _coordinateTransform?.Dispose();
                _coordinateTransform = _cameraManager.GetCoordinateTransform(proxy);

                _previewViewCenter.X = _cameraManager.PreviewView.Width * 0.5;
                _previewViewCenter.Y = _cameraManager.PreviewView.Height * 0.5;
                _previewViewRect.Width = _cameraManager.PreviewView.Width;
                _previewViewRect.Height = _cameraManager.PreviewView.Height;

                _updateCoordinateTransform = false;
            }

            using var inputImage = InputImage.FromMediaImage(proxy.Image, 0);
            using var task  = _barcodeScanner.Process(inputImage);
            var result = TasksClass.Await(task);

            Java.Lang.Object? invertedResult = null;
            if (_cameraManager.CameraView.ForceInverted)
            {
                Methods.InvertLuminance(proxy.Image);
                using var invertedImage = InputImage.FromMediaImage(proxy.Image, 0);
                using var invertedTask = _barcodeScanner.Process(invertedImage);
                invertedResult = TasksClass.Await(invertedTask);
            }

            _barcodeResults.Clear();
            
            AddResultToSet(result);
            AddResultToSet(invertedResult);

            PlatformImage? image = null;
            if (_cameraManager.CameraView.ForceFrameCapture || (_cameraManager.CameraView.CaptureNextFrame && _barcodeResults.Count > 0))
                image = new PlatformImage(proxy.ToBitmap());
            
            _cameraManager.CameraView.DetectionFinished(_barcodeResults, image);

            result?.Dispose();
            invertedResult?.Dispose();
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

    private void AddResultToSet(Java.Lang.Object? result)
    {
        if (result is not JavaList javaList)
            return;

        foreach (Barcode barcode in javaList)
        {
            if (barcode is null)
                continue;
            if (string.IsNullOrEmpty(barcode.DisplayValue) && string.IsNullOrEmpty(barcode.RawValue))
                continue;

            var barcodeResult = barcode.AsBarcodeResult(_coordinateTransform);

            if (_cameraManager?.CameraView?.AimMode == true && !barcodeResult.PreviewBoundingBox.Contains(_previewViewCenter))
                continue;
            if (_cameraManager?.CameraView?.ViewfinderMode == true && !_previewViewRect.Contains(barcodeResult.PreviewBoundingBox))
                continue;

            _barcodeResults.Add(barcodeResult);
        }
    }

    public void UpdateTransform(Matrix? matrix)
    {
        _updateCoordinateTransform = true;
    }

    protected override void Dispose(bool disposing)
    {
        _coordinateTransform?.Dispose();
        _barcodeScanner?.Dispose();
        
        base.Dispose(disposing);
    }
}