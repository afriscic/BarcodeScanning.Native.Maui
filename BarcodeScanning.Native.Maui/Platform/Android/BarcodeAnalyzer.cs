using Android.Gms.Tasks;
using AndroidX.Camera.Core;
using AndroidX.Camera.View.Transform;
using Microsoft.Maui.Graphics.Platform;
using System.Diagnostics;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;

using MLKitBarcodeScanning = Xamarin.Google.MLKit.Vision.BarCode.BarcodeScanning;
using Size = Android.Util.Size;

namespace BarcodeScanning;

internal class BarcodeAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer, IOnSuccessListener, IOnCompleteListener
{
    public Size DefaultTargetResolution => Methods.TargetResolution(CaptureQuality.Medium);
    public int TargetCoordinateSystem => ImageAnalysis.CoordinateSystemOriginal;

    private IBarcodeScanner _barcodeScanner;
    private CoordinateTransform _coordinateTransform;
    private bool _processInverted;
    private IImageProxy _proxy;
    private String lastDetectedBarcode = "";
    private int consecutiveCount = 0;

    private readonly HashSet<BarcodeResult> _barcodeResults;
    private readonly CameraManager _cameraManager;
    private TaskCompletionSource<BarcodeResult> barcodeCompletionsource= new  TaskCompletionSource<BarcodeResult>();

    internal BarcodeAnalyzer(CameraManager cameraManager)
    {
        _barcodeResults = [];
        _cameraManager = cameraManager;
        _processInverted = false;

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
            _barcodeScanner?.Process(inputImage).AddOnSuccessListener(this).AddOnCompleteListener(this);
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
                        else
                        {
                            if (barcode != null)
                            {
                            //Iterates atleast 3 times to check it the code from subsequent frame are same - Streaming camera source -keeps getting incorrect results
                                if (barcode.DisplayValue.Equals(lastDetectedBarcode))
                                {
                                    consecutiveCount++;
                                    if (consecutiveCount >= 3)
                                    {
                                        Console.WriteLine("bar code recognized");
                                        barcodeCompletionSource.SetResult(barcode);
                                        // Call your .NET MAUI backend with the barcode value
                                    }
                                }
                                else
                                {
                                    barcodeCompletionSource = new  TaskCompletionSource<BarcodeResult>();
                                    lastDetectedBarcode = barcode.DisplayValue;
                                    consecutiveCount = 1;
                                }
                            }
                        }
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

                MainThread.BeginInvokeOnMainThread( async()=>{
                    var recognizedbarcodeResult = await barcodeCompletionSource.Task;
                    if(!string.IsNullOrEmpty(recognizedbarcodeResult?.DisplayValue))
                        _cameraManager?.CameraView?.DetectionFinished(new HashSet<BarcodeResult>(){recognizedbarcodeResult});
                });
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
                _barcodeScanner?.Process(inputImage).AddOnSuccessListener(this).AddOnCompleteListener(this);
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
        _barcodeScanner?.Dispose();
        
        base.Dispose(disposing);
    }
}
