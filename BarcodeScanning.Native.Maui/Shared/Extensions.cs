namespace BarcodeScanning;

public static class Extensions
{
    public static MauiAppBuilder UseBarcodeScanning(this MauiAppBuilder builder)
    {
        return builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(ICameraView), typeof(CameraViewHandler));
        });
    }
}