namespace BarcodeScanning;

public static class Methods
{
    public static async Task<bool> AskForRequiredPermission()
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

    public static async Task<HashSet<BarcodeResult>> ScanFromImage(byte[] imageArray)
    {
       #if IOS
       return await Platforms.iOS.Methods.ScanFromImage(imageArray);
       #elif ANDROID
       return await Platforms.Android.Methods.ScanFromImage(imageArray);
       #endif

    }
}