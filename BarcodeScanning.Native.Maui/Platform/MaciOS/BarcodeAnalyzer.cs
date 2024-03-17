using AVFoundation;
using CoreMedia;
using Vision;

namespace BarcodeScanning;

internal class BarcodeAnalyzer : AVCaptureVideoDataOutputSampleBufferDelegate
{
    private readonly HashSet<BarcodeResult> _barcodeResults;
    private readonly BarcodeView _barcodeView;
    private readonly CameraView _cameraView;
    private readonly VNSequenceRequestHandler _sequenceRequestHandler;

    private VNDetectBarcodesRequest _barcodeRequest;

    internal BarcodeAnalyzer(CameraView cameraView, BarcodeView barcodeView)
    {
        _barcodeView = barcodeView;
        _cameraView = cameraView;

        _barcodeResults = [];
        _sequenceRequestHandler = new();
    }

    public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
    {
        try
        {
            if (_barcodeRequest is null)
            {
                _barcodeRequest = new VNDetectBarcodesRequest((request, error) => 
                {
                    if (error is null)
                        Methods.ProcessBarcodeResult(request.GetResults<VNBarcodeObservation>(), _barcodeResults, _barcodeView.PreviewLayer);
                });

                var selectedSymbologies = Methods.SelectedSymbologies(_cameraView.BarcodeSymbologies);
                if (selectedSymbologies is not null)
                    _barcodeRequest.Symbologies = selectedSymbologies;
            }

            if (sampleBuffer is null || _cameraView.PauseScanning)
                return;

            _sequenceRequestHandler.Perform([_barcodeRequest], sampleBuffer, out _);

            if (_cameraView.AimMode)
            {
                var previewCenter = new Point(_barcodeView.PreviewLayer.Bounds.Width / 2, _barcodeView.PreviewLayer.Bounds.Height / 2);

                foreach (var barcode in _barcodeResults)
                {
                    if (!barcode.BoundingBox.Contains(previewCenter))
                        _barcodeResults.Remove(barcode);
                }
            }

            if (_cameraView.ViewfinderMode)
            {
                var previewRect = new RectF(0, 0, (float)_barcodeView.PreviewLayer.Bounds.Width, (float)_barcodeView.PreviewLayer.Bounds.Height);

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
                sampleBuffer?.Dispose();
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
                _barcodeRequest?.Dispose();
                _sequenceRequestHandler?.Dispose();
            }
            catch (Exception)
            {
            }
        }

        base.Dispose(disposing);
    }
}
