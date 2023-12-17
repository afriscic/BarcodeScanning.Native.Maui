using AVFoundation;
using CoreMedia;
using Vision;

namespace BarcodeScanning.Platforms.iOS;

internal class BarcodeAnalyzer : AVCaptureVideoDataOutputSampleBufferDelegate
{
    private readonly AVCaptureVideoPreviewLayer _previewLayer;
    private readonly CameraView _cameraView;
    private readonly VNDetectBarcodesRequest _barcodeRequest;
    private readonly VNSequenceRequestHandler _sequenceRequestHandler;

    private HashSet<BarcodeResult> _barcodeResults;

    internal BarcodeAnalyzer(CameraView cameraView, AVCaptureVideoPreviewLayer previewLayer)
    {
        _cameraView = cameraView;
        _previewLayer = previewLayer;
        _sequenceRequestHandler = new VNSequenceRequestHandler();
        _barcodeRequest = new VNDetectBarcodesRequest((request, error) => {
            if (error is null)
                _barcodeResults = Methods.ProcessBarcodeResult(request.GetResults<VNBarcodeObservation>(), _previewLayer);
        });

        var selectedSymbologies = Methods.SelectedSymbologies(_cameraView.BarcodeSymbologies);
        if (selectedSymbologies is not null)
            _barcodeRequest.Symbologies = selectedSymbologies;
    }

    public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
    {
        try
        {
            if (sampleBuffer is null || _cameraView.PauseScanning)
                return;

            _sequenceRequestHandler.Perform([_barcodeRequest], sampleBuffer, out _);

            if (_cameraView.AimMode)
            {
                var previewCenter = new Point(_previewLayer.Bounds.Width / 2, _previewLayer.Bounds.Height / 2);

                foreach (var barcode in _barcodeResults)
                {
                    if (!barcode.BoundingBox.Contains(previewCenter))
                        _barcodeResults.Remove(barcode);
                }
            }

            if (_cameraView.ViewfinderMode)
            {
                var previewRect = new RectF(0, 0, (float)_previewLayer.Bounds.Width, (float)_previewLayer.Bounds.Height);

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
            SafeCloseSampleBuffer(sampleBuffer);
        }
    }

    private static void SafeCloseSampleBuffer(CMSampleBuffer buffer)
    {
        try
        {
            buffer?.Dispose();
        }
        catch (Exception)
        {

        }
    }
}
