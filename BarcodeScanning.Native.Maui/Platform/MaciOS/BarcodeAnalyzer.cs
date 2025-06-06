﻿using AVFoundation;
using CoreGraphics;
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
    private readonly Lock _resultsLock;
    private readonly VNDetectBarcodesRequest _detectBarcodesRequest;
    private readonly VNSequenceRequestHandler _sequenceRequestHandler;

    private Point _previewCenter = new();
    private Rect _previewRect = new();
    private VNBarcodeObservation[] _result = [];

    internal BarcodeAnalyzer(CameraManager cameraManager)
    {
        _barcodeResults = [];
        _cameraManager = cameraManager;
        _resultsLock = new();
        _detectBarcodesRequest = new VNDetectBarcodesRequest((request, error) => 
        {
            if (error is null)
                _result = request.GetResults<VNBarcodeObservation>();
            else
                _result = [];
        });
        _sequenceRequestHandler = new VNSequenceRequestHandler();

        _previewRect.X = 0;
        _previewRect.Y = 0;
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
            ArgumentNullException.ThrowIfNull(sampleBuffer);

            if (_cameraManager.CameraView.PauseScanning)
                return;

            _sequenceRequestHandler.Perform([_detectBarcodesRequest], sampleBuffer, out _);

            lock (_resultsLock)
            {
                _barcodeResults.Clear();

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

                _cameraManager.CameraView.DetectionFinished(_barcodeResults);
            }

            if (_cameraManager.CameraView.ForceFrameCapture || (_cameraManager.CameraView.CaptureNextFrame && _barcodeResults.Count > 0))
            {
                using var imageBuffer = sampleBuffer.GetImageBuffer();
                if (imageBuffer is not null)
                {
                    using var cIImage = new CIImage(imageBuffer);
                    using var cIContext = new CIContext();
                    using var cGImage = cIContext.CreateCGImage(cIImage, cIImage.Extent);
                    if (cGImage is not null)
                    {
                        var image = new PlatformImage(new UIImage(cGImage));
                        _cameraManager.CameraView.TriggerOnImageCaptured(image);
                    }
                }
            }
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