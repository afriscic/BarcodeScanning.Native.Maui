namespace BarcodeScanning;

public static partial class Methods
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
}