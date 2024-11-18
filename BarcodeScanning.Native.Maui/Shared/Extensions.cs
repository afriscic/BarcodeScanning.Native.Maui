namespace BarcodeScanning;

public static partial class Extensions
{
    public static MauiAppBuilder UseBarcodeScanning(this MauiAppBuilder builder)
    {
        return builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<CameraView, CameraViewHandler>());
    }
}