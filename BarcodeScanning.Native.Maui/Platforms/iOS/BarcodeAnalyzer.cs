using AVFoundation;
using CoreMedia;
using Vision;

namespace BarcodeScanning.Platforms.iOS;

internal class BarcodeAnalyzer : AVCaptureVideoDataOutputSampleBufferDelegate
{
    private readonly VNDetectBarcodesRequest _barcodeRequest;
    private readonly VNSequenceRequestHandler _sequenceRequestHandler;
    private readonly AVCaptureVideoPreviewLayer _previewLayer;
    private readonly ICameraView _cameraView;

    private HashSet<BarcodeResult> _barcodeResults;

    internal BarcodeAnalyzer(ICameraView cameraView, AVCaptureVideoPreviewLayer previewLayer)
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

            _sequenceRequestHandler.Perform(new VNRequest[] { _barcodeRequest }, sampleBuffer, out _);

            if (_barcodeResults is not null &&  _cameraView is not null)
                _cameraView.DetectionFinished(_barcodeResults);
        }
        catch (Exception)
        {

        }
        finally
        {
            sampleBuffer?.Dispose();
        }
    }
}
