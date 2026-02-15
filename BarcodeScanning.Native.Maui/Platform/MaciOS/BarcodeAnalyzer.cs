using AVFoundation;
using CoreGraphics;
using CoreImage;
using CoreMedia;
using Microsoft.Maui.Graphics.Platform;
using UIKit;
using Vision;

namespace BarcodeScanning;

internal class BarcodeAnalyzer : AVCaptureVideoDataOutputSampleBufferDelegate
{
    private readonly CameraManager? _cameraManager;
    private readonly HashSet<BarcodeResult> _barcodeResults;
    private readonly VNDetectBarcodesRequest _detectBarcodesRequest;
    private readonly VNSequenceRequestHandler _sequenceRequestHandler;

    private Point _previewCenter = Point.Zero;
    private Rect _previewRect = Rect.Zero;
    private VNBarcodeObservation[] _result = [];

    internal BarcodeAnalyzer(CameraManager? cameraManager)
    {
        _cameraManager = cameraManager;
        _barcodeResults = [];
        _detectBarcodesRequest = new VNDetectBarcodesRequest((request, error) => 
        {
            if (error is null)
                _result = request.GetResults<VNBarcodeObservation>();
            else
                _result = [];
        });
        _sequenceRequestHandler = new VNSequenceRequestHandler();
    }

    internal void UpdateSymbologies()
    {
        _detectBarcodesRequest.Symbologies = Methods.SelectedSymbologies(_cameraManager?.CameraView?.BarcodeSymbologies ?? BarcodeFormats.All);
    }

    public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(_cameraManager?.CameraView);
            ArgumentNullException.ThrowIfNull(_cameraManager?.PreviewLayer);
            ArgumentNullException.ThrowIfNull(sampleBuffer);

            if (_cameraManager.CameraView.PauseScanning || _cameraManager.CameraView.ProcessingDetected)
                return;

            _sequenceRequestHandler.Perform([_detectBarcodesRequest], sampleBuffer, out _);

            if (_cameraManager.CameraView.AimMode)
            {
                _previewCenter.X = _cameraManager.PreviewLayer.Bounds.GetMidX();
                _previewCenter.Y = _cameraManager.PreviewLayer.Bounds.GetMidY();
            }
            if (_cameraManager.CameraView.ViewfinderMode)
            {
                _previewRect.Width = _cameraManager.PreviewLayer.Bounds.Width;
                _previewRect.Height = _cameraManager.PreviewLayer.Bounds.Height;
            }
            
            _barcodeResults.Clear();

            foreach (var barcode in _result)
            {
                if (string.IsNullOrEmpty(barcode.PayloadStringValue))
                    continue;

                var barcodeResult = barcode.AsBarcodeResult(_cameraManager.PreviewLayer);

                if (_cameraManager.CameraView.AimMode &&
                    !barcodeResult.PreviewBoundingBox.Contains(_previewCenter))
                    continue;

                if (_cameraManager.CameraView.ViewfinderMode &&
                    !_previewRect.Contains(barcodeResult.PreviewBoundingBox))
                    continue;

                _barcodeResults.Add(barcodeResult);
            }

            PlatformImage? image = null;
            if (_cameraManager.CameraView.ForceFrameCapture || (_cameraManager.CameraView.CaptureNextFrame && _barcodeResults.Count > 0))
            {
                using var imageBuffer = sampleBuffer.GetImageBuffer();
                if (imageBuffer is not null)
                {
                    using var cIImage = new CIImage(imageBuffer);
                    using var cIContext = new CIContext();
                    using var cGImage = cIContext.CreateCGImage(cIImage, cIImage.Extent);
                    if (cGImage is not null)
                        image = new PlatformImage(new UIImage(cGImage));
                }
            }

            _cameraManager.CameraView.DetectionFinished(_barcodeResults, image);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            try
            {
                sampleBuffer?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                MainThread.BeginInvokeOnMainThread(() => _cameraManager?.Start());
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        _sequenceRequestHandler?.Dispose();
        _detectBarcodesRequest?.Dispose();
        
        base.Dispose(disposing);
    }
}