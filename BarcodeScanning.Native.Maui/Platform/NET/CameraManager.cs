namespace BarcodeScanning;

internal class CameraManager : IDisposable
{
    internal BarcodeView? BarcodeView { get; set; }

    internal void UpdateAimMode() => throw new NotImplementedException();
    internal void UpdateSymbologies() => throw new NotImplementedException();
    internal void UpdateBackgroundColor() => throw new NotImplementedException();
    internal void UpdateCamera() => throw new NotImplementedException();
    internal void UpdateCameraEnabled() => throw new NotImplementedException();
    internal void UpdateResolution() => throw new NotImplementedException();
    internal void UpdateTapToFocus() => throw new NotImplementedException();
    internal void UpdateTorch() => throw new NotImplementedException();
    internal void UpdateVibration() => throw new NotImplementedException();
    internal void UpdateZoomFactor() => throw new NotImplementedException();

    public void Dispose() => throw new NotImplementedException();
}