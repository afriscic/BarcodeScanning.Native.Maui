namespace BarcodeScanning;

public static class Methods
{
    public static async Task<bool> AskForRequiredPermissionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.Camera>();
            }
            status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status == PermissionStatus.Granted)
                return true;
        }
        catch (Exception)
        {

        }
        return false;
    }

    public static Task<HashSet<BarcodeResult>> ScanFromImageAsync(byte[] imageArray)
    {
       #if IOS
       return Platforms.iOS.Methods.ScanFromImage(imageArray);
       #elif ANDROID
       return Platforms.Android.Methods.ScanFromImage(imageArray);
       #endif

    }
}