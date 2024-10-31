using AVFoundation;
using CoreImage;
using CoreMedia;
using Microsoft.Maui.Graphics.Platform;
using System.Diagnostics;
using UIKit;
using Vision;

namespace BarcodeScanning;

internal class BarcodeAnalyzer : AVCaptureVideoDataOutputSampleBufferDelegate
{
    private readonly HashSet<BarcodeResult> _barcodeResults;
    private readonly CameraManager _cameraManager;
    private readonly VNDetectBarcodesRequest _detectBarcodesRequest;
    private readonly VNSequenceRequestHandler _sequenceRequestHandler;
    private int consecutiveCount =0;
    private TaskCompletionSource<BarcodeResult> barcodeCompletionSource= new TaskCompletionSource<BarcodeResult>();
    private string lastDetectedBarcode = "";
    internal BarcodeAnalyzer(CameraManager cameraManager)
    {
        _barcodeResults = [];
        _cameraManager = cameraManager;
        _detectBarcodesRequest = new VNDetectBarcodesRequest((request, error) => 
        {
            if (error is null)
                Methods.ProcessBarcodeResult(request.GetResults<VNBarcodeObservation>(), _barcodeResults, _cameraManager.PreviewLayer);
        });
        _sequenceRequestHandler = new VNSequenceRequestHandler();
    }

    internal void UpdateSymbologies()
    {
        if (_cameraManager?.CameraView is not null)
            _detectBarcodesRequest.Symbologies = Methods.SelectedSymbologies(_cameraManager.CameraView.BarcodeSymbologies);
    }

    public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
    {
        try
        {
            if (_cameraManager.CameraView.CaptureNextFrame)
            {
                _cameraManager.CameraView.CaptureNextFrame = false;
                using var imageBuffer = sampleBuffer.GetImageBuffer();
                using var cIImage = new CIImage(imageBuffer);
                using var cIContext = new CIContext();
                using var cGImage = cIContext.CreateCGImage(cIImage, cIImage.Extent);
                var image = new PlatformImage(new UIImage(cGImage));
                _cameraManager.CameraView.TriggerOnImageCaptured(image);
            }

            _barcodeResults.Clear();
            _sequenceRequestHandler?.Perform([_detectBarcodesRequest], sampleBuffer, out _);

            if (_cameraManager.CameraView.AimMode)
            {
                var previewCenter = new Point(_cameraManager.PreviewLayer.Bounds.Width / 2, _cameraManager.PreviewLayer.Bounds.Height / 2);

                foreach (var barcode in _barcodeResults)
                {
                    if (!barcode.PreviewBoundingBox.Contains(previewCenter))
                        _barcodeResults.Remove(barcode);
                    else
                        {
                            if (barcode != null)
                                {
                                    if (barcode.DisplayValue.Equals(lastDetectedBarcode))
                                    {
                                        consecutiveCount++;
                                        //Checksum added to keep the barcode more error free- especially in streaming source
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
                var previewRect = new RectF(0, 0, (float)_cameraManager.PreviewLayer.Bounds.Width, (float)_cameraManager.PreviewLayer.Bounds.Height);

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
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            try
            {
                sampleBuffer?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
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
