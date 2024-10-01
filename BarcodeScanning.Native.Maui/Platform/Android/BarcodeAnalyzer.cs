using Android.Gms.Tasks;
using AndroidX.Camera.Core;
using AndroidX.Camera.View.Transform;
using Microsoft.Maui.Graphics.Platform;
using System.Diagnostics;
using Xamarin.Google.MLKit.Vision.Common;

using Size = Android.Util.Size;

namespace BarcodeScanning;

internal class BarcodeAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer, IOnSuccessListener, IOnCompleteListener
{
    public Size DefaultTargetResolution => Methods.TargetResolution(CaptureQuality.Medium);
    public int TargetCoordinateSystem => ImageAnalysis.CoordinateSystemOriginal;

    private CoordinateTransform _coordinateTransform;
    private bool _processInverted;
    private IImageProxy _proxy;

    private readonly HashSet<BarcodeResult> _barcodeResults;
    private readonly CameraManager _cameraManager;

    internal BarcodeAnalyzer(CameraManager cameraManager)
    {
        _barcodeResults = [];
        _cameraManager = cameraManager;
        _processInverted = false;
    }

    public void Analyze(IImageProxy proxy)
    {
        try
        {
            _proxy = proxy;
            _barcodeResults.Clear();

            if (_cameraManager.CameraView.CaptureNextFrame)
            {
                _cameraManager.CameraView.CaptureNextFrame = false;
                var image = new PlatformImage(_proxy.ToBitmap());
                _cameraManager.CameraView.TriggerOnImageCaptured(image);
            }

            if (_cameraManager.RecalculateCoordinateTransform || _coordinateTransform is null)
                _coordinateTransform = _cameraManager.GetCoordinateTransform(_proxy);

            _processInverted = _cameraManager.CameraView.ForceInverted;
            using var inputImage = InputImage.FromMediaImage(_proxy.Image, _proxy.ImageInfo.RotationDegrees);
            _cameraManager.BarcodeScanner.Process(inputImage).AddOnSuccessListener(this).AddOnCompleteListener(this);
        }
        catch (Exception)
        {
            CloseProxy();
        }
    }

    public void OnSuccess(Java.Lang.Object result)
    {
        try
        {
            Methods.ProcessBarcodeResult(result, _barcodeResults, _coordinateTransform);

            if (!_processInverted)
            {
                if (_cameraManager.CameraView.AimMode)
                {
                    var previewCenter = new Point(_cameraManager.PreviewView.Width / 2, _cameraManager.PreviewView.Height / 2);

                    foreach (var barcode in _barcodeResults)
                    {
                        if (!barcode.PreviewBoundingBox.Contains(previewCenter))
                            _barcodeResults.Remove(barcode);
                    }
                }

                if (_cameraManager.CameraView.ViewfinderMode)
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
        catch (Exception)
        {
        }
    }

    public void OnComplete(Android.Gms.Tasks.Task task)
    {
        if (_processInverted)
        {
            try
            {
                Methods.InvertLuminance(_proxy.Image);

                _processInverted = false;
                using var inputImage = InputImage.FromMediaImage(_proxy.Image, _proxy.ImageInfo.RotationDegrees);
                _cameraManager.BarcodeScanner.Process(inputImage).AddOnSuccessListener(this).AddOnCompleteListener(this);
            }
            catch (Exception)
            {
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

    protected override void Dispose(bool disposing)
    {
        _coordinateTransform?.Dispose();
        
        base.Dispose(disposing);
    }
}