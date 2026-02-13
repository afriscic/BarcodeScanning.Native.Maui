namespace BarcodeScanning;

internal class CameraManager : IDisposable
{
    internal BarcodeView? BarcodeView { get; set; }

    internal void UpdateAimMode() => throw new PlatformNotSupportedException();
    internal void UpdateSymbologies() => throw new PlatformNotSupportedException();
    internal void UpdateBackgroundColor() => throw new PlatformNotSupportedException();
    internal void UpdateCamera() => throw new PlatformNotSupportedException();
    internal void UpdateCameraEnabled() => throw new PlatformNotSupportedException();
    internal void UpdateResolution() => throw new PlatformNotSupportedException();
    internal void UpdateTapToFocus() => throw new PlatformNotSupportedException();
    internal void UpdateTorch() => throw new PlatformNotSupportedException();
    internal void UpdateVibration() => throw new PlatformNotSupportedException();
    internal void UpdateZoomFactor() => throw new PlatformNotSupportedException();

    public void Dispose() => throw new PlatformNotSupportedException();
}