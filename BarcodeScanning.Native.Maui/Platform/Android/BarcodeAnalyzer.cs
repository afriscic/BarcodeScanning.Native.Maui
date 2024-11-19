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

using MLKitBarcodeScanning = Xamarin.Google.MLKit.Vision.BarCode.BarcodeScanning;
using Point = Microsoft.Maui.Graphics.Point;
using RectF = Microsoft.Maui.Graphics.RectF;
using Size = Android.Util.Size;

namespace BarcodeScanning;

internal class BarcodeAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer, IOnSuccessListener, IOnCompleteListener
{
    public Size DefaultTargetResolution => Methods.TargetResolution(null);
    public int TargetCoordinateSystem => ImageAnalysis.CoordinateSystemViewReferenced;

    private IBarcodeScanner? _barcodeScanner;
    private CoordinateTransform? _coordinateTransform;
    private IImageProxy? _proxy;

    private bool _processInverted = false;
    private bool _updateCoordinateTransform = false;
    private int _transformDegrees = 0;
    private Point _previewViewCenter = new();
    private RectF _previewViewRect = new();

    private readonly HashSet<BarcodeResult> _barcodeResults;
    private readonly CameraManager _cameraManager;
    private readonly Lock _resultsLock;

    internal BarcodeAnalyzer(CameraManager cameraManager)
    {
        _barcodeResults = [];
        _cameraManager = cameraManager;
        _resultsLock = new();

        _previewViewRect.X = 0;
        _previewViewRect.Y = 0;

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
            ArgumentNullException.ThrowIfNull(_barcodeScanner);

            _processInverted = _cameraManager.CameraView.ForceInverted;

            if (_cameraManager.CameraView.PauseScanning)
            {
                CloseProxy();
                return;
            }

            if (_cameraManager.CameraView.CaptureNextFrame)
            {
                _cameraManager.CameraView.CaptureNextFrame = false;
                var image = new PlatformImage(_proxy.ToBitmap());
                _cameraManager.CameraView.TriggerOnImageCaptured(image);
            }

            if (_updateCoordinateTransform || _transformDegrees != _proxy.ImageInfo.RotationDegrees)
            {
                _coordinateTransform = _cameraManager.GetCoordinateTransform(_proxy);
                _transformDegrees = _proxy.ImageInfo.RotationDegrees;

                _previewViewCenter.X = _cameraManager.PreviewView.Width / 2;
                _previewViewCenter.Y = _cameraManager.PreviewView.Height / 2;
                _previewViewRect.Width = _cameraManager.PreviewView.Width;
                _previewViewRect.Height = _cameraManager.PreviewView.Height;

                _updateCoordinateTransform = false;
            }

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
            lock (_resultsLock)
            {
                if (_processInverted == _cameraManager?.CameraView?.ForceInverted)
                    _barcodeResults.Clear();

                if (result is not JavaList javaList)
                    return;
                
                foreach (Barcode barcode in javaList)
                {
                    if (barcode is null)
                        continue;
                    if (string.IsNullOrEmpty(barcode.DisplayValue) && string.IsNullOrEmpty(barcode.RawValue))
                        continue;

                    var barcodeResult = barcode.AsBarcodeResult(_coordinateTransform);

                    if ((_cameraManager?.CameraView?.AimMode ?? false) && !barcodeResult.PreviewBoundingBox.Contains(_previewViewCenter))
                        continue;
                    if ((_cameraManager?.CameraView?.ViewfinderMode ?? false) && !_previewViewRect.Contains(barcodeResult.PreviewBoundingBox))
                        continue;

                    _barcodeResults.Add(barcodeResult);
                }   
            }

            if (!_processInverted)
                _cameraManager?.CameraView?.DetectionFinished(_barcodeResults, _resultsLock);          
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