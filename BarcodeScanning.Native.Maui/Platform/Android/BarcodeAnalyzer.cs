using Android.Gms.Tasks;
using Android.Graphics;
using AndroidX.Camera.Core;
using AndroidX.Camera.View.Transform;
using Microsoft.Maui.Graphics.Platform;
using System.Diagnostics;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;

using MLKitBarcodeScanning = Xamarin.Google.MLKit.Vision.BarCode.BarcodeScanning;
using Point = Microsoft.Maui.Graphics.Point;
using RectF = Microsoft.Maui.Graphics.RectF;
using Size = Android.Util.Size;

namespace BarcodeScanning;

internal class BarcodeAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer, IOnSuccessListener, IOnCompleteListener
{
    public Size DefaultTargetResolution => Methods.TargetResolution(CaptureQuality.Medium);
    public int TargetCoordinateSystem => ImageAnalysis.CoordinateSystemViewReferenced;

    private IBarcodeScanner? _barcodeScanner;
    private CoordinateTransform? _coordinateTransform;
    private IImageProxy? _proxy;

    private bool _processInverted = false;
    private bool _updateCoordinateTransform = false;

    private readonly HashSet<BarcodeResult> _barcodeResults;
    private readonly CameraManager _cameraManager;

    internal BarcodeAnalyzer(CameraManager cameraManager)
    {
        _barcodeResults = [];
        _cameraManager = cameraManager;

        UpdateSymbologies();
    }

    internal void UpdateSymbologies()
    {
        if (_cameraManager?.CameraView is not null)
        {
            _barcodeScanner?.Dispose();
            _barcodeScanner = MLKitBarcodeScanning.GetClient(new BarcodeScannerOptions.Builder()
                .SetBarcodeFormats(Methods.ConvertBarcodeFormats(_cameraManager.CameraView.BarcodeSymbologies))
                .Build());
        }
    }

    public void Analyze(IImageProxy proxy)
    {
        _proxy = proxy;

        try
        {
            ArgumentNullException.ThrowIfNull(_proxy?.Image);
            ArgumentNullException.ThrowIfNull(_cameraManager?.CameraView);

            _barcodeResults.Clear();
            _processInverted = _cameraManager.CameraView.ForceInverted;

            if (_cameraManager.CameraView.CaptureNextFrame)
            {
                _cameraManager.CameraView.CaptureNextFrame = false;
                var image = new PlatformImage(_proxy.ToBitmap());
                _cameraManager.CameraView.TriggerOnImageCaptured(image);
            }

            if (_updateCoordinateTransform)
            {
                _coordinateTransform = _cameraManager.GetCoordinateTransform(_proxy);
                _updateCoordinateTransform = false;
            }
            
            ArgumentNullException.ThrowIfNull(_barcodeScanner);
            using var inputImage = InputImage.FromMediaImage(_proxy.Image, _proxy.ImageInfo.RotationDegrees);
            _barcodeScanner.Process(inputImage).AddOnSuccessListener(this).AddOnCompleteListener(this);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            CloseProxy();
        }
    }

    public void OnSuccess(Java.Lang.Object? result)
    {
        try
        {   
            Methods.ProcessBarcodeResult(result, _barcodeResults, _coordinateTransform);

            if (!_processInverted)
            {
                if (_cameraManager?.CameraView?.AimMode ?? false)
                {
                    var previewCenter = new Point(_cameraManager.PreviewView.Width / 2, _cameraManager.PreviewView.Height / 2);

                    foreach (var barcode in _barcodeResults)
                    {
                        if (!barcode.PreviewBoundingBox.Contains(previewCenter))
                            _barcodeResults.Remove(barcode);
                    }
                }

                if (_cameraManager?.CameraView?.ViewfinderMode ?? false)
                {
                    var previewRect = new RectF(0, 0, _cameraManager.PreviewView.Width, _cameraManager.PreviewView.Height);

                    foreach (var barcode in _barcodeResults)
                    {
                        if (!previewRect.Contains(barcode.PreviewBoundingBox))
                            _barcodeResults.Remove(barcode);
                    }
                }

                _cameraManager?.CameraView?.DetectionFinished(_barcodeResults);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    public void OnComplete(Android.Gms.Tasks.Task task)
    {
        if (_processInverted)
        {
            try
            {
                _processInverted = false;

                ArgumentNullException.ThrowIfNull(_proxy?.Image);
                ArgumentNullException.ThrowIfNull(_barcodeScanner);

                Methods.InvertLuminance(_proxy.Image);
                using var inputImage = InputImage.FromMediaImage(_proxy.Image, _proxy.ImageInfo.RotationDegrees);
                _barcodeScanner.Process(inputImage).AddOnSuccessListener(this).AddOnCompleteListener(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                CloseProxy();
            }
        }
        else
        {
            CloseProxy();
        }
    }

    private void CloseProxy()
    {
        try
        {
            _proxy?.Close();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            MainThread.BeginInvokeOnMainThread(() => _cameraManager?.Start());
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